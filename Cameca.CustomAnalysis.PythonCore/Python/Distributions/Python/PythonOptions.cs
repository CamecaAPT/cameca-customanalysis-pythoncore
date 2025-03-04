using Cameca.CustomAnalysis.Interface;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Windows;

namespace Cameca.CustomAnalysis.PythonCore;

public class PythonOptions : BindableBase
{
	[Display(Name = "Try Auto-Locate Python")]
	public DelegateCommand AutoLocatePythonCommand { get; }

	private string? pythonDll;
	[Display(Name = "Python DLL Path")]
	[FilePath(Filter = "DLL (*.dll)|*.dll")]
	public string? PythonDll
	{
		get => pythonDll;
		set => SetProperty(ref pythonDll, value, () => TestCommand.RaiseCanExecuteChanged());
	}

	private string? pythonExe;
	[Display(Name = "Python EXE Path")]
	[FilePath(Filter = "EXE (*.exe)|*.exe")]
	public string? PythonExe
	{
		get => pythonExe;
		set => SetProperty(ref pythonExe, value, () =>
		{
			TestCommand.RaiseCanExecuteChanged();
			AutoCreateVenvCommand.RaiseCanExecuteChanged();
		});
	}

	[Display(Name = "Try Auto-Create Virtual Environment")]
	public DelegateCommand AutoCreateVenvCommand { get; }

	private string? pythonVenvDir;
	[Display(Name = "Python Virtual Environment Directory")]
	[FolderPath]
	public string? PythonVenvDir
	{
		get => pythonVenvDir;
		set => SetProperty(ref pythonVenvDir, value, () =>
		{
			AutoCreateVenvCommand.RaiseCanExecuteChanged();
			AutoConfigureCommand.RaiseCanExecuteChanged();
		});
	}

	[Display(Name = "Try Auto-Configure Python Options")]
	public DelegateCommand AutoConfigureCommand { get; }

	private string? prependPathEnvVar;
	[Display(Name = "Additional PATH Environment Variable Paths")]
	[FolderPath(AllowMultiple = true)]
	public string? PrependPathEnvVar
	{
		get => prependPathEnvVar;
		set => SetProperty(ref prependPathEnvVar, value);
	}

	private bool applyCondaIntelMklFix;
	public bool ApplyCondaIntelMklFix
	{
		get => applyCondaIntelMklFix;
		set => SetProperty(ref applyCondaIntelMklFix, value);
	}

	private bool setNoSiteFlag = true;
	public bool SetNoSiteFlag
	{
		get => setNoSiteFlag;
		set => SetProperty(ref setNoSiteFlag, value);
	}

	private string? pythonHome;
	[FolderPath]
	public string? PythonHome
	{
		get => pythonHome;
		set => SetProperty(ref pythonHome, value);
	}

	private string? workingDirectory;
	[Display(
		Name = "Working Directory",
		Description = "The directory that the Python extension is executed in. Paths encoded in the Python code are resolved relative to this directory.",
		AutoGenerateField = false)]
	[FolderPath]
	public string? WorkingDirectory
	{
		get => workingDirectory;
		set => SetProperty(ref workingDirectory, value);
	}

	[Display(Name = "Run Python Test Application", AutoGenerateField = false)]
	public DelegateCommand TestCommand { get; }

	[Display(Name = "Open Extension Directory")]
	public DelegateCommand OpenDirectoryCommand { get; }

	private readonly string extensionDirectory;
	private readonly string testAppPath;

	public PythonOptions()
	{
		extensionDirectory = new DirectoryInfo(Assembly.GetExecutingAssembly().Location).Parent!.FullName;
		testAppPath = Path.Join(extensionDirectory, "PythonNetTest.exe");
		AutoLocatePythonCommand = new DelegateCommand(AutoLocatePython);
		TestCommand = new DelegateCommand(Test, () => File.Exists(PythonDll) && File.Exists(testAppPath));
		OpenDirectoryCommand = new DelegateCommand(OpenDirectory);
		AutoCreateVenvCommand = new DelegateCommand(AutoCreateVenv, CanAutoCreateVenv);
		AutoConfigureCommand = new DelegateCommand(AutoConfigure);
	}

	private void AutoCreateVenv()
	{
		var venvPath = !string.IsNullOrEmpty(PythonVenvDir)
			? PythonVenvDir
			: Path.Join(extensionDirectory, "venv");
		if (!File.Exists(PythonExe) || Directory.Exists(venvPath))
		{
			return;
		}

		string pythonExePath = PythonExe;
		string venvDirectory = venvPath;
		string requirementsFile = Path.Join(extensionDirectory, "requirements.txt");

		// Combine both commands into one
		string createVenvCommand = $"\"{pythonExePath}\" -m venv \"{venvDirectory}\"";
		string venvPythonExePath = Path.Join(venvDirectory, "Scripts", "python.exe");
		string updatePipCommand = $"\"{venvPythonExePath}\" -m pip install --upgrade pip";
		string installDepsCommand = $"\"{venvPythonExePath}\" -m pip install -r \"{requirementsFile}\"";

		var subCommands = new List<string>
		{
			createVenvCommand,
		};
		if (File.Exists(requirementsFile))
		{
			subCommands.Add(updatePipCommand);
			subCommands.Add(installDepsCommand);
		}

		string command = string.Join(" && ", subCommands);

		// Run both commands in one console window
		if (RunCommandInNewConsole(command))
		{
			MessageBox.Show("Virtual environment created successfully", "Virtual Environment Result", MessageBoxButton.OK);
		}
		else
		{
			MessageBox.Show("Error creating virtual environment", "Virtual Environment Result", MessageBoxButton.OK, MessageBoxImage.Error);
		}

		PythonVenvDir = venvPath;
	}

