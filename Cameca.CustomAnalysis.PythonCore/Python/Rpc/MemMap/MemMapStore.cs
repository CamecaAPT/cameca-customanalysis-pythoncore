using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Cameca.CustomAnalysis.PythonCore.Python.Rpc.MemMap;

public sealed class MemMapStore : IDisposable
{
	private Dictionary<string, MemMapEntry> memMaps = new();
	private bool disposedValue;

	public bool Contains(string key) => memMaps.ContainsKey(key);

	public void Set(string key, MemMapEntry value)
	{
		// If overwriting an existing entry, dispose the old one first
		if (memMaps.ContainsKey(key))
		{
			memMaps[key].Dispose();
		}
		memMaps[key] = value;
	}

	public T[]? Get<T>(string key) where T : struct
	{
		if (memMaps.ContainsKey(key))
		{
			return ReadMmapAsArray<T>(key);
		}
		return null;
	}

	public void Release(string key)
	{
		if (memMaps.TryGetValue(key, out var memMap))
		{
			memMap.Dispose();
			memMaps.Remove(key);
		}
	}

	private T[] ReadMmapAsArray<T>(string id) where T : struct
	{
		if (!memMaps.TryGetValue(id, out var mmap))
		{
			throw new ArgumentException($"Must refer to an allocated memory map", nameof(id));
		}

		long bytes = mmap.Def.Capacity;
		int itemSize = Marshal.SizeOf<T>();

		// Log a real warning or handle in some way.
		Debug.Assert(bytes % itemSize == 0, "Type should be aligned with the memmap type size");

		int valueCount = (int)(bytes / itemSize);


		T[] buffer = new T[valueCount];
		using (var accessor = mmap.Mmf.CreateViewAccessor(0, mmap.Def.Capacity))
		{
			accessor.ReadArray<T>(0, buffer, 0, valueCount);
		}
		return buffer;
	}


	private void Dispose(bool disposing)
	{
		if (!disposedValue)
		{
			if (disposing)
			{
				foreach(var memMap in memMaps.Values)
				{
					memMap.Dispose();
				}
			}
			disposedValue = true;
		}
	}

	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
