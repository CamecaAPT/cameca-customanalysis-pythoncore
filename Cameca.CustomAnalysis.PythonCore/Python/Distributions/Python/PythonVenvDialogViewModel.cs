#nullable enable
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Cameca.CustomAnalysis.PythonCore;

internal partial class PythonVenvDialogViewModel : ObservableObject, IDialogAware
{
	private readonly object _outputItemsLock = new();
	private PythonCreateVenvResult createResult = PythonCreateVenvResult.None;
	private string? venvPath;

	public ObservableCollection<string> CmdStatements { get; } = new();

	public string CmdStatementText => string.Join(Environment.NewLine, CmdStatements) + Environment.NewLine;

	public ObservableCollection<string> TextContent { get; } = new();

	public string Title { get; } = "Create Virtual Environment";

	public event Action<IDialogResult>? RequestClose;

	public PythonVenvDialogViewModel()
	{
		CmdStatements.CollectionChanged += CmdStatements_CollectionChanged;
	}

	private void CmdStatements_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
	{
		OnPropertyChanged(nameof(CmdStatementText));
	}

	public bool CanCloseDialog() => true;

	public void OnDialogClosed() { }

	public void OnDialogOpened(IDialogParameters parameters)
	{
		TextContent.Clear();
		venvPath = parameters.GetValue<string>("venvPath");
		var pythonExePath = parameters.GetValue<string>("pythonExe");
		var extensionDirectory = parameters.GetValue<string>("extensionDirectory");

		// Build command
		if (!File.Exists(pythonExePath))
		{
			return;
		}

		string requirementsFile = Path.Join(extensionDirectory, "requirements.txt");

		// Combine both commands into one
		string createVenvCommand = $"\"{pythonExePath}\" -m venv \"{venvPath}\"";
		string venvPythonExePath = Path.Join(venvPath, "Scripts", "python.exe");
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

		CmdStatements.AddRange(subCommands);
	}

	private void CloseDialog(ButtonResult result)
	{
		RaiseRequestClose(new DialogResult(result, new DialogParameters { { "createResult", createResult } }));
	}

	private void RaiseRequestClose(IDialogResult dialogResult) => RequestClose?.Invoke(dialogResult);

	public bool CanDelete => venvPath is not null && Directory.Exists(venvPath);

	[RelayCommand(CanExecute = nameof(CanDelete))]
	public void Delete()
	{
		TextContent.Clear();
		if (venvPath is not null && Directory.Exists(venvPath))
		{
			AppendText($"Deleting: {venvPath}");
			try
			{
				Directory.Delete(venvPath, true);
				AppendText("-- Virtual environment deleted successfully --");
				createResult = PythonCreateVenvResult.Deleted;
			}
			catch (Exception ex)
			{
				AppendText("-- Failed to delete firtual environment --");
				AppendText(ex.Message);
				AppendText(ex.StackTrace);
			}
		}
		DeleteCommand.NotifyCanExecuteChanged();
	}

	[RelayCommand(IncludeCancelCommand = true)]
	public async Task Run(CancellationToken cancellationToken)
	{
		TextContent.Clear();
		try
		{
			bool success = true;
			foreach (var cmd in CmdStatements)
			{
				success &= await RunCmdStatement(cmd, cancellationToken);
				if (!success)
				{
					AppendText("-- Virtual environment creation did not finish successfully --");
					return;
				}
			}
			AppendText("-- Virtual environment creation successful! --");
			createResult = PythonCreateVenvResult.Created;
			return;
		}
		catch (OperationCanceledException)
		{
			AppendText("-- Virtual environment creation cancelled --");
		}
		finally
		{
			DeleteCommand.NotifyCanExecuteChanged();
		}
		// In any case where creation was not successful: mark as deleted as the venv state could be in a corrupted state
		createResult = PythonCreateVenvResult.Deleted;
	}

	private async Task<bool> RunCmdStatement(string commandText, CancellationToken cancellationToken)
	{
		AppendText($"{Environment.NewLine}> {commandText}");
		ProcessStartInfo startInfo = new ProcessStartInfo
		{
			FileName = "cmd.exe",
			Arguments = $"/C \"{commandText}\"",
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

		using Process process = new Process
		{
			StartInfo = startInfo,
			EnableRaisingEvents = true,
		};
		process.OutputDataReceived += (sender, e) => AppendText(e.Data);
		process.ErrorDataReceived += (sender, e) => AppendText(e.Data);

		process.Start();
		process.BeginOutputReadLine();
		process.BeginErrorReadLine();
		// Start process in task so we can attempt graceful cancellation of the underlying process
		var processTask = Task.Run(() => process.WaitForExitAsync(), cancellationToken);
		try
		{
			await processTask;
		}
		catch (OperationCanceledException)
		{
			// If process is still running, try to clean it up explicitly
			if (!process.HasExited)
			{
				process.Kill(entireProcessTree: true);
			}
			throw;
		}
		await process.WaitForExitAsync(cancellationToken);
		return process.ExitCode == 0;
	}

	private void AppendText(string? data)
	{
		if (data is null) return;
		Application.Current.Dispatcher.BeginInvoke(() =>
		{
			lock (_outputItemsLock)
			{
				TextContent.Add(data + Environment.NewLine);
			}
		});
	}

	[RelayCommand]
	public void Ok()
	{
		CloseDialog(ButtonResult.OK);
	}

	[RelayCommand]
	public void Cancel()
	{
		if (RunCancelCommand.CanExecute(null))
		{
			RunCancelCommand.Execute(null);
		}
		CloseDialog(ButtonResult.Cancel);
	}
}
#nullable restore
