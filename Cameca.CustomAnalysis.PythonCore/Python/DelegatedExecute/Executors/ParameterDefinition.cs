using System;

namespace Cameca.CustomAnalysis.PythonCore;

public class ParameterDefinition
{
	public string Name { get; }

	public IPyObjectProvider ValueProvider { get; }

	public ParameterDefinition(string name, IPyObjectProvider valueProvider)
	{
		Name = PythonSyntax.IsValidIdentifier(name)
			? name
			: throw new ArgumentException(string.Format(Resources.InvalidPythonIdentifierExceptionMessage, name), nameof(name));
		ValueProvider = valueProvider;
	}
}
