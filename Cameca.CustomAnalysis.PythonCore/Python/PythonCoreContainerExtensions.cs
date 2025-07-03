using Prism.Ioc;

namespace Cameca.CustomAnalysis.PythonCore;

public static class PythonCoreContainerExtensions
{
	public static IContainerRegistry RegisterPythonCore(this IContainerRegistry registry)
	{
		if (!registry.IsRegistered<IPythonManager>())
		{
			registry.RegisterSingleton<IPythonManager, PythonManager>();
			registry.RegisterSingleton<IPyExecutor, PyExecutor>();
		}
		registry.RegisterOnce<PythonService>();
		registry.RegisterOnce<DistributionNotFoundDialogView>();
		registry.RegisterOnce<DistributionNotFoundDialogViewModel>();
		registry.RegisterOnce<DistributionNotFoundDialogWindow>();
		return registry;
	}

	public static IContainerRegistry RegisterOnce<T>(this IContainerRegistry registry)
	{
		if (!registry.IsRegistered<T>())
		{
			registry.Register<T>();
		}
		return registry;
	}
}
