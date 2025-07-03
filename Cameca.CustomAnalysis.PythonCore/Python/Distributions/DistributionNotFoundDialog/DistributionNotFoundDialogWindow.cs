using Prism.Services.Dialogs;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Cameca.CustomAnalysis.PythonCore;

internal class DistributionNotFoundDialogWindow : Window, IDialogWindow
{
	public DistributionNotFoundDialogWindow()
	{
		WindowStyle = WindowStyle.None;
		ResizeMode = ResizeMode.NoResize;
		BorderThickness = new Thickness(1);
		AllowsTransparency = true;
		Background = new SolidColorBrush(Colors.Transparent);
		WindowStartupLocation = WindowStartupLocation.CenterOwner;
		ShowInTaskbar = false;
		MinHeight = 180;
		MinWidth = 500;
		MouseDown += OnMouseDown;
		SizeToContent = SizeToContent.WidthAndHeight;
	}

	public IDialogResult Result { get; set; } = new DialogResult();

	private void OnMouseDown(object sender, MouseButtonEventArgs e)
	{
		if (e.LeftButton == MouseButtonState.Pressed)
		{
			DragMove();
		}
	}
}
