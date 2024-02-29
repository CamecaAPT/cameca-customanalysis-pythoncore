using System;
using System.Collections.Generic;

namespace Cameca.CustomAnalysis.PythonCore;

public class FunctionWrapper : EntryFunctionDefinition
{
	private const string DefaultIndent = "\t";

	public string Script { get; }

	public string Indent { get; }

	public FunctionWrapper(string script, IEnumerable<ParameterDefinition>? positionalArguments = null, IEnumerable<ParameterDefinition>? keywordArguments = null, string? functionName = null, string? indent = null)
		: base(positionalArguments, keywordArguments, functionName)
	{
		Script = script;
		indent ??= DefaultIndent;
		Indent = PythonSyntax.IsValidIndent(indent)
			? indent :
			throw new ArgumentException(string.Format(Resources.InvalidPythonIndentationExceptionMessage, indent), nameof(indent));
	}
}
