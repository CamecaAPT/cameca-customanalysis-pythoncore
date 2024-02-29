using Cameca.CustomAnalysis.PythonCore.Python;
using Cameca.CustomAnalysis.PythonScript.Python.Distributions.Anaconda.AnacondaNotFoundDialog;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Prism.Ioc;

namespace Cameca.CustomAnalysis.PythonCore;

public static class AnacondaDistributionContainerExtensions
{
	/// <summary>
	/// Register use of an Anaconda Python distribute. If not resolved on the user machine, prompt for installation.
	/// </summary>
	/// <param name="registry"></param>
	/// <param name="environment"></param>
	/// <param name="fallbackToBase"></param>
	/// <returns></returns>
	public static IContainerRegistry RegisterAnacondaDistribution(
		this IContainerRegistry registry,
		string? environment = null,
		string? envFilePath = null,
		bool fallbackToBase = false,
		bool tryCreateEnvironment = false,
		bool showDownloadPrompt = true)
	{
		// All distributions should call this for common service registration
		registry.RegisterPythonCore();

		// Expect an ILogger. Register a NullLogger if not registered in calling application
		if (!registry.IsRegistered<ILogger>())
		{
			registry.Register(typeof(ILogger<>), typeof(NullLogger<>));
		}

		// Allow multiple registrations only for different environments
		registry.RegisterDialogWindow<AnacondaNotFoundDialogWindow>(nameof(AnacondaNotFoundDialogWindow));
		registry.RegisterDialog<AnacondaNotFoundDialogView, AnacondaNotFoundDialogViewModel>();
		registry.RegisterSingleton<AnacondaRegistryResolver>();
		registry.RegisterSingleton<AnacondaAutoResolver>();
		registry.RegisterInstance(new AnacondaDistributionOptions
		{
			Environment = environment,
			EnvironmentFilePath = envFilePath,
			FallbackToBase = fallbackToBase,
			TryCreateEnvironment = tryCreateEnvironment,
			ShowDownloadPrompt = showDownloadPrompt,
		});
		registry.RegisterSingleton<IPyDistribution, AnacondaDistribution>(nameof(AnacondaDistribution));
		return registry;
	}
}
