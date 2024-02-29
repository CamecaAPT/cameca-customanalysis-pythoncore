using Python.Runtime;

namespace Cameca.CustomAnalysis.PythonCore;

public interface IPyObjectProvider
{
	PyObject GetPyObject(PyModule scope);
}
