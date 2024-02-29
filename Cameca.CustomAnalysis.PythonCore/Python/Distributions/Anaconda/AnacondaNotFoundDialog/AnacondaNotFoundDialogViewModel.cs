using CommunityToolkit.Mvvm.Input;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Windows.Input;

namespace Cameca.CustomAnalysis.PythonCore;

internal sealed class AnacondaNotFoundDialogViewModel : BindableBase, IDialogAware
{
	public string Title { get; } = "";

	private string downloadUrl = "";
	public string DownloadUrl
	{
		get => downloadUrl;
		set => SetProperty(ref downloadUrl, value);
	}

	public ICommand DownloadCommand { get; }

	public ICommand CancelCommand { get; }

	public AnacondaNotFoundDialogViewModel()
	{
		DownloadCommand = new RelayCommand(OnDownload);
		CancelCommand = new RelayCommand(OnCancel);
	}

	private void OnCancel()
	{
		RaiseRequestClose(new DialogResult(ButtonResult.Cancel));
	}

	private void OnDownload()
	{
		RaiseRequestClose(new DialogResult(ButtonResult.OK));
	}

	public bool CanCloseDialog() => true;

	public void OnDialogClosed() { }

	public void OnDialogOpened(IDialogParameters parameters)
	{
		DownloadUrl = parameters.GetValue<string>("DownloadUrl") ?? "";
	}

	public event Action<IDialogResult>? RequestClose;

	private void RaiseRequestClose(IDialogResult dialogResult)
	{
		RequestClose?.Invoke(dialogResult);
	}
}
