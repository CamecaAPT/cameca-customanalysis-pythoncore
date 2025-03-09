using System.Buffers;

namespace Cameca.CustomAnalysis.PythonCoreLib;

public static class Unsafe
{
	public unsafe static IntPtr ToIntPtr(MemoryHandle handle) => new IntPtr(handle.Pointer);
}
