using System;
using System.IO.MemoryMappedFiles;

namespace Cameca.CustomAnalysis.PythonCore;


public sealed record MemMapEntry(MemoryMappedFile Mmf, BufferDef Def) : IDisposable
{
	public void Dispose()
	{
		Mmf.Dispose();
	}
}
