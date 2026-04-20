using Cameca.CustomAnalysis.PythonCore.Python.Rpc.Models;
using System;
using System.IO.MemoryMappedFiles;

namespace Cameca.CustomAnalysis.PythonCore.Python.Rpc.MemMap;


public sealed record MemMapEntry(MemoryMappedFile Mmf, BufferDef Def) : IDisposable
{
	public void Dispose()
	{
		Mmf.Dispose();
	}
}
