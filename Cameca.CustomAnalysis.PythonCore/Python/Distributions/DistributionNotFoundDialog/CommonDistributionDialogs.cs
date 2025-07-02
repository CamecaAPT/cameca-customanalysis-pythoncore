using Prism.Services.Dialogs;
using System.Diagnostics;
using System.Windows.Media;

namespace Cameca.CustomAnalysis.PythonCore;

internal static class CommonDistributionDialogs
{
	public static void ShowPythonDistributionNotFoundDialog()
	{
		string downloadUrl = "https://www.python.org/downloads/";
		ShowNotFoundDialogImpl(
			downloadUrl,
			"Python",
			Color.FromRgb(48, 105, 152));
	}

	public static void ShowAnacondaDistributionNotFoundDialog()
	{
		string downloadUrl = AnacondaResources.AnacondaDownloadUrl;
		ShowNotFoundDialogImpl(
			downloadUrl,
			"Anaconda",
			Color.FromRgb(67, 176, 73));
	}

	private static void ShowNotFoundDialogImpl(string downloadUrl, string distributionLabel, Color distributionColor)
	{
		var dialogParams = new DialogParameters
		{
			{ nameof(DistributionNotFoundDialogViewModel.DownloadUrl), downloadUrl },
			{ nameof(DistributionNotFoundDialogViewModel.DistributionLabel), distributionLabel },
			{ nameof(DistributionNotFoundDialogViewModel.DistributionColor), distributionColor },
		};
		InternalDialogService.ShowDialog<DistributionNotFoundDialogView, DistributionNotFoundDialogViewModel, DistributionNotFoundDialogWindow>(
			dialogParams,
			result =>
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
}
