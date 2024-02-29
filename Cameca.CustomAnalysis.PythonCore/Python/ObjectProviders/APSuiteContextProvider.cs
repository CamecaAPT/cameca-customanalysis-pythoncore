using Cameca.CustomAnalysis.Interface;
using Prism.Ioc;
using Python.Runtime;
using System;
using System.Buffers;

namespace Cameca.CustomAnalysis.PythonCore;

public class APSuiteContextProvider : IPyObjectProvider
{
	private readonly IIonData ionData;
	private readonly INodeInfo? nodeInfo;
	private readonly IIonDisplayInfo? ionDisplayInfo;
	private readonly IMassSpectrumRangeManager? rangeManager;
	private readonly INodeProperties? properties;
	private readonly INodeElementDataSet? nodeElementDataSet;
	private readonly IElementDataSetService elementDataSetService;
	private readonly IReconstructionSections? reconstructionSections;
	private readonly IExperimentInfoResolver? experimentInfoResolver;

	public APSuiteContextProvider(
		IIonData ionData,
		IContainerProvider containerProvider,
		Guid instanceId)
	{
		this.ionData = ionData;
		this.ionDisplayInfo = containerProvider.Resolve<IIonDisplayInfoProvider>().Resolve(instanceId);
		// Only allow fetching and setting ranges from root level - no spatial ranging support for extensions
		this.nodeInfo = containerProvider.Resolve<INodeInfoProvider>().Resolve(instanceId);
		var rootNodeId = GetRootNodeId(containerProvider.Resolve<INodeInfoProvider>(), instanceId);
		this.rangeManager = containerProvider.Resolve<IMassSpectrumRangeManagerProvider>().Resolve(rootNodeId);
		this.properties = containerProvider.Resolve<INodePropertiesProvider>().Resolve(instanceId);
		this.nodeElementDataSet = containerProvider.Resolve<INodeElementDataSetProvider>().Resolve(instanceId);
		this.elementDataSetService = containerProvider.Resolve<IElementDataSetService>();
		this.reconstructionSections = containerProvider.Resolve<IReconstructionSectionsProvider>().Resolve(instanceId);
		this.experimentInfoResolver = containerProvider.Resolve<IExperimentInfoProvider>().Resolve(instanceId);
	}

	public PyObject GetPyObject(PyModule scope)
	{
		var os = scope.Import("os");
		os.GetAttr("environ")["PYTHON_NET_MODE"] = new PyString("CSharp");
		//Environment.SetEnvironmentVariable("PYTHON_NET_MODE", "CSharp");
		var pyapsuite = scope.Import("pyapsuite");
		//var pyapsuite = scope.Import("adapters.apsuite_context");

		// Pass in some functions that requre C# work
		// TODO: Potentially extract to a C# class library and call directly from the script
		var functions = new PyDict();
		functions["ToIntPtr"] = new Func<MemoryHandle, IntPtr>(CSharpFunctions.ToIntPtr).ToPython();

		// Pass in node scope resolved services
		var services = new PyDict();
		services["IIonDisplayInfo"] = ionDisplayInfo.ToPython();
		services["INodeInfo"] = nodeInfo.ToPython();
		services["IMassSpectrumRangeManager"] = rangeManager.ToPython();
		services["INodeProperties"] = properties.ToPython();
		services["INodeElementDataSet"] = nodeElementDataSet.ToPython();
		services["IElementDataSetService"] = elementDataSetService.ToPython();
		services["IIonDisplayInfo"] = ionDisplayInfo.ToPython();
		services["IReconstructionSections"] = reconstructionSections.ToPython();
		services["IExperimentInfoResolver"] = experimentInfoResolver.ToPython();

		var context = pyapsuite.InvokeMethod(
			"APSuiteContext",
			ionData.ToPython(),
			services,
			functions);
		return context;
	}

	private const string ResolutionExceptionMessage = "Could not resolve node information for NodeID = {0:B}";
	private static Guid GetRootNodeId(INodeInfoProvider nodeInfoProvider, Guid nodeId)
	{
		Guid ptrNodeId = nodeId;
		INodeInfo ptrNodeInfo = nodeInfoProvider.Resolve(ptrNodeId) ?? throw new CustomAnalysisException(string.Format(ResolutionExceptionMessage, nodeId));
		while (ptrNodeInfo.Parent.HasValue && ptrNodeInfo.Parent.Value != Guid.Empty)
		{
			ptrNodeId = ptrNodeInfo.Parent.Value;
			ptrNodeInfo = nodeInfoProvider.Resolve(ptrNodeId) ?? throw new CustomAnalysisException(string.Format(ResolutionExceptionMessage, nodeId));
		}
		return ptrNodeId;
	}
}

internal static class CSharpFunctions
{
	public unsafe static IntPtr ToIntPtr(MemoryHandle handle) => new IntPtr(handle.Pointer);
}
