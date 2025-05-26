using Python.Runtime;
using System.Threading;

namespace Cameca.CustomAnalysis.PythonCore;

public class LocalModuleExecutable : IPyExecutable
{
	private readonly string moduleName;
	private readonly EntryFunctionDefinition entryFunctionDefinition;
	private readonly bool reloadModule;

	public LocalModuleExecutable(string moduleName, EntryFunctionDefinition entryFunctionDefinition, bool reloadModule = false)
	{
		this.moduleName = moduleName;
		this.entryFunctionDefinition = entryFunctionDefinition;
		this.reloadModule = reloadModule;
	}

	public PyObject? Execute(PyModule scope, CancellationToken token)
	{
		var local_module = scope.Import(moduleName);
		if (reloadModule)
		{
			var importlib = scope.Import("importlib");
			local_module = importlib.InvokeMethod("reload", local_module);
		}
		var entryFunctionArgs = entryFunctionDefinition.GetPythonArguments(scope);
		return local_module.InvokeMethod(
			entryFunctionDefinition.FunctionName,
			entryFunctionArgs.PositionalArguments,
			entryFunctionArgs.KeywordArguments);
	}
}
