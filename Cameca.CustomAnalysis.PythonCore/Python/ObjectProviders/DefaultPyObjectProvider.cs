using Python.Runtime;

namespace Cameca.CustomAnalysis.PythonCore;

public class DefaultPyObjectProvider : IPyObjectProvider
{
	private readonly object? value;

	public DefaultPyObjectProvider(object? value)
	{
		this.value = value;
	}

	public PyObject GetPyObject(PyModule scope) => value?.ToPython() ?? PyObject.None;
}
