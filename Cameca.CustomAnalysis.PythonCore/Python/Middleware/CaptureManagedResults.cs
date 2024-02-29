using System;
using System.Threading;
using Python.Runtime;

namespace Cameca.CustomAnalysis.PythonCore;

public delegate PyObject? ResultTransformer(PyObject? results);

public class CaptureManagedResults<T> : IPyExecutorMiddleware
{
	private T? _value = default;
	public T? Value
	{
		get => _value;
		private set
		{
			_value = value;
			HasResult = true;
		}
	}

	public bool HasResult { get; private set; } = false;

	private readonly Func<PyObject?, T?> mapFunc;
	private readonly ResultTransformer? resultTransformer;

	public CaptureManagedResults(Func<PyObject?, T?>? mapFunc = null, ResultTransformer? resultTransformer = null)
	{
		this.mapFunc = mapFunc ?? DefaultMapFunc;
		this.resultTransformer = resultTransformer;
	}

	private static T? DefaultMapFunc(PyObject? value) => value is not null ? value.As<T>() : default;

	public void Preprocess(PyModule scope, CancellationToken token) { }

	public void PostProcess(PyModule scope, PyObject? results, CancellationToken token)
	{
		if (results is null)
		{
			return;
		}

		try
		{
			Value = mapFunc(resultTransformer is not null ? resultTransformer(results) : results);
		}
		catch
		{
			// Pass
		}
	}

	public void Finalize(PyModule scope) { }
}
