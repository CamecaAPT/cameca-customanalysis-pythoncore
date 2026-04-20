using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace Cameca.CustomAnalysis.PythonCore.Python.Rpc.Models;

public record BufferDef(string TypeStr, long[] Shape)
{
	public long Size => Shape.Aggregate(1L, (a, b) => a * b);
	public Type Type => Rpc.TypeStr.Parse(TypeStr);
	public long Capacity => Size * Marshal.SizeOf(Type);

}
