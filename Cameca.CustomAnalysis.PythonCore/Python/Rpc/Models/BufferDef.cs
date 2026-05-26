using System;
using System.Linq;
using System.Text.Json.Serialization;

namespace Cameca.CustomAnalysis.PythonCore;

public sealed record BufferDef(string TypeStr, long[] Shape)
{
	[JsonIgnore]
	public long Size => Shape.Aggregate(1L, (a, b) => a * b);
	[JsonIgnore]
	public Type Type => PythonCore.TypeStr.Parse(TypeStr).Type;
	[JsonIgnore]
	public long Capacity => Size * PythonCore.TypeStr.Parse(TypeStr).ItemSize;

}
