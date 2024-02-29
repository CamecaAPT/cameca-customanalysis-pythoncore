using System;
using System.Runtime.InteropServices;
using Python.Runtime;

namespace Cameca.CustomAnalysis.PythonCore;

public class CaptureFilterIndices : CaptureManagedResults<ReadOnlyMemory<ulong>[]>
{
	public CaptureFilterIndices(ResultTransformer? resultTransformer = null) : base(ToEnumerableIndices, resultTransformer) {}

	private static ReadOnlyMemory<ulong>[] ToEnumerableIndices(PyObject? value)
	{
		if (value is null)
		{
			return new ReadOnlyMemory<ulong>[] { Array.Empty<ulong>() };
		}
		else
		{
			// Assume a valid numpy array of np.uint64
			var kwargs = new PyDict();
			kwargs["copy".ToPython()] = false.ToPython();
			using PyBuffer pybuf_array = value.InvokeMethod("astype", new PyObject[] { "uint64".ToPython() }, kwargs).GetAttr("data").GetBuffer();
			var len = pybuf_array.Length;
			byte[] managedArray = new byte[len];
			pybuf_array.Read(managedArray, 0, managedArray.Length, 0);
			var converted = MemoryMarshal.Cast<byte, ulong>(managedArray).ToArray();

			//var converted = value.As<ulong[]>();
			return new ReadOnlyMemory<ulong>[] { converted };
		}
		
	}
}
