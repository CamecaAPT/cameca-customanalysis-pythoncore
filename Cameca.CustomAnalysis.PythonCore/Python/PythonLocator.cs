using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;



#if WINDOWS
using Microsoft.Win32;
#endif

namespace Cameca.CustomAnalysis.PythonCore;

public class PythonInstallation
{
	public string Path { get; }
	public string Version { get; }
	public string Type { get; }
	[Display(AutoGenerateField = false)]
	public bool VirtualEnvironment { get; }

	public PythonInstallation(string path, string version, string type, bool virtualEnvironment = false)
	{
		Path = path;
		Version = version;
		Type = type;
		VirtualEnvironment = virtualEnvironment;
	}
}


public static class PythonLocator
{
	public static IEnumerable<PythonInstallation> FindPythonInstallations()
	{
		List<PythonInstallation> pythonInstallations = new List<PythonInstallation>();

		// Check common environment variables
		pythonInstallations.AddRange(GetPythonPathsFromEnvironmentVariables());

#if WINDOWS
		// Check registry for Python installations
		pythonInstallations.AddRange(GetPythonPathsFromRegistry());
#endif

		// Check common installation directories
		pythonInstallations.AddRange(GetPythonPathsFromDirectories());

		// Check for Anaconda installations
		pythonInstallations.AddRange(GetAnacondaPaths());

		return pythonInstallations;
	}

	private static IEnumerable<PythonInstallation> GetPythonPathsFromEnvironmentVariables()
	{
		List<PythonInstallation> installations = new List<PythonInstallation>();
		string[] envVars = { "PATH", "PYTHONPATH" };

		foreach (var envVar in envVars)
		{
			string? envValue = Environment.GetEnvironmentVariable(envVar);
			if (!string.IsNullOrEmpty(envValue))
			{
				foreach (var path in envValue.Split(Path.PathSeparator))
				{
					string pythonExePath = Path.Join(path, "python.exe");
					if (File.Exists(pythonExePath))
					{
						installations.Add(new PythonInstallation(
							pythonExePath,
							GetPythonVersion(pythonExePath),
							GetPythonType(pythonExePath)
						));
					}
				}
			}
		}

		return installations;
	}

#if WINDOWS
	private static IEnumerable<PythonInstallation> GetPythonPathsFromRegistry()
	{
		List<PythonInstallation> installations = new List<PythonInstallation>();
		string[] registryKeys = {
			@"SOFTWARE\Python\PythonCore",
			@"SOFTWARE\Wow6432Node\Python\PythonCore"
		};

		foreach (var key in registryKeys)
		{
			using (RegistryKey? baseKey = Registry.LocalMachine.OpenSubKey(key))
			{
				if (baseKey != null)
				{
					foreach (var subKeyName in baseKey.GetSubKeyNames())
					{
						using (RegistryKey? subKey = baseKey.OpenSubKey(subKeyName))
						{
							string? installPath = subKey?.GetValue("InstallPath") as string;
							if (!string.IsNullOrEmpty(installPath))
							{
								string pythonExePath = Path.Join(installPath, "python.exe");
								if (File.Exists(pythonExePath))
								{
									installations.Add(new PythonInstallation(
										pythonExePath,
										GetPythonVersion(pythonExePath),
										GetPythonType(pythonExePath)
									));
								}
							}
						}
					}
				}
			}
		}

		return installations;
	}
#endif

	private static IEnumerable<PythonInstallation> GetPythonPathsFromDirectories()
	{
		List<PythonInstallation> installations = new List<PythonInstallation>();
		string[] commonDirs = {
			Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Python"),
			Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Python"),
			Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Python")
		};

		foreach (var dir in commonDirs)
		{
			if (Directory.Exists(dir))
			{
				foreach (var subDir in Directory.GetDirectories(dir))
				{
					string pythonExePath = Path.Join(subDir, "python.exe");
					if (File.Exists(pythonExePath))
					{
						installations.Add(new PythonInstallation(
							pythonExePath,
							GetPythonVersion(pythonExePath),
							GetPythonType(pythonExePath)
						));
					}
				}
			}
		}

		return installations;
	}

	private static IEnumerable<PythonInstallation> GetAnacondaPaths()
	{
		List<PythonInstallation> installations = new List<PythonInstallation>();
		string[] anacondaDirs = {
			Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Anaconda3"),
			Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Miniconda3"),
			Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Anaconda3"),
			Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Miniconda3"),
			Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Anaconda3"),
			Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Miniconda3")
		};

		foreach (var dir in anacondaDirs)
		{
			if (Directory.Exists(dir))
			{
				string pythonExePath = Path.Join(dir, "python.exe");
				if (File.Exists(pythonExePath))
				{
					installations.Add(new PythonInstallation(
						pythonExePath,
						GetPythonVersion(pythonExePath),
						"Conda (base)"
					));
				}
			}
		}

		var installationsCopy = installations.ToList();
		foreach (var condaRootPath in installationsCopy)
		{
			if (new FileInfo(condaRootPath.Path).Directory?.FullName is not { } installationDirectory)
			{
				continue;
			}
			var envsDir = Path.Join(installationDirectory, "envs");
			if (Directory.Exists(envsDir))
			{
				foreach (var condaEnvDir in Directory.EnumerateDirectories(envsDir, "*", SearchOption.TopDirectoryOnly))
				{
					string envName = new DirectoryInfo(condaEnvDir).Name;
					string pythonExePath = Path.Join(condaEnvDir, "python.exe");
					if (File.Exists(pythonExePath))
					{
						installations.Add(new PythonInstallation(
							pythonExePath,
							GetPythonVersion(pythonExePath),
							$"Conda ({envName})",
							true
						));
					}
				}
			}
		}

		return installations;
	}

	private static string GetPythonVersion(string pythonExePath)
	{
		try
		{
			ProcessStartInfo start = new ProcessStartInfo
			{
				FileName = pythonExePath,
				Arguments = "--version",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				CreateNoWindow = true
			};

			using (Process? process = Process.Start(start))
			{
				if (process is not null)
				{
					using (StreamReader reader = process.StandardOutput)
					{
						string? result = reader.ReadLine();
						return result?.Split(' ')[1] ?? "Unknown";
					}
				}
			}
		}
		catch
		{
			// Error calling Python for version should not be fatal, just assume version is unknown
		}
		return "Unknown";
	}

	private static string GetPythonType(string pythonExePath)
	{
		string directory = Path.GetDirectoryName(pythonExePath) ?? string.Empty;
		if (Directory.Exists(Path.Join(directory, "conda-meta")))
		{
			return "Conda";
		}
		if (File.Exists(Path.Join(directory, "pypy.exe")))
		{
			return "PyPy";
		}
		if (File.Exists(Path.Join(directory, "jython.jar")))
		{
			return "Jython";
		}
		if (File.Exists(Path.Join(directory, "ipy.exe")))
		{
			return "IronPython";
		}
		return "CPython";
	}
}
