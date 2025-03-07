using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Cameca.CustomAnalysis.PythonCore;

internal class InvertBooleanConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		return value is bool boolValue ? !boolValue : DependencyProperty.UnsetValue;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}
}
