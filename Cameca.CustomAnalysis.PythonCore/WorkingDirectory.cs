using System;
using System.IO;

namespace Cameca.CustomAnalysis.PythonCore;

internal sealed class WorkingDirectory : IDisposable
{
	private readonly string originalDirectory;
	private bool disposed;

	private WorkingDirectory(string? newDirectory)
	{
		originalDirectory = Directory.GetCurrentDirectory();
		if (!string.IsNullOrEmpty(newDirectory))
		{
			Directory.SetCurrentDirectory(newDirectory);
		}
	}

	public static WorkingDirectory Enter(string? newDirectory)
	{
		return new WorkingDirectory(newDirectory);
	}

	public void Dispose()
	{
		if (!disposed)
		{
			Directory.SetCurrentDirectory(originalDirectory);
			disposed = true;
		}
	}
}
