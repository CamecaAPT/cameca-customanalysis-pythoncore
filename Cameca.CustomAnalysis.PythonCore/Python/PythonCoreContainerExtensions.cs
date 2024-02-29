using Prism.Ioc;

namespace Cameca.CustomAnalysis.PythonCore.Python;

public static class PythonCoreContainerExtensions
{
	public static IContainerRegistry RegisterPythonCore(this IContainerRegistry registry)
	{
		if (!registry.IsRegistered<IPythonManager>())
		{
			registry.RegisterSingleton<IPythonManager, PythonManager>();
			registry.RegisterSingleton<IPyExecutor, PyExecutor>();
		}
		return registry;
	}
}
