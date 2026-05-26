using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.PythonCore.Python.Rpc;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Cameca.CustomAnalysis.PythonCore;

public class PythonRpcOptions : BindableBase
{
	[Display(Name = "Try Auto-Detect Python")]
	public DelegateCommand AutoLocatePythonCommand { get; }

	private string? pythonExe;
	[Display(Name = "Python EXE Path")]
	[FilePath(Filter = "EXE (*.exe)|*.exe")]
	public string? PythonExe
	{
		get => pythonExe;
		set => SetProperty(ref pythonExe, value, () => AutoCreateVenvCommand.RaiseCanExecuteChanged());
	}

	[Display(Name = "Try Auto-Create Virtual Environment")]
	public DelegateCommand AutoCreateVenvCommand { get; }

	private string? pythonVenvDir;
	[Display(Name = "Python Virtual Environment Directory")]
	[FolderPath]
	public string? PythonVenvDir
	{
		get => pythonVenvDir;
		set => SetProperty(ref pythonVenvDir, value);
	}

	[Display(Name = "Open Extension Directory")]
	public DelegateCommand OpenDirectoryCommand { get; }

	private bool showPythonTerminal =  false;
	[Display(
		Name = "Show Python Terminal",
		Description = "Shows terminal window with Python process output, useful for development or debugging.")]
	public bool ShowPythonTerminal
	{
		get => showPythonTerminal;
		set => SetProperty(ref showPythonTerminal, value);
	}

	private bool keepTerminalOpen = false;
	[Display(
		Name = "Keep Python Terminal Open",
		Description = "If showing Python terminal, this keeps the window open after the extension closes. Useful for seeing logs upon an unexpected crash. All Python extensions will not clean up with this option enabled, so it is recommended to only enable why diagnosing issues with a problematic extension.")]
	public bool KeepTerminalOpen
	{
		get => keepTerminalOpen;
		set => SetProperty(ref keepTerminalOpen, value);
	}

	private PythonSupportedLogLevel pythonLogLevel = PythonSupportedLogLevel.Information;
	[Display(Name = "Python Log Level")]
	public PythonSupportedLogLevel PythonLogLevel
	{
		get => pythonLogLevel;
		set => SetProperty(ref pythonLogLevel, value);
	}

	private PythonSupportedLogLevel pythonRpcLogLevel = PythonSupportedLogLevel.Warning;
	[Display(Name = "Python Host Mapped Log Level", Description = "Level of messages sent to host application logging to show in main logging screen")]
	public PythonSupportedLogLevel PythonRpcLogLevel
	{
		get => pythonRpcLogLevel;
		set => SetProperty(ref pythonRpcLogLevel, value);
	}

	private readonly string extensionDirectory;

	public PythonRpcOptions()
	{
		extensionDirectory = new DirectoryInfo(Assembly.GetExecutingAssembly().Location).Parent!.FullName;
		AutoLocatePythonCommand = new DelegateCommand(AutoLocatePython);
		OpenDirectoryCommand = new DelegateCommand(OpenDirectory);
		AutoCreateVenvCommand = new DelegateCommand(AutoCreateVenv, CanAutoCreateVenv);
	}

	private void AutoCreateVenv()
	{
		if (!File.Exists(PythonExe))
		{
			return;
		}

		var venvPath = Path.Join(extensionDirectory, "venv");
		Dialogs.ShowPythonVenvDialog(venvPath, PythonExe, extensionDirectory, (result) =>
		{
			switch (result.Parameters.GetValue<PythonCreateVenvResult>("createResult"))
			{
				case PythonCreateVenvResult.Created:
					PythonVenvDir = venvPath;
					break;
				case PythonCreateVenvResult.Deleted:
					if (PythonVenvDir == venvPath)
					{
						PythonVenvDir = null;
					}
					break;
				default:
					break;
			}
		});
	}

	private bool CanAutoCreateVenv() => File.Exists(PythonExe);

	private void AutoLocatePython()
	{
		var installations = PythonLocator.FindPythonInstallations().ToList();

		// If no installations located, prompt for download
		if (!installations.Any())
		{
			CommonDistributionDialogs.ShowPythonDistributionNotFoundDialog();
			return;
		}

		// If a single installation located, simply selected it. No need to force the user to select only item in dialog list.
		if (installations.Count() == 1)
		{
			ApplyPythonInstallation(installations.Single());
			return;
		}

		// With mutiple installation found, prompt the user to select which version to use
		Dialogs.ShowPythonLocatorDialog(installations, (dialogResult) =>
		{
			if (dialogResult.Result == ButtonResult.OK && dialogResult.Parameters.GetValue<PythonInstallation?>("selected") is { } selected)
			{
				ApplyPythonInstallation(selected);
			}
		});
	}

	private void ApplyPythonInstallation(PythonInstallation installation)
	{
		var prevPythonExe = PythonExe;
		var prevPythonVenvDir = PythonVenvDir;

		var location = new FileInfo(installation.Path).DirectoryName;
		// Resolve best exe
		if (File.Exists(installation.Path))
		{
			PythonExe = installation.Path;
		}
		// Update virtual environment
		if (installation.VirtualEnvironment && new FileInfo(installation.Path).Directory is { } directoryInfo)
		{
			PythonVenvDir = directoryInfo.FullName;
		}
		else
		{
			PythonVenvDir = null;
		}
		if (!Directory.Exists(PythonVenvDir))
		{
			AutoCreateVenv();
		}
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
