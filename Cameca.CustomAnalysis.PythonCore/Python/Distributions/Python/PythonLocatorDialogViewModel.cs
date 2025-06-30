using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Cameca.CustomAnalysis.PythonCore;

internal class PythonLocatorDialogViewModel : BindableBase, IDialogAware
{
	public string Title { get; } = "Auto-Detect Python Installation";

	public ObservableCollection<PythonInstallation> PythonInstances { get; } = new();

	private PythonInstallation? selectedInstallation;
	public PythonInstallation? SelectedInstallation
	{
		get => selectedInstallation;
		set => SetProperty(ref selectedInstallation, value, () => OkCommand.RaiseCanExecuteChanged());
	}

	public DelegateCommand OkCommand { get; }
	public DelegateCommand CancelCommand { get; }

	public PythonLocatorDialogViewModel()
	{
		OkCommand = new DelegateCommand(() => CloseDialog(ButtonResult.OK), () => SelectedInstallation is not null);
		CancelCommand = new DelegateCommand(() => CloseDialog(ButtonResult.Cancel));
	}

	public event Action<IDialogResult>? RequestClose;

	public bool CanCloseDialog() => true;

	public void OnDialogClosed() { }

	public void OnDialogOpened(IDialogParameters parameters)
	{
		PythonInstances.Clear();
		PythonInstances.AddRange(parameters.GetValue<List<PythonInstallation>>("installations"));
	}

	private void CloseDialog(ButtonResult result)
	{
		RaiseRequestClose(new DialogResult(result, new DialogParameters { { "selected", SelectedInstallation } }));
	}

	private void RaiseRequestClose(IDialogResult dialogResult)
	{
		RequestClose?.Invoke(dialogResult);
	}
}