	private void AutoConfigure()
	{
		// Check if conda environment
		bool isCondaEnv = PythonVenvDir is not null && Directory.Exists(Path.Join(PythonVenvDir, "conda-meta"));
		if (isCondaEnv)
		{
			ApplyCondaOptions();
		}
		else
		{
			ApplyDefaultOptions();
		}
	}

	private void ApplyCondaOptions()
	{
		PrependPathEnvVar = string.Join(Path.PathSeparator, new string[]
		{
				Path.Join(PythonVenvDir, "Scripts"),
				Path.Join(PythonVenvDir, "Library"),
				Path.Join(PythonVenvDir, "bin"),
				Path.Join(PythonVenvDir, "Library", "bin"),
				Path.Join(PythonVenvDir, "Library", "mingw-w64", "bin"),
		});
		SetNoSiteFlag = false;
		ApplyCondaIntelMklFix = true;
		if (PythonVenvDir is not null)
		{
			PythonHome = PythonVenvDir;
		}
		else if (PythonDll is not null && new FileInfo(PythonDll).Directory?.FullName is { } dllDir)
		{
			PythonHome = dllDir;
		}
		else if (PythonExe is not null && new FileInfo(PythonExe).Directory?.FullName is { } exeDir)
		{
			PythonHome = exeDir;
		}
	}

	private void ApplyDefaultOptions()
	{
		PrependPathEnvVar = "";
		SetNoSiteFlag = true;
		ApplyCondaIntelMklFix = false;
	}

	static bool RunCommandInNewConsole(string command)
	{
		ProcessStartInfo startInfo = new ProcessStartInfo
		{
			FileName = "cmd.exe",
			Arguments = $"/c \"{command}\"",
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			CreateNoWindow = true
		};
		var startInfoPaths = new List<string?>
		{
			Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process),
			Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User),
			Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine),
		};
		var startInfoPath = string.Join(";", startInfoPaths.Where(x => !string.IsNullOrWhiteSpace(x)));
		startInfo.EnvironmentVariables["PATH"] = startInfoPath;

		using Process? process = Process.Start(startInfo);
		if (process is null)
		{
			return false;
		}
		process.WaitForExit();
		return process.ExitCode == 0;
	}

	private bool CanAutoCreateVenv() => File.Exists(PythonExe) && (string.IsNullOrEmpty(PythonVenvDir) || !Directory.Exists(PythonVenvDir));

	private void AutoLocatePython()
	{
		var installations = PythonLocator.FindPythonInstallations().ToList();

		var dialogService = ContainerLocator.Container.Resolve<IDialogService>();
		var dialogParams = new DialogParameters
		{
			{ "installations", installations }
		};
		dialogService.ShowDialog("PythonLocatorDialog", dialogParams, (dialogResult) =>
		{
			if (dialogResult.Result == ButtonResult.OK && dialogResult.Parameters.GetValue<PythonInstallation?>("selected") is { } selected)
			{
				var location = new FileInfo(selected.Path).DirectoryName;
				// Resolve best DLL
				if (location is not null && PyPathTools.ResolvePythonDll(location) is { } dllPath)
				{
					PythonDll = dllPath;
				}
				// Resolve best exe
				if (File.Exists(selected.Path))
				{
					PythonExe = selected.Path;
				}
				if (selected.VirtualEnvironment && new FileInfo(selected.Path).Directory is { } directoryInfo)
				{
					PythonVenvDir = directoryInfo.FullName;
				}
			}
		});

	}

	private void Test()
	{
		Process p = new Process();
		ProcessStartInfo psi = new ProcessStartInfo();
		psi.FileName = "cmd.exe";
		string arguments = $"/K \"\"{testAppPath}\" \"{PythonDll}\"";
		if (!string.IsNullOrWhiteSpace(PythonVenvDir))
		{
			arguments += $" \"{PythonVenvDir}\"";
		}
		arguments += "\"";
		psi.Arguments = arguments;

		//var arguments = new List<string> { "/K", $"\"{testAppPath}\"", $"\"{PythonDll}\"" };
		//if (!string.IsNullOrWhiteSpace(PythonVenvDir))
		//{
		//	arguments.Add($"\"{PythonVenvDir}\"");
		//}
		//psi.ArgumentList.AddRange(arguments);
		p.StartInfo = psi;
		p.Start();
		//p.WaitForExit();
	}

	private void OpenDirectory()
	{
		Process p = new Process();
		ProcessStartInfo psi = new ProcessStartInfo(extensionDirectory)
		{
			UseShellExecute = true,
		};
		p.StartInfo = psi;
		p.Start();
	}
}
