using StreamJsonRpc;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Cameca.CustomAnalysis.PythonCore;

public sealed class RpcPeer<THostCallbacks, TPythonApi> : IDisposable
	where THostCallbacks : class
	where TPythonApi : class, IPythonApiBase
{
    private readonly object _gate = new();
    private readonly THostCallbacks callbacks;
    private ManualResetEventSlim _proxyReady = new();

    private TcpListener? _listener;
    private TcpClient? _client;
    private NetworkStream? _stream;
    private JsonRpc? _rpc;
    private TPythonApi? _proxy;

    private CancellationTokenSource? _runCts;
    private Thread? _runThread;


    public string Address { get; private set; } = "";
    public int Port { get; private set; }

    public RpcPeer(THostCallbacks callbacks)
    {
        this.callbacks = callbacks;
    }

    public void Start()
    {
        lock (_gate)
        {
            if (_runThread is not null)
            {
                throw new InvalidOperationException("Server already started.");
            }

            _runCts = new CancellationTokenSource();

            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();

            Address = ((IPEndPoint)_listener.LocalEndpoint).Address.ToString();
            Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
            Debug.WriteLine($"Listening on {Address}:{Port}...");

            _runThread = new Thread(RunLoop)
            {
                IsBackground = true,
                Name = "RpcPeer",
            };
            _runThread.Start();
        }
    }

    private void RunLoop()
    {
        TcpClient client;
        try
        {
            client = _listener!.AcceptTcpClient();
        }
        catch
        {
            // listener was stopped during Dispose before any client connected
            return;
        }

        _client = client;
        _stream = client.GetStream();

        var formatter = new SystemTextJsonFormatter();
		formatter.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        var handler = new LengthHeaderMessageHandler(_stream, _stream, formatter);

        using var rpc = new JsonRpc(handler);
        _rpc = rpc;

        // Register callbacks/targets on this side
        rpc.AddLocalRpcTarget(callbacks);
        rpc.StartListening();

        try
        {
            _proxy = rpc.Attach<TPythonApi>();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            throw;
        }
        _proxyReady.Set();

        // Exit the thread on lost connection
        rpc.Disconnected += (_s, _e) =>
        {
            Debug.WriteLine($"Disconnected: cancelling");
            _runCts?.Cancel();
            Debug.WriteLine($"Disconnected: cancelled");
        };

        try
        {
            // Keep session alive until canceled or connection lost
            _runCts!.Token.WaitHandle.WaitOne();
        }
        finally
        {
            _rpc = null;
        }
    }


    // Call this from Main/WPF after StartAsync to get the typed proxy.
    public TPythonApi GetProxy(TimeSpan? timeout = null)
    {
        if (!_proxyReady.Wait(timeout ?? TimeSpan.FromSeconds(30)))
        {
            throw new TimeoutException("Timed out waiting for Python client to connect.");
        }
        return _proxy!;
    }

    private void CloseConnection()
    {
        // Ask the remote to shut down gracefully before closing the connection.
        if (_proxy is not null)
        {
            // Fire and forget: ask the remote to shut down, but don't block waiting for a response
            var res = Task.Run(async () =>
			{
				try
				{
					await _proxy.ShutdownAsync();
				}
				catch (ObjectDisposedException)
				{
					// Pass -- if already disposed, there's nothing more possible to do to clean up
					// TODO: This shouldn't actually happen in normal operation, so once a logger gets in here add a logger.Warn
				}
			}).Wait(TimeSpan.FromSeconds(2));
            Debug.WriteLine($"Comleted ShutdownAsync call: {res}");
        }

        _runCts?.Cancel();

        // Force any pending Accept/Read to break.
        try { _listener?.Stop(); } catch { }
        try { _stream?.Close(); } catch { }
        try { _client?.Close(); } catch { }
    }

    public void Stop()
    {
        Thread? runThread;
        lock (_gate)
        {
            runThread = _runThread;
            if (runThread is null)
                return;
        }

        CloseConnection();
        runThread.Join(TimeSpan.FromSeconds(5));
        Cleanup();
    }

    private void Cleanup()
    {
        lock (_gate)
        {
            _runThread = null;
            _runCts?.Dispose();
            _runCts = null;

            _rpc = null;
            _proxy = default;

            try { _stream?.Dispose(); } catch { }
            _stream = null;

            try { _client?.Dispose(); } catch { }
            _client = null;

            try { _listener?.Stop(); } catch { }
            _listener = null;
        }
    }

    public void Dispose() => Stop();
}
