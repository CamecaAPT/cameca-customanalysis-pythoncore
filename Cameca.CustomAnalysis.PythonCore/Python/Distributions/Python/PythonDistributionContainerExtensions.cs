using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Prism.Ioc;
using Cameca.CustomAnalysis.Interface;
using System.Reflection;

namespace Cameca.CustomAnalysis.PythonCore;

public static class PythonDistributionContainerExtensions
{
	public static IContainerRegistry RegisterPythonDistribution(
		this IContainerRegistry registry)
	{
		// All distributions should call this for common service registration
		registry.RegisterPythonCore();

		// Expect an ILogger. Register a NullLogger if not registered in calling application
		if (!registry.IsRegistered<ILogger>())
		{
			registry.Register(typeof(ILogger<>), typeof(NullLogger<>));
		}

		registry.RegisterSingleton<IPyDistribution, PythonDistribution>(nameof(PythonDistribution));
		registry.RegisterDialog<PythonLocatorDialogView, PythonLocatorDialogViewModel>("PythonLocatorDialog");

		return registry;
	}

	public static IContainerProvider InitializePythonDistribution(this IContainerProvider provider, string optionsDisplayName)
	{
		var extensionRegistry = provider.Resolve<IExtensionRegistry>();
		var uniqueModelId = Assembly.GetCallingAssembly().GetName().Name;
		extensionRegistry.RegisterOptions<PythonOptions>(
			optionsDisplayName,
			// Using calling assembly ensures each implementing extension gets its own saved options instance
			uniqueModelIdentifier: Assembly.GetCallingAssembly().GetName().Name);
		return provider;
	}
}
