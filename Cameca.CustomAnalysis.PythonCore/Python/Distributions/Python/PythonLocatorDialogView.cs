using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using Prism.Services.Dialogs;

namespace Cameca.CustomAnalysis.PythonCore;

internal class PythonLocatorDialogView : UserControl
{
	public PythonLocatorDialogView()
	{
		Dialog.SetWindowStyle(this, CreateWindowStyle());
		Dialog.SetWindowStartupLocation(this, WindowStartupLocation.CenterScreen);
		Content = BuildContent();
	}

	private static Style CreateWindowStyle() => new Style(typeof(Window))
	{
		Setters =
		{
			new Setter(Window.TitleProperty, new Binding(nameof(PythonLocatorDialogViewModel.Title))),
			new Setter(Window.ResizeModeProperty, ResizeMode.NoResize),
			new Setter(Window.ShowInTaskbarProperty, false),
			new Setter(Window.SizeToContentProperty, SizeToContent.WidthAndHeight),
			new Setter(Window.WindowStyleProperty, WindowStyle.ToolWindow),
		},
	};

	private static object BuildContent() => new Grid
	{
		RowDefinitions =
		{
			new RowDefinition { Height = new GridLength(1d, GridUnitType.Star) },
			new RowDefinition { Height = GridLength.Auto, },
		},
		Children =
		{
			new DataGrid
				{
					AutoGenerateColumns = true,
					IsReadOnly = true,
					SelectionMode = DataGridSelectionMode.Single,
					SelectionUnit = DataGridSelectionUnit.FullRow,
				}
				.SetBindingEx(ItemsControl.ItemsSourceProperty, nameof(PythonLocatorDialogViewModel.PythonInstances))
				.SetBindingEx(Selector.SelectedItemProperty, nameof(PythonLocatorDialogViewModel.SelectedInstallation))
				.SetGridRow(0),
			new UniformGrid
				{
					Columns = 2,
					Rows = 1,
					HorizontalAlignment = HorizontalAlignment.Right,
					Resources = new ResourceDictionary
					{
						[typeof(Button)] = new Style(typeof(Button))
						{
							Setters =
							{
								new Setter(WidthProperty, 80d),
								new Setter(PaddingProperty, new Thickness(5d)),
							},
						},
					},
					Children =
					{
						new Button
							{
								Content = "Ok",
							}
							.SetBindingEx(ButtonBase.CommandProperty, nameof(PythonLocatorDialogViewModel.OkCommand)),
						new Button
							{
								Content = "Cancel",
							}
							.SetBindingEx(ButtonBase.CommandProperty, nameof(PythonLocatorDialogViewModel.CancelCommand)),
					}
				}
				.SetGridRow(1),
		}
	};

	public override string ToString()
	{
		return typeof(PythonVenvDialogView).AssemblyQualifiedName ?? base.ToString();
	}
}
