using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Cameca.CustomAnalysis.PythonCore;

internal static class PythonDialogExtensions
{
	// Not nullable: is not a generic type parameter
	internal static readonly string PythonLocatorDialogKey = typeof(PythonLocatorDialogViewModel).AssemblyQualifiedName!;
	internal static readonly string PythonVenvDialogKey = typeof(PythonVenvDialogViewModel).AssemblyQualifiedName!;

	public static void ShowPythonLocatorDialog(this IDialogService dialogService, List<PythonInstallation>? installations, Action<IDialogResult> callback)
	{
		string serialized = JsonSerializer.Serialize(installations);
		var dialogParams = new DialogParameters
		{
			{ "installations", serialized }
		};
		System.Diagnostics.Debug.WriteLine(PythonLocatorDialogKey);
		System.Diagnostics.Debug.WriteLine(PythonLocatorDialogKey);
		System.Console.WriteLine(PythonLocatorDialogKey);
		dialogService.ShowDialog(PythonLocatorDialogKey, dialogParams, callback);
	}

	public static void ShowPythonVenvDialog(this IDialogService dialogService, string venvPath, string pythonExe, string extensionDirectory, Action<IDialogResult> callback)
	{
		var dialogParams = new DialogParameters
		{
			{ "venvPath", venvPath },
			{ "pythonExe", pythonExe },
			{ "extensionDirectory", extensionDirectory },
		};
		dialogService.ShowDialog(PythonVenvDialogKey, dialogParams, callback);
	}
}
