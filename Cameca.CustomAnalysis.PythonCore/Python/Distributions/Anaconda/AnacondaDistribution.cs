using Microsoft.Extensions.Logging;
using Prism.Services.Dialogs;
using Python.Runtime;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Cameca.CustomAnalysis.PythonCore;

internal class AnacondaDistribution : IPyDistribution
{
	private readonly ILogger<AnacondaDistribution> logger;
	private readonly AnacondaAutoResolver autoResolver;
	private readonly IDialogService dialogService;
	private readonly AnacondaDistributionOptions options;

	public AnacondaDistribution(ILogger<AnacondaDistribution> logger, AnacondaAutoResolver autoResolver, IDialogService dialogService, AnacondaDistributionOptions options)
	{
		this.logger = logger;
		this.autoResolver = autoResolver;
		this.dialogService = dialogService;
		this.options = options;
	}

	/// <summary>
	/// Configure paths for an Anaconda distribution.
	/// If Anaconda cannot be found, prompt for installation.
	/// </summary>
	/// <returns></returns>
	public bool Initialize()
	{
		if (autoResolver.AutoLocateAnacondaPath() is not { } condaPath)
		{
			if (options.ShowDownloadPrompt)
			{
				ShowPromptForDownload();
			}
			return false;
		}

		try
		{
			return InitializeAnacondaImpl(condaPath, options);
		}
		catch (Exception ex)
		{
			logger.LogWarning(ex, AnacondaResources.LogWarningGeneralInitializeException);
			return false;
		}
	}

	/// <summary>
	/// Initialized the Python engine from the Anaconda distribution and environment.
	/// </summary>
	/// <remarks>
	/// If an optional environment name is provided, use that environment. Note that any Anaconda specific functionality such as activation scripts will not be supported.
	/// If the environment doesn't exist, optionally fallback to the base environment
	/// </remarks>
	/// <param name="condaPath"></param>
	/// <param name="env"></param>
	/// <param name="fallbackToBase"></param>
	/// <returns></returns>
	private bool InitializeAnacondaImpl(string condaPath, AnacondaDistributionOptions options)
	{
		string? env = GetEnvironmentName(options.EnvironmentFilePath) ?? options.Environment;
		// Try to resolve the environment. If the correct envs folder doesn't exist, log a warning and fallback to base
		if (env is not null)
		{
			if (ActivateEnvironment(condaPath, env, options, options.TryCreateEnvironment) is not { } envPath)
			{
				return false;
			}
			condaPath = envPath;
		}

		// Locate and set Python DLL using the highest version python.dll found in the anaconda directory
		if (PyPathTools.ResolvePythonDll(condaPath) is not { } dllPath)
		{
			return false;
		}
		Runtime.PythonDLL = dllPath;

		// Create new path environment variable with conda execution paths first

		var condaActivatePaths = new string[]
		{
			condaPath,
			Path.Join(condaPath, "Scripts"),
			Path.Join(condaPath, "Library"),
			Path.Join(condaPath, "bin"),
			Path.Join(condaPath, "Library", "bin"),
			Path.Join(condaPath, "Library", "mingw-w64", "bin"),
		};

		string updatedPath = PrependToPathSeparatedValue(Environment.GetEnvironmentVariable("PATH"), condaActivatePaths);
		Environment.SetEnvironmentVariable("PATH", updatedPath, EnvironmentVariableTarget.Process);
		Environment.SetEnvironmentVariable("PYTHONHOME", condaPath, EnvironmentVariableTarget.Process);

		PythonEngine.PythonHome = condaPath;

		PythonEngine.Initialize();

		// Apply distribution specific adjustments
		ApplyIntelMklFix();

		return true;
	}

	private string? ActivateEnvironment(string condaPath, string env, AnacondaDistributionOptions options, bool tryCreate = true)
	{
		var envPath = Path.Join(condaPath, "envs", env);
		if (Directory.Exists(envPath))
		{
			return envPath;
		}
		// Only allow one try create with recursion breaking flag
		else if (tryCreate && options.EnvironmentFilePath is not null && options.TryCreateEnvironment)
		{
			TryCreateEnvironment(condaPath, options.EnvironmentFilePath);
			return ActivateEnvironment(condaPath, env, options, false);
		}
		else if (options.FallbackToBase || string.Equals(env, "base", StringComparison.Ordinal))  // Default env "base" isn't in envs directory, but assume if name is "base" we want implicit fallbackToBase
		{
			logger.LogWarning("Could not resolve Anaconda environment '{Environment}' at '{EnvironmentPath}'. Falling back to base environment.", env, envPath);
			return condaPath;
		}
		else
		{
			logger.LogWarning("Could not resolve Anaconda environment '{Environment}' at '{EnvironmentPath}'. Fallbackto base environment is not enabled.", env, envPath);
		}
		return null;
	}

