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
			CreateBufferDef(sectionInfo).TypeStr);
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

		string id = Guid.NewGuid().ToString();
		var bufferDef = CreateBufferDef(section);
		var mmf = MemoryMappedFile.CreateNew(id, bufferDef.Capacity);
		memMapStore.Set(id, new(mmf, bufferDef));

		using var stream = mmf.CreateViewStream(0, bufferDef.Capacity, MemoryMappedFileAccess.Write);
		foreach (var chunk in ionData.CreateSectionDataEnumerable(sectionName))
		{
			var secBytes = chunk.ReadSectionData<byte>(sectionName);
			stream.Write(secBytes.Span);
		}

		return new MemMapArrayInfo(id, bufferDef);
	}

	[JsonRpcMethod("nodes")]
	public AnalysisTreeNode Nodes()
	{
		return BuildAnalysisTreeNodeRecursive(resources.TopLevelNode);

		static AnalysisTreeNode BuildAnalysisTreeNodeRecursive(INodeResource node)
		{
			return new AnalysisTreeNode(node.Id.ToString(), node.Children.Select(BuildAnalysisTreeNodeRecursive).ToList());
		}
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

	private BufferDef CreateBufferDef(ISectionInfo sectionInfo)
	{
		long count = (long)sectionInfo.RecordCount;
		int valuesPerRecord = (int)sectionInfo.ValuesPerRecord;
		long valueCount = count * valuesPerRecord;
		var valueBytes = sectionInfo.DataTypeSizeBits / 8;
		var recordBytes = valueBytes * valuesPerRecord;
		long capacity = recordBytes * count;
		// Incorrect API nullability, this will always be non-null
		var typeStrResolver = TypeStr.For(sectionInfo.Type!);
		var (typeStr, shape) = typeStrResolver switch
		{
			TypeResolver.Fixed fixedResolver => (fixedResolver.TypeStr,  new long[] { count, valuesPerRecord }),
			TypeResolver.String stringResolver => (stringResolver.Resolve((long)sectionInfo.RecordCount), new long[] { 1 }),
			_ => throw new InvalidOperationException($"Unexpected TypeResolver subclass {typeStrResolver.GetType()}"),
		};
		return new BufferDef(typeStr, shape);
	}
}
