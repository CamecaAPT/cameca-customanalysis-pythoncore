using Cameca.CustomAnalysis.PythonCore.Python.Rpc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Cameca.CustomAnalysis.PythonCore;

public sealed class PythonPeerProcess : IDisposable
{
    private readonly object _gate = new();
    private readonly ProcessStartInfo _processStartInfo;
    private readonly bool _keepConsoleOpen;

    private Process? _process;
    private bool _stopped;
    private bool _disposed;

    public PythonPeerProcess(PythonScriptInfo scriptInfo, ConsoleOptions? consoleOptions = null)
    {
        consoleOptions ??= new ConsoleOptions();
        _keepConsoleOpen = consoleOptions.ShowConsole && !consoleOptions.CloseOnShutdown;
        _processStartInfo = BuildProcessStartInfo(scriptInfo, consoleOptions);
    }

    public static PythonPeerProcess Start(PythonScriptInfo scriptInfo, ConsoleOptions? consoleOptions = null)
    {
        var pythonPeerProcess = new PythonPeerProcess(scriptInfo, consoleOptions);
        pythonPeerProcess.Start();
        return pythonPeerProcess;
    }

    public void Start()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            // Already running - no-op
            if (_process is not null)
            {
                return;
            }

            var process = new Process
            {
                StartInfo = _processStartInfo,
            };

            if (!process.Start())
            {
				process.Dispose();
                throw new InvalidOperationException("Failed to start python process.");
			}

			_process = process;
        }
    }

    /// <summary>
    /// Attempt to cleanly shut down the managed process.
    /// </summary>
    /// <param name="timeout">If not provided, defaults to 5 seconds</param>
    public void Stop(TimeSpan? timeout = null)
    {
        Process? process;
        lock (_gate)
        {
            if (_disposed || _stopped ||  _process is null || _keepConsoleOpen)
            {
                return;
            }
            _stopped = true;
            process = _process;
        }
        StopCore(process, timeout ?? TimeSpan.FromSeconds(5));
    }

    private static void StopCore(Process process, TimeSpan timeout)
    {
        // Check if already exited
        try
        {
            if (process.HasExited)
            {
                return;
            }
        }
        catch
        {
            // Process not valid or already disposed
            return;
        }

        // Wait for graceful exit
        if (process.WaitForExit((int)timeout.TotalMilliseconds))
        {
            return;
        }

        // Force kill
        try
        {
            process.Kill(entireProcessTree: true);

            // Wait for kill to complete with a bounded timeout
            process.WaitForExit(2000);
        }
        catch
        {
            // Best effort - process may already be gone
        }
    }

    static ProcessStartInfo BuildProcessStartInfo(PythonScriptInfo scriptInfo, ConsoleOptions? consoleOptions = null)
    {
		consoleOptions ??= new ConsoleOptions();

		if (!Directory.Exists(scriptInfo.WorkingDirectory))
        {
            throw new ArgumentException("WorkingDirectory must be an existing directory", nameof(scriptInfo));
        }

        var workingDirectory = scriptInfo.WorkingDirectory;
        var pythonExePath = FullyQualifiedPath(scriptInfo.PythonExePath, scriptInfo.WorkingDirectory);

        ProcessStartInfo psi;
        if (!consoleOptions.ShowConsole)
        {
            psi = new ProcessStartInfo
            {
                FileName = $"\"{pythonExePath}\"",
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
			BuildArgumentList(psi, scriptInfo.PythonArgs);
		}
        else
        {
            psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                WorkingDirectory = workingDirectory,
                UseShellExecute = true,
                CreateNoWindow = true,
				Arguments = $"/s {(consoleOptions.CloseOnShutdown ? "/c" : "/k")} \"{(CreateArgumentsString(new string[] { scriptInfo.PythonExePath }.Concat(scriptInfo.PythonArgs)))}\"",
			};
			var args = CreateArgumentsString(new string[] { scriptInfo.PythonExePath }.Concat(scriptInfo.PythonArgs));

		}
        return psi;
    }
	
	// Ignore the double quote in value situration: it gets complicated, and in practice this is only going to be used for file paths that can't have that anyways
	static string QuoteArg(string arg) => arg.IndexOfAny(new[] { ' ', '\t', '\n', '\v'}) >= 0 ? $"\"{arg}\"" : arg;

	static string CreateArgumentsString(IEnumerable<string> pythonArgs)
	{
		return string.Join(" ", pythonArgs.Select(QuoteArg));
	}

	static void BuildArgumentList(ProcessStartInfo psi, string[] pythonArgs)
	{
		foreach (var arg in pythonArgs)
		{
			psi.ArgumentList.Add(arg);
		}
	}

    /// <summary>
    /// Returns the fully qualified path by combining the specified path with the working directory if the path is not
    /// already fully qualified.
    /// </summary>
    /// <param name="path">The relative or absolute path to be converted to a fully qualified path. If this path is already rooted, it will
    /// be returned as is.</param>
    /// <param name="workingDirectory">The working directory to use for constructing the fully qualified path if the specified path is relative.</param>
    /// <returns>A string representing the fully qualified path. If the input path is fully qualified, it returns the path unchanged;
    /// otherwise, it combines the working directory with the path.</returns>
    static string FullyQualifiedPath(string path, string workingDirectory)
    {
        if (Path.IsPathFullyQualified(path))
        {
            return path;
        }
        return Path.Join(workingDirectory, path);
    }

    public void Dispose()
    {
        Process? processToStop;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            processToStop = (!_stopped && !_keepConsoleOpen) ? _process : null;
            _stopped = true;
        }

        if (processToStop is not null)
        {
            StopCore(processToStop, TimeSpan.FromSeconds(5));
        }

        lock (_gate)
        {
            _process?.Dispose();
            _process = null;
        }
    }
}

/// <summary>
/// Defines the necessary information to launch a Python process hosting an <c>IPythonApi</c> implementation.
/// </summary>
/// <param name="WorkingDirectory"></param>
/// <param name="PythonExePath">If not an absolute path, relative to the provided working directory</param>
/// <param name="PythonFilePath">If not an absolute path, relative to the provided working directory</param>
/// <param name="PythonArgs">Passed to <see cref="ProcessStartInfo.ArgumentList"/></param>
public sealed record PythonScriptInfo(
    string WorkingDirectory,
    string PythonExePath,
    string[] PythonArgs);


/// <param name="ShowConsole">Show the console window of the launched Python process</summary>
/// <param name="CloseOnShutdown">Close a displayed console window when the Python process is terminated: only applicable if <see cref="ConsoleOptions.ShowConsole"/>is <c>true</c></summary>
public sealed record ConsoleOptions(
    bool ShowConsole = false,
    bool CloseOnShutdown = true);

public sealed record LogOptions(
	PythonSupportedLogLevel MainLevel = PythonSupportedLogLevel.Information,
	PythonSupportedLogLevel RpcLevel = PythonSupportedLogLevel.Warning);
