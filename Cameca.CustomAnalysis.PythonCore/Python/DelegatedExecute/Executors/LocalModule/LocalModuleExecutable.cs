using Python.Runtime;
using System.Threading;

namespace Cameca.CustomAnalysis.PythonCore;

public class LocalModuleExecutable : IPyExecutable
{
	private readonly string moduleName;
	private readonly EntryFunctionDefinition entryFunctionDefinition;

	public LocalModuleExecutable(string moduleName, EntryFunctionDefinition entryFunctionDefinition)
	{
		this.moduleName = moduleName;
		this.entryFunctionDefinition = entryFunctionDefinition;
	}

	public PyObject? Execute(PyModule scope, CancellationToken token)
	{
		var local_module = scope.Import(moduleName);
		var entryFunctionArgs = entryFunctionDefinition.GetPythonArguments(scope);
		return local_module.InvokeMethod(
			entryFunctionDefinition.FunctionName,
			entryFunctionArgs.PositionalArguments,
			entryFunctionArgs.KeywordArguments);
	}
}
