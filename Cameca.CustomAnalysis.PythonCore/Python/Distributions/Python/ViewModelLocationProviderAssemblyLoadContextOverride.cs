using Prism.Mvvm;
using System;
using System.Linq;
using System.Reflection;

namespace Cameca.CustomAnalysis.PythonCore.Python.Distributions.Python;

internal static class ViewModelLocationProviderAssemblyLoadContextOverride
{
	public static void Register()
	{
		var wrapper = CreateWrapper(
			GetPrivateDefaultViewModelFactoryWithViewParameter(),
			GetPrivateDefaultViewModelFactory());
		ViewModelLocationProvider.SetDefaultViewModelFactory(wrapper);
	}

	private static Func<object, Type, object>? GetPrivateDefaultViewModelFactoryWithViewParameter()
	{
		var m = typeof(ViewModelLocationProvider).GetMember("_defaultViewModelFactoryWithViewParameter", BindingFlags.Static | BindingFlags.NonPublic);
		return ((FieldInfo)(m.Single())).GetValue(null) as Func<object, Type, object>;
	}

	private static Func<Type, object> GetPrivateDefaultViewModelFactory()
	{
		var m = typeof(ViewModelLocationProvider).GetMember("_defaultViewModelFactory", BindingFlags.Static | BindingFlags.NonPublic);
		return (Func<Type, object>)((FieldInfo)(m.Single())).GetValue(null)!;
	}

	private static Func<object, Type, object> CreateWrapper(
		Func<object, Type, object>? defaultViewModelFactoryWithViewParameter,
		Func<Type, object> defaultViewModelFactory)
	{
		return (object view, Type viewModelType) =>
		{
			if (view.GetType().ToString() == typeof(PythonLocatorDialogView).ToString())
			{
				viewModelType = typeof(PythonLocatorDialogViewModel);
			}
			return defaultViewModelFactoryWithViewParameter is not null
					? defaultViewModelFactoryWithViewParameter(view, viewModelType)
					: defaultViewModelFactory(viewModelType);
		};
	}
}
