using Cameca.CustomAnalysis.Interface;
using Prism.Ioc;
using System.Reflection;

namespace Cameca.CustomAnalysis.PythonCore;

public static class RpcPythonContainerExtensions
{
	public static IContainerRegistry RegisterRpcPython(
		this IContainerRegistry registry)
	{
		// All distributions should call this for common service registration
		registry.RegisterPythonCore();

		registry.Register<PythonSessionFactory>();

		return registry;
	}

	public static IContainerProvider InitializeRpcPython(this IContainerProvider provider, string optionsDisplayName)
	{
		var extensionRegistry = provider.Resolve<IExtensionRegistry>();
		var uniqueModelId = Assembly.GetCallingAssembly().GetName().Name;
		extensionRegistry.RegisterOptions<PythonRpcOptions>(
			optionsDisplayName,
			// Using calling assembly ensures each implementing extension gets its own saved options instance
			uniqueModelIdentifier: Assembly.GetCallingAssembly().GetName().Name);
		return provider;
	}
}
