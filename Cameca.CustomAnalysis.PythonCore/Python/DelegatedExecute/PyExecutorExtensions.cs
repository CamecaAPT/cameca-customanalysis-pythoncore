using Python.Runtime;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Cameca.CustomAnalysis.PythonCore;

public static class PyExecutorExtensions
{
	public static async Task<bool> ExecuteAndPrintExceptions<T>(
		this IPyExecutor pyExecutor,
		T executable,
		IEnumerable<IPyExecutorMiddleware>? middleware = null,
		CancellationToken cancellationToken = default)
		where T : IPyExecutable
	{
		try
		{
			var wrapper = new PrintPyExceptionExecutableWrapper<T>(executable);
			await pyExecutor.Execute(wrapper, middleware ?? Enumerable.Empty<IPyExecutorMiddleware>(), cancellationToken);
			return true;
		}
		catch (TaskCanceledException)
		{
			// Expected on cancellation
		}
		catch (PythonException)
		{
			// Handled internally for diaplay, but re-thrown to avoid treating as "successful" for post-processing
			// Capture here again and suppress, and exception details are reported to stderr
			// Potentially log in the future
		}
		return false;
	}
}
