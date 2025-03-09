using Prism.Services.Dialogs;
using System.Windows.Input;

namespace Cameca.CustomAnalysis.PythonCore;
/// <summary>
/// Interaction logic for AnacondaNotFoundDialogWindow.xaml
/// </summary>
public partial class AnacondaNotFoundDialogWindow : IDialogWindow
{
	public AnacondaNotFoundDialogWindow()
	{
		InitializeComponent();
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
