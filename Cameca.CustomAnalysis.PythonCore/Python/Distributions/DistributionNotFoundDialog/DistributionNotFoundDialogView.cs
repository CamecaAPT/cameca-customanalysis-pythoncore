using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace Cameca.CustomAnalysis.PythonCore;

internal class DistributionNotFoundDialogView : UserControl
{
	public DistributionNotFoundDialogView()
	{
		Resources = BuildResources();
		Content = BuildContent();
	}

	private static ResourceDictionary BuildResources() {
		Style infoTextBlockStyle = new Style(typeof(TextBlock))
		{
			Setters =
			{
				new Setter(FontSizeProperty, 14d),
				new Setter(HorizontalAlignmentProperty, HorizontalAlignment.Center),
			}
		};
		return new ResourceDictionary
		{
			["BorderStyle"] = new Style(typeof(Border))
			{
				Setters =
			{
				new Setter(Border.CornerRadiusProperty, new CornerRadius(8d)),
				new Setter(Border.BorderBrushProperty, new SolidColorBrush(Colors.Gray)),
				new Setter(Border.BorderThicknessProperty, new Thickness(1d)),
				new Setter(Border.BackgroundProperty, new SolidColorBrush(Colors.White)),
			}
			},
			["DownloadButtonStyle"] = new Style(typeof(Button))
			{
				Setters =
			{
				new Setter(ForegroundProperty, new SolidColorBrush(Colors.White)),
				new Setter(FontSizeProperty, 11d),
				new Setter(WidthProperty, 140d),
				new Setter(HeightProperty, 40d),
				new Setter(TemplateProperty, BuildDownloadButtonTemplate()),
				new Setter(EffectProperty, new DropShadowEffect
				{
					Color = Colors.DarkGray,
					Direction = 320d,
					ShadowDepth = 3d,
				})
			}
			},
			["CloseButtonStyle"] = new Style(typeof(Button))
			{
				Setters =
				{
					new Setter(MarginProperty, new Thickness(8d)),
					new Setter(HeightProperty, 25d),
					new Setter(WidthProperty, 60d),
					new Setter(BackgroundProperty, new SolidColorBrush(Colors.Transparent)),
					new Setter(BorderThicknessProperty, new Thickness(0d)),
					new Setter(FontWeightProperty, FontWeights.DemiBold),
					new Setter(ForegroundProperty, new SolidColorBrush(Colors.Gray)),
				}
			},
			["InfoTextBlockStyle"] = infoTextBlockStyle,
			["HeaderInfoTextBlockStyle"] = new Style(typeof(TextBlock), infoTextBlockStyle)
			{
				Setters =
				{
					new Setter(FontWeightProperty, FontWeights.Bold),
				}
			},
			["SelectableOnlyTextBoxStyle"] = new Style(typeof(TextBox))
			{
				Setters =
				{
					new Setter(FontSizeProperty, 14d),
					new Setter(HorizontalContentAlignmentProperty, HorizontalAlignment.Center),
					new Setter(TextBoxBase.IsReadOnlyProperty, true),
					new Setter(BackgroundProperty, new SolidColorBrush(Colors.Transparent)),
					new Setter(MarginProperty, new Thickness(8d)),
					new Setter(BorderThicknessProperty, new Thickness(0d)),
				}
			},
		};
	}

	private static ControlTemplate BuildDownloadButtonTemplate()
	{
		return new ControlTemplate(typeof(Button))
		{
			VisualTree = new FrameworkElementFactory(typeof(Border))
				.SetValueFefExt(BorderThicknessProperty, new Thickness(0d))
				.SetBindingFefExt(BackgroundProperty, new Binding(nameof(Background)) {
					Mode=BindingMode.OneTime,
					RelativeSource=RelativeSource.TemplatedParent,
				})
				.SetValueFefExt(Border.CornerRadiusProperty, new CornerRadius(20d))
			.AppendChildFefExt(
				new FrameworkElementFactory(typeof(ContentPresenter))
				.SetValueFefExt(HorizontalAlignmentProperty, HorizontalAlignment.Center)
				.SetValueFefExt(VerticalAlignmentProperty, VerticalAlignment.Center)),
		}.SealFrameworkTemplate();
	}

	private object BuildContent()
	{
		var brush = new SolidColorBrush();
		BindingOperations.SetBinding(brush, SolidColorBrush.ColorProperty, new Binding(nameof(DistributionNotFoundDialogViewModel.DistributionColor)) { Mode = BindingMode.OneWay });

		return new Border
		{
			Style = (Style)Resources["BorderStyle"],
			Child = new Grid
			{
				RowDefinitions =
				{
					new RowDefinition { Height = new GridLength(1d, GridUnitType.Star) },
					new RowDefinition { Height = GridLength.Auto },
					new RowDefinition { Height = GridLength.Auto },
				},
				Children =
				{
					new StackPanel
					{
						HorizontalAlignment = HorizontalAlignment.Stretch,
						VerticalAlignment = VerticalAlignment.Center,
						Children =
						{
							new TextBlock
							{
								Style = (Style)Resources["HeaderInfoTextBlockStyle"],
							}
							.SetBindingEx(TextBlock.TextProperty, new Binding(nameof(DistributionNotFoundDialogViewModel.DistributionLabel))
							{
								StringFormat = "No {0} distribution could be located.",
								Mode = BindingMode.OneWay,
							}),
							new TextBlock
							{
								Style = (Style)Resources["InfoTextBlockStyle"],
							}
							.SetBindingEx(TextBlock.TextProperty, new Binding(nameof(DistributionNotFoundDialogViewModel.DistributionLabel))
							{
								StringFormat = "Please download and install {0} to continue.",
								Mode = BindingMode.OneWay,
							}),
							new TextBlock
							{
								Style = (Style)Resources["InfoTextBlockStyle"],
								Text = "Download link will open in your default browser."
							},
							new TextBox
							{
								Style = (Style)Resources["SelectableOnlyTextBoxStyle"]
							}
							.SetBindingEx(TextBox.TextProperty, new Binding(nameof(DistributionNotFoundDialogViewModel.DownloadUrl))
							{
								Mode = BindingMode.OneWay,
							}),
						},
					}
					.SetGridRow(0),
					new Button
					{
						Content = "Download",
						Style = (Style)Resources["DownloadButtonStyle"],
						Background = brush,
					}
					.SetBindingEx(ButtonBase.CommandProperty, nameof(DistributionNotFoundDialogViewModel.DownloadCommand))
					.SetGridRow(1),
					new Button
					{
						Content = "Close",
						Style = (Style)Resources["CloseButtonStyle"],
					}
					.SetBindingEx(ButtonBase.CommandProperty, nameof(DistributionNotFoundDialogViewModel.CancelCommand))
					.SetGridRow(2),
				}
			}
		};
	}
}
