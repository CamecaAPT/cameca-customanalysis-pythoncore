using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.PythonCore.Python.Rpc.MemMap;
using Cameca.CustomAnalysis.PythonCore.Python.Rpc.Models;
using Cameca.CustomAnalysis.Utilities;
using Microsoft.Extensions.Logging;
using StreamJsonRpc;
using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Threading.Tasks;

namespace Cameca.CustomAnalysis.PythonCore.Python.Rpc;

public class HostCallbacks
{
    private readonly ILogger logger;
    private readonly IResources resources;
	private readonly MemMapStore memMapStore;

	public HostCallbacks(ILogger logger, IResources resources, MemMapStore? memMapStore = null)
    {
        this.logger = logger;
        this.resources = resources;
		this.memMapStore = memMapStore ?? new MemMapStore();
	}

    [JsonRpcMethod("log")]
    public void Log(HostLogRecord logRecord)
	{
		using (logger.BeginScope(logRecord.CreateLogContext()))
		{
			logger.Log(logRecord.MsLogLevel, logRecord.Message);
		}
	}

	[JsonRpcMethod("filename")]
	public string Filename()
	{
		return resources.TopLevelNode.GetValidIonData()?.Filename ?? "";
	}

	[JsonRpcMethod("sections")]
	public List<string> Sections()
	{
		return resources.TopLevelNode.GetValidIonData()?.Sections.Keys.ToList() ?? new List<string>();
	}

	[JsonRpcMethod("sectionInfo")]
	public async Task<SectionInfo?> SectionInfo(string sectionName)
	{
		if (await resources.GetIonData() is not IIonData ionData
			|| ionData.Sections[sectionName] is not ISectionInfo sectionInfo)
		{
			return null;
		}
		return new SectionInfo(
			sectionInfo.Unit,
			sectionInfo.IsProtected,
			sectionInfo.IsVirtual,
			(long)sectionInfo.RecordCount,
			(int)sectionInfo.ValuesPerRecord,
			TypeStr.For(sectionInfo.Type!));  // Incorrect API nullability, this will always be non-null
	}

	[JsonRpcMethod("sectionData")]
	public async Task<MemMapArrayInfo?> SectionData(string sectionName)
	{
		var ionData = await resources.GetIonData()
			?? throw new InvalidOperationException("Could not resolve IonData");

		if (!ionData.Sections.TryGetValue(sectionName, out var section))
		{
			return null;
		}

		long count = (long)ionData.IonCount;
		int valuesPerRecord = (int)section.ValuesPerRecord;
		long valueCount = count * valuesPerRecord;
		var valueBytes = section.DataTypeSizeBits / 8;
		var recordBytes = valueBytes * valuesPerRecord;
		long capacity = recordBytes * count;
		string id = Guid.NewGuid().ToString();

		var mmf = MemoryMappedFile.CreateNew(id, capacity);
		// Type shouldn't be null: the underlying implementation isn't nullable
		// Possible error in interface type, or null support might only be for creation
		var bufferDef = new BufferDef(TypeStr.For(section.Type!), [count, valuesPerRecord]);
		memMapStore.Set(id, new(mmf, bufferDef));

		using var stream = mmf.CreateViewStream(0, capacity, MemoryMappedFileAccess.Write);
		foreach (var chunk in ionData.CreateSectionDataEnumerable(sectionName))
		{
			var secBytes = chunk.ReadSectionData<byte>(sectionName);
			stream.Write(secBytes.Span);
		}

		return new MemMapArrayInfo(id, bufferDef);
	}

	[JsonRpcMethod("mmap.alloc")]
	public async Task<string> MemMapAlloc(BufferDef mmapDef)
	{
		string id = Guid.NewGuid().ToString();

		var mmf = MemoryMappedFile.CreateNew(id, mmapDef.Capacity);
		memMapStore.Set(id, new(mmf, mmapDef));
		return id;
	}

	[JsonRpcMethod("mmap.dispose")]
	public async Task MemMapDispose(string id)
	{
		memMapStore.Release(id);
	}
}
