using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;

namespace Cameca.CustomAnalysis.PythonCore;
internal static class PythonDialogExtensions
{
	public static void ShowPythonLocatorDialog(this IDialogService dialogService, List<PythonInstallation>? installations, Action<IDialogResult> callback)
	{
		var dialogParams = new DialogParameters
		{
			{ "installations", installations }
		};
		dialogService.ShowDialog("PythonLocatorDialog", dialogParams, callback);
	}

	public static void ShowPythonVenvDialog(this IDialogService dialogService, string venvPath, string pythonExe, string extensionDirectory, Action<IDialogResult> callback)
	{
		var dialogParams = new DialogParameters
		{
			{ "venvPath", venvPath },
			{ "pythonExe", pythonExe },
			{ "extensionDirectory", extensionDirectory },
		};
		dialogService.ShowDialog("PythonVenvDialog", dialogParams, callback);
	}
}
