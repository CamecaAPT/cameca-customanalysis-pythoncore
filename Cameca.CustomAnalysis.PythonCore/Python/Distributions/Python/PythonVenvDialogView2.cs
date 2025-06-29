using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Input;
using Prism.Services.Dialogs;

namespace Cameca.CustomAnalysis.PythonCore;

internal class PythonVenvDialogView2 : UserControl
{
	public PythonVenvDialogView2()
	{
		Resources = BuildResources();
		Dialog.SetWindowStyle(this, CreateWindowStyle());
		Dialog.SetWindowStartupLocation(this, WindowStartupLocation.CenterScreen);
		Content = BuildContent();
	}

	private static ResourceDictionary BuildResources() => new ResourceDictionary
	{
		["InvertBooleanConverter"] = new InvertBooleanConverter(),
	};

	private static Style CreateWindowStyle() => new Style(typeof(Window))
	{
		Setters =
		{
			new Setter(Window.TitleProperty, new Binding(nameof(PythonVenvDialogViewModel2.Title))),
			new Setter(Window.ResizeModeProperty, ResizeMode.NoResize),
			new Setter(Window.ShowInTaskbarProperty, false),
			new Setter(Window.SizeToContentProperty, SizeToContent.WidthAndHeight),
			new Setter(Window.WindowStyleProperty, WindowStyle.ToolWindow),
		},
	};

	private object BuildContent()
	{
		var gridResources = new ResourceDictionary
		{
			["TextBoxStyle"] = new Style(typeof(TextBox))
			{
				Setters =
				{
					new Setter(TextBoxBase.IsReadOnlyProperty, true),
					new Setter(FontFamilyProperty, new FontFamily("Consolas")),
					new Setter(BackgroundProperty, Brushes.Transparent),
					new Setter(TextBoxBase.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto),
					new Setter(TextBox.TextWrappingProperty, TextWrapping.Wrap),
				},
			},
			[typeof(Button)] = new Style(typeof(Button))
			{
				Setters =
				{
					new Setter(WidthProperty, 80d),
					new Setter(PaddingProperty, new Thickness(5d)),
				},
			},
		};
		return new Grid
		{
			Width = 800d,
			Height = 650d,
			RowDefinitions =
			{
				new RowDefinition { Height = GridLength.Auto, },
				new RowDefinition { Height = GridLength.Auto, },
				new RowDefinition { Height = GridLength.Auto, },
				new RowDefinition { Height = new GridLength(1d, GridUnitType.Star) },
				new RowDefinition { Height = GridLength.Auto, },
			},
			Resources = gridResources,
			Children =
			{
				new TextBox
					{
						Style = (Style)gridResources["TextBoxStyle"],
					}
					.SetBindingEx(TextBox.TextProperty, new Binding(nameof(PythonVenvDialogViewModel2.CmdStatementText)) { Mode = BindingMode.OneWay })
					.SetGridRow(0),
				new UniformGrid
					{
						HorizontalAlignment = HorizontalAlignment.Center,
						Columns = 3,
						Children =
						{
							new Button
							{
								Content = "Create",
							}
							.SetBindingEx(ButtonBase.CommandProperty, nameof(PythonVenvDialogViewModel2.RunCommand)),
							new Button
							{
								Content = "Delete",
							}
							.SetBindingEx(ButtonBase.CommandProperty, nameof(PythonVenvDialogViewModel2.DeleteCommand)),
							new Button
							{
								Content = "Cancel",
							}
							.SetBindingEx(ButtonBase.CommandProperty, nameof(PythonVenvDialogViewModel2.RunCancelCommand)),
						}
					}
					.SetGridRow(1),
				new TextBlock
					{
						Text = "Creating the virtual environment might take some time. Please be patient and wait for the success message."
					}
					.SetGridRow(2),
				new AppendingTextBox
					{
						AppendScrollBehavior = AppendScrollBehavior.FollowBottom,
						Style = (Style)gridResources["TextBoxStyle"],
					}
					.SetBindingEx(AppendingTextBox.ItemsSourceProperty, nameof(PythonVenvDialogViewModel2.TextContent))
					.SetGridRow(3),
				new UniformGrid
					{
						Columns = 2,
						Rows = 1,
						HorizontalAlignment = HorizontalAlignment.Right,
						Children =
						{
							new Button
								{
									Content = "Ok",
								}
								.SetBindingEx(ButtonBase.CommandProperty, nameof(PythonVenvDialogViewModel2.OkCommand))
								.SetBindingEx(IsEnabledProperty, new Binding($"{nameof(PythonVenvDialogViewModel2.RunCommand)}.{nameof(IAsyncRelayCommand.IsRunning)}")
								{
									Converter = (IValueConverter)Resources["InvertBooleanConverter"],
								}),
							new Button
								{
									Content = "Cancel",
								}
								.SetBindingEx(ButtonBase.CommandProperty, nameof(PythonVenvDialogViewModel2.CancelCommand)),
						}
					}
					.SetGridRow(4),
			}
		};
	}
}
