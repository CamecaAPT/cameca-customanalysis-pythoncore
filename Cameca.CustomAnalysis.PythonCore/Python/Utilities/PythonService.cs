using Microsoft.Extensions.Logging;

namespace Cameca.CustomAnalysis.PythonCore;

/// <summary>
/// Service that creates <see cref="PythonFunction"/> instances that can be used to streamline
/// the process of mapping to and calling a Python function
/// </summary>
public class PythonService
{
	private readonly IPyExecutor pyExecutor;
	private readonly ILogger<PythonService> logger;

	/// <inheritdoc cref="PythonService"/>
	public PythonService(IPyExecutor pyExecutor, ILogger<PythonService> logger)
	{
		this.pyExecutor = pyExecutor;
		this.logger = logger;
	}

	/// <inheritdoc cref="PythonFunction" />
	public PythonFunction MapPythonFunction(string moduleName, string funcName)
	{
		return new PythonFunction(moduleName, funcName, pyExecutor, logger);
	}
}
