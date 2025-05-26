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
		if (!registry.IsRegistered<PythonService>())
		{
			registry.Register<PythonService>();
		}
		return registry;
	}
}
