using Cameca.CustomAnalysis.Interface;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using Prism.Ioc;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Linq;

namespace Cameca.CustomAnalysis.PythonCore;

public class PythonOptions : BindableBase
{
	[Display(Name = "Try Auto-Detect Python")]
	public DelegateCommand AutoLocatePythonCommand { get; }

	private string? pythonDll;
	[Display(Name = "Python DLL Path")]
	[FilePath(Filter = "DLL (*.dll)|*.dll")]
	public string? PythonDll
	{
		get => pythonDll;
		set => SetProperty(ref pythonDll, value);
	}

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
		set => SetProperty(ref pythonVenvDir, value, () => AutoConfigureCommand.RaiseCanExecuteChanged());
	}

	[Display(Name = "Try Auto-Configure Python Options")]
	public DelegateCommand AutoConfigureCommand { get; }

	private string prependPathEnvVar = "";
	[Display(Name = "Additional PATH Environment Variable Paths")]
	[FolderPath(AllowMultiple = true)]
	public string PrependPathEnvVar
	{
		get => prependPathEnvVar;
		set => SetProperty(ref prependPathEnvVar, value);
	}

	private bool applyCondaIntelMklFix;
	[Display(Name = "Apply Anaconda Fix", Description = "Sets KMP_DUPLICATE_LIB_OK=True. Necessary for Anaconda to function correctly with AP Suite. Enable when using Anaconda Python distributions, otherwise it likely should be disabled.")]
	public bool ApplyCondaIntelMklFix
	{
		get => applyCondaIntelMklFix;
		set => SetProperty(ref applyCondaIntelMklFix, value);
	}

	private bool setNoSiteFlag = true;
	[Display(Name = "Set NoSiteFlag", Description = "Disable the import of the module site and the site-dependent manipulations of sys.path that it entails.")]
	public bool SetNoSiteFlag
	{
		get => setNoSiteFlag;
		set => SetProperty(ref setNoSiteFlag, value);
	}

	private string? pythonHome;
	[FolderPath]
	[Display(Name = "PYTHONHOME Environment Variable")]
	public string? PythonHome
	{
		get => pythonHome;
		set => SetProperty(ref pythonHome, value);
	}

	[Display(Name = "Open Extension Directory")]
	public DelegateCommand OpenDirectoryCommand { get; }

	private readonly string extensionDirectory;
	private readonly string testAppPath;

	public PythonOptions()
	{
		extensionDirectory = new DirectoryInfo(Assembly.GetExecutingAssembly().Location).Parent!.FullName;
		testAppPath = Path.Join(extensionDirectory, "PythonNetTest.exe");
		AutoLocatePythonCommand = new DelegateCommand(AutoLocatePython);
		OpenDirectoryCommand = new DelegateCommand(OpenDirectory);
		AutoCreateVenvCommand = new DelegateCommand(AutoCreateVenv, CanAutoCreateVenv);
		AutoConfigureCommand = new DelegateCommand(AutoConfigure);
	}

	private void AutoCreateVenv()
	{
		if (!File.Exists(PythonExe))
		{
			return;
		}

		var venvPath = Path.Join(extensionDirectory, "venv");
		ContainerLocator.Container.Resolve<IDialogService>()
			.ShowPythonVenvDialog(venvPath, PythonExe, extensionDirectory, (result) =>
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

	private bool CanAutoCreateVenv() => File.Exists(PythonExe);

	private void AutoLocatePython()
	{
		var installations = PythonLocator.FindPythonInstallations().ToList();

		var dialogService = ContainerLocator.Container.Resolve<IDialogService>();
		dialogService.ShowPythonLocatorDialog(installations, (dialogResult) =>
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
