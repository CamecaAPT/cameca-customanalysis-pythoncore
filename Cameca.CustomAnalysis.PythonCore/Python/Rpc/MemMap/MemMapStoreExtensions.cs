using System;
using System.IO.MemoryMappedFiles;

namespace Cameca.CustomAnalysis.PythonCore;

public static class MemMapStoreExtensions
{
	public static MemMapArrayInfo Create<T>(this MemMapStore store, ReadOnlyMemory<T> memory) where T : unmanaged
	{
		if (TypeStr.For<T>() is not TypeResolver.Fixed fixedResolver)
		{
			throw new ArgumentException("ReadOnlyMemory must have a fixed type generic T", nameof(memory));
		}
		var bufferDef = new BufferDef(fixedResolver.TypeStr, [memory.Length]);
		var id = Guid.NewGuid().ToString();
		var mmf = MemoryMappedFile.CreateNew(id, bufferDef.Capacity);
		store.Set(id, new MemMapEntry(mmf, bufferDef));
		return new MemMapArrayInfo(id, bufferDef);
	}
}
