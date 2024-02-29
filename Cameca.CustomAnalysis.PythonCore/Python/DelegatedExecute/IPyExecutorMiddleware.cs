using Python.Runtime;
using System.Threading;

namespace Cameca.CustomAnalysis.PythonCore;

public interface IPyExecutorMiddleware
{
	void Preprocess(PyModule scope, CancellationToken token);

	void PostProcess(PyModule scope, PyObject? results, CancellationToken token);

	void Finalize(PyModule scope);
}
