using Prism.Services.Dialogs;
using System;

namespace Cameca.CustomAnalysis.PythonCore;

internal static class AnacondaNotFoundDialogServiceExtensions
{
	public static void ShowAnacondaNotFound(this IDialogService dialogService, string url, Action<IDialogResult> callback)
	{
		dialogService.ShowDialog(
			nameof(AnacondaNotFoundDialogView),
			new DialogParameters
			{
				{ "DownloadUrl", url },
			},
			callback,
			nameof(AnacondaNotFoundDialogWindow));
	}
}
