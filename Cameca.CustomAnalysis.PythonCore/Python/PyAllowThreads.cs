using Python.Runtime;
using System;

namespace Cameca.CustomAnalysis.PythonCore;

/// <summary>
/// Disposable wrapper around PythonEngine AllowThreads methods.
/// Calls <see cref="PythonEngine.BeginAllowThreads()" /> on creation and
/// <see cref="PythonEngine.EndAllowThreads(IntPtr)" /> on <see cref="IDisposable.Dispose()" />.
/// Utilization with <c>using</c> statements ensures
/// <see cref="PythonEngine.EndAllowThreads(IntPtr)" /> is always called.
/// </summary>
internal sealed class PyAllowThreads : IDisposable
{
	private readonly IntPtr threadState;

	public PyAllowThreads()
	{
		threadState = PythonEngine.BeginAllowThreads();
	}

	public void Dispose()
	{
		PythonEngine.EndAllowThreads(threadState);
	}
}
