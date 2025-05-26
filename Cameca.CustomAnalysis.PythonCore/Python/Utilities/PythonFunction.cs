using Microsoft.Extensions.Logging;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Cameca.CustomAnalysis.PythonCore;

/// <summary>
/// Provides a streamlined fluent interface for mapping and calling a Python function
/// </summary>
/// <remarks>
/// Provides an easy hook for setting a callback for optional std stream redirect.
/// Provides a simple method for setting positional parameters with a params object[] method that
/// either pass directly if implementing <see cref="IPyObjectProvider"/> or automatically wraps the
/// value in the <see cref="DefaultPyObjectProvider"/>.
/// Returns <see cref="PyObject?"/> with all the results of the function that can be subsequently
/// parsed into CLR objects.
/// </remarks>
/// <exception cref="OperationCanceledException">The <see cref="CancellationToken"/> is cancelled</exception>
/// <exception cref="PythonException">The Python code threw an exception and did not complete sucessfully</exception>
public class PythonFunction
{
	private readonly string moduleName;
	private readonly string funcName;
	private readonly IPyExecutor pyExecutor;
	private readonly ILogger<PythonService> logger;
	private Action<object?>? stdwriteCallback = null;
	private object[] parameters = Array.Empty<object>();
	private bool reloadOnCall = false;

	/// <inheritdoc cref="PythonFunction" />
	public PythonFunction(string moduleName, string funcName, IPyExecutor pyExecutor, ILogger<PythonService> logger)
	{
		this.moduleName = moduleName;
		this.funcName = funcName;
		this.pyExecutor = pyExecutor;
		this.logger = logger;
	}

	public PythonFunction SetStdStreamCallback(Action<object?> stdwriteCallback)
	{
		this.stdwriteCallback = stdwriteCallback;
		return this;
	}

	public PythonFunction SetParameters(params object[] parameters)
	{
		this.parameters = parameters;
		return this;
	}

	public PythonFunction SetReloadOnCall(bool reloadOnCall)
	{
		this.reloadOnCall = reloadOnCall;
		return this;
	}

	public async Task<PyObject?> Call(CancellationToken cancellationToken = default)
	{
		var captureResultsMiddleware = new CaptureManagedResults<PyObject?>(value => value);
		var middleware = new List<IPyExecutorMiddleware>()
		{
			captureResultsMiddleware,
		};
		if (stdwriteCallback is not null)
		{
			middleware.Add(new StdstreamRedirect(stdwriteCallback));
		}
		var paramDefs = parameters
			.Select((value, i) => new ParameterDefinition($"_{i}", value is IPyObjectProvider provider ? provider : new DefaultPyObjectProvider(value)))
			.ToArray();
		var entryFunction = new EntryFunctionDefinition(
			paramDefs,
			functionName: funcName);
		var executableModule = new LocalModuleExecutable(moduleName, entryFunction, reloadModule: reloadOnCall);

		var wrapper = new PrintPyExceptionExecutableWrapper<LocalModuleExecutable>(executableModule);
		await pyExecutor.Execute(wrapper, middleware, cancellationToken);
		return captureResultsMiddleware.Value;
	}
}
