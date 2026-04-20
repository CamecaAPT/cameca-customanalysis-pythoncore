using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace Cameca.CustomAnalysis.PythonCore.Python.Rpc.Models;

public sealed record BufferDef(string TypeStr, long[] Shape)
{
	[JsonIgnore]
	public long Size => Shape.Aggregate(1L, (a, b) => a * b);
	[JsonIgnore]
	public Type Type => Rpc.TypeStr.Parse(TypeStr).Type;
	[JsonIgnore]
	public long Capacity => Size * Rpc.TypeStr.Parse(TypeStr).ItemSize;

}
