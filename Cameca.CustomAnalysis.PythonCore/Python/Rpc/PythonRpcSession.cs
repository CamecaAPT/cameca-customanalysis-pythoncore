using Cameca.CustomAnalysis.PythonCore.Python.Rpc;
using System;
using System.IO;
using System.Reflection;
using System.Threading;

namespace Cameca.CustomAnalysis.PythonCore;

public class PythonRpcSession<THostCallbacks, TPythonApi> : IDisposable
	where THostCallbacks : class
	where TPythonApi : class, IPythonApiBase
{
    private readonly RpcPeer<THostCallbacks, TPythonApi> server;
    private readonly PythonPeerProcess pyProcess;

    public TPythonApi Remote { get; }

    public PythonRpcSession(RpcPeer<THostCallbacks, TPythonApi> server, PythonPeerProcess pyProcess, TPythonApi remote)
    {
        this.server = server;
        this.pyProcess = pyProcess;
        Remote = remote;
    }

    public static PythonRpcSession<THostCallbacks, TPythonApi> Start(
        string path,
        THostCallbacks callbacks,
        string? workingDirectory = null,
        string? pythonExePath = null,
        ConsoleOptions? consoleOptions = null,
		LogOptions? logOptions = null)
    {
        var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        workingDirectory ??= assemblyDir;
        pythonExePath ??= "venv\\Scripts\\extension-launch.exe";
		logOptions ??= new LogOptions();

		var server = new RpcPeer<THostCallbacks, TPythonApi>(callbacks);
        server.Start();

        var scriptInfo = new PythonScriptInfo(
            workingDirectory,
            pythonExePath,
            new string[] {
				path,
                "-H", server.Address,
                "-p", server.Port.ToString(),
				"--log", ToLogParam(logOptions.MainLevel),
				"--log-rpc", ToLogParam(logOptions.RpcLevel),
            });

        var pyProcess = PythonPeerProcess.Start(scriptInfo, consoleOptions);
        var remote = server.GetProxy();

        return new PythonRpcSession<THostCallbacks, TPythonApi>(server, pyProcess, remote);
    }

	static string ToLogParam(PythonSupportedLogLevel level) => level switch
	{
		PythonSupportedLogLevel.Debug => "DEBUG",
		PythonSupportedLogLevel.Information => "INFO",
		PythonSupportedLogLevel.Warning => "WARNING",
		PythonSupportedLogLevel.Error => "ERROR",
		PythonSupportedLogLevel.Critical => "CRITICAL",
		_ => throw new ArgumentOutOfRangeException(nameof(level), $"Unsupported log level: {level}")
	};

    public void Stop()
    {
        server.Stop();
        pyProcess.Stop(TimeSpan.FromSeconds(5));
    }

    // https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-dispose#base-class-with-managed-resources
    private int _isDisposed;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        Stop();

        // In case _isDisposed is 0, atomically set it to 1.
        // Enter the branch only if the original value is 0.
        if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 0)
        {
            if (disposing)
            {
                // Dispose managed state.
                server?.Dispose();
                pyProcess?.Dispose();
            }
        }
    }
}
