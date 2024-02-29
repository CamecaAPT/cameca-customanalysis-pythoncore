namespace Cameca.CustomAnalysis.PythonCore;

public class AnacondaDistributionOptions
{
	/// <summary>
	/// Name of the Anaconda environment to try to activate. Superseded by `name` in a valid <see cref="EnvironmentFilePath"/> if <see cref="TryCreateEnvironment"/> is set
	/// </summary>
	public string? Environment { get; init; } = null;

	/// <summary>
	/// Path to a yml file that defines an anaconda environment
	/// </summary>
	public string? EnvironmentFilePath { get; init; } = null;

	/// <summary>
	/// If <see cref="EnvironmentFilePath"/> is not <c>null</c> but environment could not be resolved, try to fallback to a base Anaconda environment
	/// </summary>
	public bool FallbackToBase { get; init; } = false;

	/// <summary>
	/// If <see cref="EnvironmentFilePath"/> is not <c>null</c> but environment could not be resolved, try to create an environment and try again
	/// </summary>
	public bool TryCreateEnvironment { get; init; } = false;

	/// <summary>
	/// Show a prompt with a link to download the required Anaconda distribution if it could not be resolved on the user machine.
	/// </summary>
	public bool ShowDownloadPrompt { get; init; } = true;
}
