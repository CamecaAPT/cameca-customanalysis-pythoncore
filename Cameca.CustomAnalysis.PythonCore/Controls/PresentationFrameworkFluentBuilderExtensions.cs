using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Cameca.CustomAnalysis.PythonCore;

internal static class PresentationFrameworkFluentBuilderExtensions
{
	/// <inheritdoc cref="Grid.SetRow"/>
	public static T SetGridRow<T>(this T element, int value) where T : UIElement
	{
		Grid.SetRow(element, value);
		return element;
	}

	/// <inheritdoc cref="Grid.SetColumn"/>
	public static T SetGridColumn<T>(this T element, int value) where T : UIElement
	{
		Grid.SetColumn(element, value);
		return element;
	}

	/// <inheritdoc cref="Grid.SetIsSharedSizeScope"/>
	public static T SetGridIsSharedSizeScope<T>(this T element, bool value) where T : UIElement
	{
		Grid.SetIsSharedSizeScope(element, value);
		return element;
	}

	/// <inheritdoc cref="FrameworkElement.SetBinding(DependencyProperty, string)"/>
	public static T SetBindingEx<T>(this T element, DependencyProperty dp, string path) where T : FrameworkElement
	{
		element.SetBinding(dp, path);
		return element;
	}

	/// <inheritdoc cref="FrameworkElement.SetBinding(DependencyProperty, BindingBase)"/>
	public static T SetBindingEx<T>(this T element, DependencyProperty dp, BindingBase binding) where T : FrameworkElement
	{
		element.SetBinding(dp, binding);
		return element;
	}

	/// <inheritdoc cref="FrameworkElementFactory.SetBinding(DependencyProperty, BindingBase)"/>
	public static T SetValueFefExt<T>(this T factory, DependencyProperty dp, object value) where T : FrameworkElementFactory
	{
		factory.SetValue(dp, value);
		return factory;
	}

	/// <inheritdoc cref="FrameworkElementFactory.SetBinding(DependencyProperty, BindingBase)"/>
	public static T SetBindingFefExt<T>(this T factory, DependencyProperty dp, BindingBase binding) where T : FrameworkElementFactory
	{
		factory.SetBinding(dp, binding);
		return factory;
	}

	/// <inheritdoc cref="FrameworkTemplate.Seal"/>
	public static T SealFrameworkTemplate<T>(this T frameworkTemplate) where T : FrameworkTemplate
	{
		frameworkTemplate.Seal();
		return frameworkTemplate;
	}

	public static FrameworkElementFactory AppendChildFefExt(this FrameworkElementFactory factory, FrameworkElementFactory child)
	{
		factory.AppendChild(child);
		return factory;
	}
}
