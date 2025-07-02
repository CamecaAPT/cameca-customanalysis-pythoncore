using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;

namespace Cameca.CustomAnalysis.PythonCore;

internal static class Dialogs
{
	public static void ShowPythonLocatorDialog(List<PythonInstallation>? installations, Action<IDialogResult> callback)
	{
		var dialogParams = new DialogParameters
		{
			{ "installations", installations }
		};
		InternalDialogService.ShowDialog<PythonLocatorDialogView, PythonLocatorDialogViewModel>(dialogParams, callback);
	}

	public static void ShowPythonVenvDialog(string venvPath, string pythonExe, string extensionDirectory, Action<IDialogResult> callback)
	{
		var dialogParams = new DialogParameters
		{
			{ "venvPath", venvPath },
			{ "pythonExe", pythonExe },
			{ "extensionDirectory", extensionDirectory },
		};
		InternalDialogService.ShowDialog<PythonVenvDialogView, PythonVenvDialogViewModel>(dialogParams, callback);
	}
}
