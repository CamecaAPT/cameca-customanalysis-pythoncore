using Prism.Ioc;
using Prism.Mvvm;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Cameca.CustomAnalysis.PythonCore;

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
			Type viewType = view.GetType();
			Assembly callingAssembly = Assembly.GetCallingAssembly();
			// Check if view comes from the calling ALC
			if (AssemblyLoadContext.GetLoadContext(viewType.Assembly) == AssemblyLoadContext.GetLoadContext(callingAssembly))
			{
				if (view.GetType().ToString() == typeof(PythonLocatorDialogView2).ToString())
				{
					using (AssemblyLoadContext.EnterContextualReflection(callingAssembly))
					{
						return ContainerLocator.Container.Resolve<PythonLocatorDialogViewModel>();
					}
				}
				else if (view.GetType().ToString() == typeof(PythonVenvDialogView2).ToString())
				{
					using (AssemblyLoadContext.EnterContextualReflection(callingAssembly))
					{
						return ContainerLocator.Container.Resolve<PythonVenvDialogViewModel>();
					}
				}
			}
			return defaultViewModelFactoryWithViewParameter is not null
					? defaultViewModelFactoryWithViewParameter(view, viewModelType)
					: defaultViewModelFactory(viewModelType);
		};
	}
}
