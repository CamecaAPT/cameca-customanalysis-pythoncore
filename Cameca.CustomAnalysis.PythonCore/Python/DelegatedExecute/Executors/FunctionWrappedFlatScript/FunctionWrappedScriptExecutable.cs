using Python.Runtime;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Cameca.CustomAnalysis.PythonCore;

public class FunctionWrappedScriptExecutable : IPyExecutable
{
	// Used for result transformer
	// See _script_warpper.script_wrapper
	private const string ResultMethodName = "_run_results";

	private readonly FunctionWrapper functionWrapper;

	public FunctionWrappedScriptExecutable(FunctionWrapper functionWrapper)
	{
		this.functionWrapper = functionWrapper;
	}

	public PyObject? Execute(PyModule scope, CancellationToken token)
	{
		var wrapper = scope.Import("_script_wrapper");
		var entryFunctionArgs = functionWrapper.GetPythonArguments(scope);
		var argNameList = functionWrapper.PositionalArguments.Select(x => x.Name).ToList();
		var kwargsNameList = functionWrapper.KeywordArguments.Select(x => x.Name).ToList();

		var pyIndent = new PyString(functionWrapper.Indent);
		var runFuncWrappedScript = wrapper
			.InvokeMethod("script_wrapper",
				new PyString(functionWrapper.Script),
				new PyString($"def {functionWrapper.FunctionName}({string.Join(", ", argNameList.Concat(kwargsNameList))})"),
				pyIndent)
			.As<string>();

		var moduleName = GetModuleName(runFuncWrappedScript);
		var module = PyModule.FromString(moduleName, runFuncWrappedScript);
		// Current implementation has any value returned from the top level of the script stored in "_run_results"
		// which can then be accessed through the locals parameter of any included middleware
		return module.InvokeMethod(
			functionWrapper.FunctionName,
			entryFunctionArgs.PositionalArguments,
			entryFunctionArgs.KeywordArguments);
	}

	public static PyObject? ResultTransformer(PyObject? results)
	{
		return results is not null ? results[ResultMethodName] : null;
	}

	/// <summary>
	/// Generate module name from hash of script content
	/// </summary>
	/// <remarks>
	/// Python caches modules by name. By generating a hashed name by content, changes will generate a new module.
	/// Because the hash is derived from content, reverting changes to previous scripts will load from cache.
	/// </remarks>
	/// <param name="script"></param>
	/// <returns></returns>
	private static string GetModuleName(string script)
	{
		// Does not need to by cryptographically secure, but basic testing indicated
		// SHA-256 was fastest. Presumably hardware accelerated on modern hardware.
		using var sha256 = SHA256.Create();
		var encodedBytes = Encoding.UTF8.GetBytes(script);
		var hashBytes = sha256.ComputeHash(encodedBytes);
		// Ensure module name doesn't start with a number
		return $"_{Convert.ToHexString(hashBytes)}";
	}
}