	/// <summary>
	/// TODO: Currently doesn't seem to work. Some OpenSSL error. I don't have time to fix it, but don't rely on this feature for now
	/// </summary>
	/// <param name="condaPath"></param>
	/// <param name="environmentFilePath"></param>
	private void TryCreateEnvironment(string condaPath, string environmentFilePath)
	{
		try
		{
			string condaExePath = Path.Join(condaPath, "Scripts", "conda.exe");
			string condaCommand = $"\"{condaExePath}\" env create -f \"{environmentFilePath}\"";

			// Create a new process
			Process process = new Process();
			// Configure the process using the StartInfo properties
			process.StartInfo.RedirectStandardOutput = true; // Redirects the standard output so it can be read
			process.StartInfo.UseShellExecute = false; // Required to redirect
			process.StartInfo.CreateNoWindow = false; // Prevents the command window from popping up

			process.Start(); // Start the process
			process.WaitForExit(); // Wait for the process to exit
			string result = process.StandardOutput.ReadToEnd();
			logger.LogDebug(result);
		}
		catch (Exception ex)
		{
			logger.LogWarning(ex, "Could not create Anaconda environment: {EnvironmentFilePath}", environmentFilePath);
		}
	}

	private void ShowPromptForDownload()
	{
		var downloadUrl = AnacondaResources.AnacondaDownloadUrl;
		dialogService.ShowAnacondaNotFound(downloadUrl, result =>
		{
			if (result.Result == ButtonResult.OK)
			{
				LaunchDownloadUrl(downloadUrl);
			}
		});
	}

	/// <summary>
	/// Execute download URL with ShellExecute to open download link in default browser
	/// </summary>
	/// <param name="downloadUrl"></param>
	private static void LaunchDownloadUrl(string downloadUrl)
	{
		Process.Start(new ProcessStartInfo(downloadUrl)
		{
			UseShellExecute = true,
		});
	}

	// TODO: Remove unsafe and undocumented workaround for mkl libraries though either AP Suite update or out of process Python
	/// <summary>
	/// Add KMP_DUPLICATE_LIB_OK = TRUE to os.environ. Must be called after <see cref="PythonEngine.Initialize()"/>
	/// </summary>
	/// <remarks>
	/// Anaconda by default includes libraries that attempt to load libiomp5md.dll, making this an Anaconda specific adjustment.
	/// 
	/// Solves crash when utilizing some libraries that load Intel MKL
	/// "OMP: Error #15: Initializing libiomp5md.dll, but found libiomp5md.dll already initialized."
	/// According to error message, this workaround is unsafe and not recommended
	/// "As an unsafe, unsupported, undocumented workaround you can set the environment variable KMP_DUPLICATE_LIB_OK=TRUE to allow the program to continue to execute, but that may cause crashes or silently produce incorrect results."
	/// I don't believe this can be resolved without dynamically linking mkl in AP Suite instead of static linking or by running the Python interpreter in a separate process.
	/// </remarks>
	private static void ApplyIntelMklFix()
	{
		using var _ = Py.GIL();
		var os = Py.Import("os");
		os.GetAttr("environ")["KMP_DUPLICATE_LIB_OK"] = new PyString("TRUE");
	}

	private static string PrependToPathSeparatedValue(string? source, params string[] paths)
	{
		// Parse all existing value into individual path components
		var parsedCurPaths = (source?.Split(Path.PathSeparator) ?? Enumerable.Empty<string>())
			.Where(x => !string.IsNullOrEmpty(x))
			.ToList();

		// Parse all new value into individual path components (each entry could have multiple paths itself)
		var parsedNewPaths = paths.SelectMany(pth => pth.Split(Path.PathSeparator))
			.Where(x => !string.IsNullOrEmpty(x))
			.ToList();

		var missingPaths = parsedNewPaths.Except(parsedCurPaths).ToList();

		// Prepend all missing paths to the existing list and rejoin
		return string.Join(Path.PathSeparator, missingPaths.Concat(parsedCurPaths));
	}

	private static string? GetEnvironmentName(string? envFilePath)
	{
		if (envFilePath is null || !File.Exists(envFilePath))
		{
			return null;
		}
		try
		{
			string? nameLine = null;
			using (var file = File.OpenRead(envFilePath))
			using (var reader = new StreamReader(file))
			{
				nameLine = reader.ReadLine();
			}
			if (nameLine is not null)
			{
				var splitLine = nameLine.Split(":", 2);
				if (splitLine.Length == 2 && splitLine[0].Trim().Equals("name", System.StringComparison.Ordinal))
				{
					return splitLine[1].Trim();
				}
			}
		}
		catch { }
		return null;
	}
}
