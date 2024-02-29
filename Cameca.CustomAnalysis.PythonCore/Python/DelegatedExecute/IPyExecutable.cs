using Python.Runtime;
using System.Threading;

namespace Cameca.CustomAnalysis.PythonCore;

public interface IPyExecutable
{
	PyObject? Execute(PyModule scope, CancellationToken token);
}
