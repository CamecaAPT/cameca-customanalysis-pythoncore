using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using Microsoft.Extensions.Logging;
using StreamJsonRpc;
using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Cameca.CustomAnalysis.PythonCore;

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

	[JsonRpcMethod("analysisId")]
	public string AnalysisId() => resources.Id.ToString();

	[JsonRpcMethod("analysisTree")]
	public AnalysisTreeNode AnalysisTree()
	{
		return BuildAnalysisTreeNodeRecursive(resources.TopLevelNode);

		static AnalysisTreeNode BuildAnalysisTreeNodeRecursive(INodeResource node)
		{
			return new AnalysisTreeNode(
				node.Id.ToString(),
				node.Name,
				node.Title,
				node.DataSectionName,
				node.TypeId,
				node.IonDataOwnerNode.Id.ToString(),
				node.Children.Select(BuildAnalysisTreeNodeRecursive).ToList());
		}
	}

	[JsonRpcMethod("getDataTypes")]
	public List<string> GetDataTypes(string nodeId)
	{
		if (Guid.Parse(nodeId) is Guid guidId
			&& GetNodeById(resources, guidId) is INodeResource node)
		{
			return node.ProvidedDataTypes.Select(x => x.ToString()).ToList();
		}
		return new List<string>();
	}

	[JsonRpcMethod("getData")]
	public object? GetData(string nodeId, string type)
	{
		if (Guid.Parse(nodeId) is Guid guidId
			&& GetNodeById(resources, guidId) is INodeResource node
			&& node.ProvidedDataTypes.FirstOrDefault(x => x.ToString() == type) is Type parsed
			&& node.GetType().GetMethod(nameof(INodeResource.GetData)) is MethodInfo method
			&& method.MakeGenericMethod(parsed) is MethodInfo generic)
		{
			var data = generic.Invoke(node, [null, default(CancellationToken)]);
			return data;
		}
		return null;
	}

	[JsonRpcMethod("massSpectrumData")]
	public MassSpectrumData? GetMassSpectrumData(string? nodeId)
	{
		Guid? guidId = nodeId is null ? resources.Id : (Guid.TryParse(nodeId, out var g) ? g : null);
		if (guidId.HasValue
			&& GetNodeById(resources, guidId.Value) is INodeResource node
			&& node.GetMassSpectrum()?.GetData<IMassSpectrumData>() is { } massSpecData)
		{

			return new MassSpectrumData(
				MapToHistogramData(massSpecData.MassHistogram),
				MapToHistogramData(massSpecData.BackgroundModel));
		}
		return null;

		HistogramData MapToHistogramData(IHistogramData histData) => new HistogramData(
			histData.Start,
			histData.BinWidth,
			WriteToMemMapArray(histData.Values));
	}

	[JsonRpcMethod("getRanges")]
	public List<IonRange> GetRanges(string? nodeId)
	{
		Guid? guidId = nodeId is null ? resources.Id : (Guid.TryParse(nodeId, out var g) ? g : null);
		if (guidId.HasValue
			&& GetNodeById(resources, guidId.Value) is INodeResource node
			&& node.RangeManager is IMassSpectrumRangeManager rangeManager)
		{
			return rangeManager.GetIonRanges().Select(r => new IonRange(
				r.Name,
				r.Formula.ToDictionary(kv => kv.Key, kv => kv.Value),
				r.Volume,
				r.Min,
				r.Max,
				new SerializedColor(r.Color.ScR, r.Color.ScG, r.Color.ScB, r.Color.ScA)))
				.ToList();
		}
		return new List<IonRange>();
	}

	[JsonRpcMethod("setRanges")]
	public bool SetRanges(List<IonRange> ranges, string? nodeId)
	{
		Guid? guidId = nodeId is null ? resources.Id : (Guid.TryParse(nodeId, out var g) ? g : null);
		if (guidId.HasValue
			&& GetNodeById(resources, guidId.Value) is INodeResource node
			&& node.RangeManager is IMassSpectrumRangeManager rangeManager
			&& rangeManager.IsEditable)
		{
			return rangeManager.SetIonRangesSync(ranges.Select(x => new IonTypeInfoRange(
				x.Name,
				ToIonFormula(x.Formula),
				x.Volume,
				x.Min,
				x.Max,
				System.Windows.Media.Color.FromScRgb(x.Color.A, x.Color.R, x.Color.G, x.Color.B))));
		}
		return false;

		static IonFormula ToIonFormula(IDictionary<string, int> formulaDict)
		{
			var components = new List<IonFormula.Component>();
			foreach (var (atom, count) in formulaDict)
			{
				components.Add(new IonFormula.Component(atom, count));
			}
			return new IonFormula(components);
		}
	}

	[JsonRpcMethod("dataState")]
	public DataState DataStateValid(string? nodeId = null)
	{
		Guid? guidId = nodeId is null ? resources.Id : (Guid.TryParse(nodeId, out var g) ? g : null);
		if (!guidId.HasValue)
		{
			throw new ArgumentException($"No node with ID \"{nodeId}\" could be found", nameof(nodeId));
		}
		// Due to incorrect shape of IResources, DataState can only be retrieved from the owner node. Need to move state retrieval to INodeResources for proper scoping.
		else if (guidId.Value != resources.Id)
		{
			throw new NotSupportedException("Currently not supported to retrieve data state for nodes that are not this extension analysis node");
		}
		return new DataState(resources.DataState.IsValid, resources.DataState.IsErrorState);
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

	private MemMapArrayInfo WriteToMemMapArray<T>(ReadOnlyMemory<T> memory) where T : struct
	{
		string id = Guid.NewGuid().ToString();
		long count = (long)memory.Length;
		int valuesPerRecord = 1;
		var typeStrResolver = TypeStr.For<T>();
		var (typeStr, shape) = typeStrResolver switch
		{
			TypeResolver.Fixed fixedResolver => (fixedResolver.TypeStr, new long[] { count, valuesPerRecord }),
			TypeResolver.String stringResolver => throw new InvalidOperationException($"{nameof(WriteToMemMapArray)} can not write and array of string values to a memory map"),
			_ => throw new InvalidOperationException($"Unexpected TypeResolver subclass {typeStrResolver.GetType()}"),
		};
		var bufferDef = new BufferDef(typeStr, shape);
		var mmf = MemoryMappedFile.CreateNew(id, bufferDef.Capacity);
		memMapStore.Set(id, new(mmf, bufferDef));

		using var stream = mmf.CreateViewStream(0, bufferDef.Capacity, MemoryMappedFileAccess.Write);
		stream.Write(MemoryMarshal.AsBytes(memory.Span));

		return new MemMapArrayInfo(id, bufferDef);
	}

	private INodeResource? GetNodeById(IResources resources, Guid nodeId)
	{
		return EnumerateNodeResources(resources.TopLevelNode).FirstOrDefault(x => x.Id == nodeId);


		static IEnumerable<INodeResource> EnumerateNodeResources(INodeResource node)
		{
			yield return node;
			foreach (var child in node.Children)
			{
				foreach (var recurse in EnumerateNodeResources(child))
				{
					yield return recurse;
				}
			}
		}
	}
}
