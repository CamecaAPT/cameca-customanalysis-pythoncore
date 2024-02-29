using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cameca.CustomAnalysis.PythonCore;

public record PythonEntryFunctionArgs(PyObject[] PositionalArguments, PyDict? KeywordArguments);

public class EntryFunctionDefinition
{
	private const string DefaultFunctionName = "main";

	public string FunctionName { get; }

	public IReadOnlyList<ParameterDefinition> PositionalArguments { get; }

	public IReadOnlyList<ParameterDefinition> KeywordArguments { get; }

	public EntryFunctionDefinition(IEnumerable<ParameterDefinition>? positionalArguments = null, IEnumerable<ParameterDefinition>? keywordArguments = null, string? functionName = null)
	{
		functionName ??= DefaultFunctionName;
		FunctionName = PythonSyntax.IsValidIdentifier(functionName)
			? functionName
			: throw new ArgumentException(string.Format(Resources.InvalidPythonIdentifierExceptionMessage, functionName), nameof(functionName));
		PositionalArguments = positionalArguments?.ToList() ?? new();
		KeywordArguments = keywordArguments?.ToList() ?? new();
	}

	public PythonEntryFunctionArgs GetPythonArguments(PyModule scope)
	{
		var argValueList = new List<PyObject>();
		PyDict? kwargsDict = null;
		foreach (var paramDef in PositionalArguments)
		{
			argValueList.Add(paramDef.ValueProvider.GetPyObject(scope));
		}
		foreach (var paramDef in KeywordArguments)
		{
			// Lazy create dict only if keyword arguments are present
			kwargsDict ??= new PyDict();
			kwargsDict[paramDef.Name.ToPython()] = paramDef.ValueProvider.GetPyObject(scope);
		}
		return new PythonEntryFunctionArgs(argValueList.ToArray(), kwargsDict);
	}
}
