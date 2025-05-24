using Cameca.CustomAnalysis.Interface;
using Cameca.CustomAnalysis.Utilities;
using Prism.Ioc;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cameca.CustomAnalysis.PythonCore;

public class APSuiteContextProvider : IPyObjectProvider
{
	private readonly IIonData ionData;
	private readonly IIonDisplayInfo? ionDisplayInfo;
	private readonly IMassSpectrumRangeManager? rangeManager;
	private readonly INodeProperties? properties;
	private readonly INodeElementDataSet? nodeElementDataSet;
	private readonly IElementDataSetService elementDataSetService;
	private readonly IReconstructionSections? reconstructionSections;
	private readonly IExperimentInfoResolver? experimentInfoResolver;
	private readonly IChart3D? chart3d;
	private readonly IRenderDataFactory? renderDataFactory;
	private readonly IContainerProvider containerProvider;
	private readonly Guid instanceId;
	private readonly IGrid3DData? gridData;
	private readonly IGrid3DParameters? gridParams;
	private readonly IResources resources;

	public APSuiteContextProvider(
		IIonData ionData,
		IContainerProvider containerProvider,
		Guid instanceId,
		IResources resources)
	{
		this.ionData = ionData;
		this.containerProvider = containerProvider;
		this.instanceId = instanceId;
		this.resources = resources;
		this.ionDisplayInfo = containerProvider.Resolve<IIonDisplayInfoProvider>().Resolve(instanceId);
		// Only allow fetching and setting ranges from root level - no spatial ranging support for extensions
		var nodeInfoProvider = containerProvider.Resolve<INodeInfoProvider>();
		var rootNodeId = GetRootNodeId(nodeInfoProvider, instanceId);
		this.rangeManager = containerProvider.Resolve<IMassSpectrumRangeManagerProvider>().Resolve(rootNodeId);
		var gridNodeId = IterateNodeContainers(nodeInfoProvider, rootNodeId)
			.FirstOrDefault(x => x.NodeInfo.TypeId == "GridNode")?.NodeId;
		var gridNodeDataProvider = gridNodeId.HasValue
			? containerProvider.Resolve<INodeDataProvider>().Resolve(gridNodeId.Value)
			: null;
		this.gridData = gridNodeDataProvider?.GetDataSync(typeof(IGrid3DData)) as IGrid3DData;
		this.gridParams = gridNodeDataProvider?.GetDataSync(typeof(IGrid3DParameters)) as IGrid3DParameters;
		this.properties = containerProvider.Resolve<INodePropertiesProvider>().Resolve(instanceId);
		this.nodeElementDataSet = containerProvider.Resolve<INodeElementDataSetProvider>().Resolve(instanceId);
		this.elementDataSetService = containerProvider.Resolve<IElementDataSetService>();
		this.reconstructionSections = containerProvider.Resolve<IReconstructionSectionsProvider>().Resolve(instanceId);
		this.experimentInfoResolver = containerProvider.Resolve<IExperimentInfoProvider>().Resolve(instanceId);
		this.chart3d = containerProvider.Resolve<IMainChartProvider>().Resolve(instanceId);
		this.renderDataFactory = containerProvider.Resolve<IRenderDataFactory>();
	}	

	public PyObject GetPyObject(PyModule scope)
	{
		var os = scope.Import("os");
		os.GetAttr("environ")["PYTHON_NET_MODE"] = new PyString("CSharp");
		//Environment.SetEnvironmentVariable("PYTHON_NET_MODE", "CSharp");
		var pyapsuite = scope.Import("pyapsuite");
		//var pyapsuite = scope.Import("adapters.apsuite_context");
		// Pass in node scope resolved services

		var services = new PyDict();
		services["IIonDisplayInfo"] = ionDisplayInfo.ToPython();
		services["IMassSpectrumRangeManager"] = rangeManager.ToPython();
		services["INodeProperties"] = properties.ToPython();
		services["INodeElementDataSet"] = nodeElementDataSet.ToPython();
		services["IElementDataSetService"] = elementDataSetService.ToPython();
		services["IIonDisplayInfo"] = ionDisplayInfo.ToPython();
		services["IReconstructionSections"] = reconstructionSections.ToPython();
		services["IExperimentInfoResolver"] = experimentInfoResolver.ToPython();
		services["IChart3D"] = chart3d.ToPython();
		services["IRenderDataFactory"] = renderDataFactory.ToPython();
		services["IContainerProvider"] = containerProvider.ToPython();
		services["IGrid3DData"] = gridData?.ToPython() ?? PyObject.None;
		services["IGrid3DParameters"] = gridParams?.ToPython() ?? PyObject.None;

		var context = pyapsuite.InvokeMethod(
			"APSuiteContext",
			ionData.ToPython(),
			services,
			instanceId.ToPython(),
			resources.ToPython());
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

	private static IEnumerable<NodeInfoContainer> IterateNodeContainers(INodeInfoProvider nodeInfoProvider, Guid nodeId)
	{
		var rootNodeId = GetRootNodeId(nodeInfoProvider, nodeId);
		Queue<Guid> queue = new Queue<Guid>();
		queue.Enqueue(rootNodeId);
		Guid result;
		while (queue.TryDequeue(out result))
		{
			INodeInfo? nodeInfo = nodeInfoProvider.Resolve(result);
			if (nodeInfo == null)
			{
				continue;
			}

			foreach (Guid child in nodeInfo.Children)
			{
				queue.Enqueue(child);
			}

			yield return new NodeInfoContainer(result, nodeInfo);
		}
	}

	private record NodeInfoContainer(Guid NodeId, INodeInfo NodeInfo);
}
