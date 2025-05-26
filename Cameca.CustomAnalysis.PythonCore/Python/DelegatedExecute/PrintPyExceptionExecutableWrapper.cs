using System.Threading;
using Python.Runtime;

namespace Cameca.CustomAnalysis.PythonCore;

public class PrintPyExceptionExecutableWrapper<T> : IPyExecutable where T : IPyExecutable
{
	private readonly T _inner;

	public PrintPyExceptionExecutableWrapper(T inner)
	{
		_inner = inner;
	}

	public PyObject? Execute(PyModule scope, CancellationToken token)
	{
		try
		{
			return _inner.Execute(scope, token);
		}
		catch (PythonException ex)
		{
			var traceback = scope.Import("traceback");
			traceback.InvokeMethod("print_exception",
				ex.Type,  //  Changed in version 3.5: The etype argument is ignored and inferred from the type of value.
				FormatValue(ex.Value, ex.Type),
				ex.Traceback ?? PyObject.None);
		
			static PyObject FormatValue(PyObject? value, PyType type)
			{
				if (value is null) return PyObject.None;
				if (value.HasAttr("__cause__")) return value;
				if (PyTuple.IsTupleType(value)) return type.Invoke(new PyTuple(value));
				if (PyList.IsListType(value)) return type.Invoke(new PyList(value));
				return type.Invoke(value);
			}
			// Rethrow so post-processing doesn't occur on Python exception
			throw;
		}
	}
}
