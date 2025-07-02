using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Prism.Services.Dialogs;
using System;
using System.Windows.Media;

namespace Cameca.CustomAnalysis.PythonCore;

internal partial class DistributionNotFoundDialogViewModel : ObservableObject, IDialogAware
{
	public string Title { get; } = "";

	[ObservableProperty]
	private string distributionLabel = "Python";

	[ObservableProperty]
	private Color distributionColor = Color.FromRgb(48, 105, 152);

	[ObservableProperty]
	private string downloadUrl = "https://www.python.org/downloads/";

	[RelayCommand]
	private void Download()
	{
		RaiseRequestClose(new DialogResult(ButtonResult.OK));
	}

	[RelayCommand]
	private void Cancel()
	{
		RaiseRequestClose(new DialogResult(ButtonResult.Cancel));
	}

	public bool CanCloseDialog() => true;

	public void OnDialogClosed() { }

	public void OnDialogOpened(IDialogParameters parameters)
	{
		if (parameters.TryGetValue(nameof(DownloadUrl), out string url))
		{
			DownloadUrl = url;
		}
		if (parameters.TryGetValue(nameof(DistributionLabel), out string label))
		{
			DistributionLabel = label;
		}
		if (parameters.TryGetValue(nameof(DistributionColor), out Color color))
		{
			DistributionColor = color;
		}
	}

	public event Action<IDialogResult>? RequestClose;

	private void RaiseRequestClose(IDialogResult dialogResult)
	{
		RequestClose?.Invoke(dialogResult);
	}
}
