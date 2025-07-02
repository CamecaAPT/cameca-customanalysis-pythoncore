/** A stripped down version of DialogService from Prism Library that doesn't depend on the dialog being registered by name
 * https://github.com/PrismLibrary/Prism/blob/v8.1.97/src/Wpf/Prism.Wpf/Services/Dialogs/DialogService.cs
 * 
 * The MIT License (MIT)
 * 
 * Copyright (c) .NET Foundation
 * 
 * All rights reserved. Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions: 
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 * 
 */
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using Prism.Ioc;
using Prism.Services.Dialogs;

namespace Cameca.CustomAnalysis.PythonCore;

internal static class InternalDialogService
{
	public static void ShowDialog<TView, TViewModel>(IDialogParameters? parameters, Action<IDialogResult> callback)
		where TView : FrameworkElement
		where TViewModel : IDialogAware
	{
		parameters ??= new DialogParameters();
		IDialogWindow dialogWindow = CreateDialogWindow<IDialogWindow>();
		ConfigureDialogWindowEvents(dialogWindow, callback);
		// Configure DialogWindow Content
		var view = ContainerLocator.Container.Resolve<TView>();
		var viewModel = ContainerLocator.Container.Resolve<TViewModel>();
		view.DataContext = viewModel;
		ConfigureDialogWindowProperties(dialogWindow, view, viewModel);

		viewModel.OnDialogOpened(parameters);

		dialogWindow.ShowDialog();
	}

	public static void ShowDialog<TView, TViewModel, TWindow>(IDialogParameters? parameters, Action<IDialogResult> callback)
		where TView : FrameworkElement
		where TViewModel : IDialogAware
		where TWindow : Window, IDialogWindow
	{
		parameters ??= new DialogParameters();
		IDialogWindow dialogWindow = CreateDialogWindow<TWindow>();
		ConfigureDialogWindowEvents(dialogWindow, callback);
		// Configure DialogWindow Content
		var view = ContainerLocator.Container.Resolve<TView>();
		var viewModel = ContainerLocator.Container.Resolve<TViewModel>();
		view.DataContext = viewModel;
		ConfigureDialogWindowProperties(dialogWindow, view, viewModel);

		viewModel.OnDialogOpened(parameters);

		dialogWindow.ShowDialog();
	}

	private static void ConfigureDialogWindowEvents(IDialogWindow dialogWindow, Action<IDialogResult> callback)
	{
		Action<IDialogResult>? requestCloseHandler = null;
		requestCloseHandler = (o) =>
		{
			dialogWindow.Result = o;
			dialogWindow.Close();
		};

		RoutedEventHandler? loadedHandler = null;
		loadedHandler = (o, e) =>
		{
			dialogWindow.Loaded -= loadedHandler;
			dialogWindow.GetDialogViewModel().RequestClose += requestCloseHandler;
		};
		dialogWindow.Loaded += loadedHandler;

		CancelEventHandler? closingHandler = null;
		closingHandler = (o, e) =>
		{
			if (!dialogWindow.GetDialogViewModel().CanCloseDialog())
				e.Cancel = true;
		};
		dialogWindow.Closing += closingHandler;

		EventHandler? closedHandler = null;
		closedHandler = (o, e) =>
		{
			dialogWindow.Closed -= closedHandler;
			dialogWindow.Closing -= closingHandler;
			dialogWindow.GetDialogViewModel().RequestClose -= requestCloseHandler;

			dialogWindow.GetDialogViewModel().OnDialogClosed();

			dialogWindow.Result ??= new DialogResult();

			callback?.Invoke(dialogWindow.Result);

			dialogWindow.DataContext = null;
			dialogWindow.Content = null;
		};
		dialogWindow.Closed += closedHandler;
	}

	private static IDialogWindow CreateDialogWindow<TWindow>() where TWindow : IDialogWindow
	{
		return ContainerLocator.Container.Resolve<TWindow>();
	}

	private static void ConfigureDialogWindowProperties(IDialogWindow window, FrameworkElement dialogContent, IDialogAware viewModel)
	{
		var windowStyle = Dialog.GetWindowStyle(dialogContent);
		if (windowStyle != null)
			window.Style = windowStyle;

		window.Content = dialogContent;
		window.DataContext = viewModel; //we want the host window and the dialog to share the same data context

		if (window.Owner == null)
			window.Owner = Application.Current?.Windows.OfType<Window>().FirstOrDefault(x => x.IsActive);
	}
}

internal static class DialogWindowExtensions
{
	public static IDialogAware GetDialogViewModel(this IDialogWindow dialogWindow) => (IDialogAware)dialogWindow.DataContext;

}
