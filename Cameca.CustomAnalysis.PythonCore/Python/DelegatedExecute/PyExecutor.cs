using Python.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Cameca.CustomAnalysis.PythonCore;

internal class PyExecutor : IPyExecutor
{
	private readonly TimeSpan cancellationPollInterval = TimeSpan.FromMilliseconds(200);
	private readonly TimeSpan cancellationTimeout = TimeSpan.FromSeconds(5);

	private readonly IPythonManager pythonManager;

	public PyExecutor(IPythonManager pythonManager)
	{
		this.pythonManager = pythonManager;
	}

	public async Task Execute(IPyExecutable executable, IEnumerable<IPyExecutorMiddleware> middleware, CancellationToken token)
	{
		if (!pythonManager.Initialize())
		{
			return;
		}

		var middlewaresList = middleware.ToList();

		using var allowThreads = new PyAllowThreads();
		long pythonThreadId = long.MinValue;

		var task = Task.Run(TaskRunner, token);
		// Inner function that will be run on a separate task for cancellation support
		void TaskRunner()
		{
			var extensionDirectory = new DirectoryInfo(Assembly.GetExecutingAssembly().Location).Parent?.FullName;
			using var _dir = WorkingDirectory.Enter(extensionDirectory);
			using var _ = Py.GIL();
			Interlocked.Exchange(ref pythonThreadId, (long)PythonEngine.GetPythonThreadID());
			using var scope = Py.CreateScope();
			try
			{
				foreach (var item in middlewaresList)
				{
					item.Preprocess(scope, token);
					if (token.IsCancellationRequested)
					{
						return;
					}
				}
				var results = executable.Execute(scope, token);
				if (token.IsCancellationRequested)
				{
					return;
				}

				// Post-process hooks
				// Iterate in reverse order for nested middleware by priority
				for (int i = middlewaresList.Count - 1; i >= 0; i--)
				{
					middlewaresList[i].PostProcess(scope, results, token);
					if (token.IsCancellationRequested)
					{
						return;
					}
				}
			}
			finally
			{
				// Iterate in reverse order for nested middleware by priority
				for (int i = middlewaresList.Count - 1; i >= 0; i--)
				{
					middlewaresList[i].Finalize(scope);
				}
			}
		}

		// Execute with cancellation check polling. Send Python interrupt on cancel
		Task pollTask;
		while (true)
		{
			pollTask = await Task.WhenAny(task, Task.Delay(cancellationPollInterval, token));

			if (task.IsCompleted)
			{
				break;
			}
			// If cancellation request was triggered, interrupt the running Python thread
			if (token.IsCancellationRequested)
			{
				using (Py.GIL())
				{
					PythonEngine.Interrupt((ulong)Interlocked.Read(ref pythonThreadId));
				}
				break;
			}
		}
		// If task is cancelled and interrupt was sent, then await cleanup and return
		// Do not pass cancellation token to timeout task. Only should complete due to timeout
		// and only relevant after cancellation already has been triggered.
		// ReSharper disable once MethodSupportsCancellation
		var finalizeTask = await Task.WhenAny(pollTask, Task.Delay(cancellationTimeout));
		await finalizeTask;
	}
}
