using Cameca.CustomAnalysis.Interface;
using Prism.Ioc;
using Python.Runtime;
using System;

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
	private readonly IChart3D? chart3d;
	private readonly IRenderDataFactory? renderDataFactory;
	private readonly IContainerProvider containerProvider;
	private readonly Guid instanceId;

	public APSuiteContextProvider(
		IIonData ionData,
		IContainerProvider containerProvider,
		Guid instanceId)
	{
		this.ionData = ionData;
		this.containerProvider = containerProvider;
		this.instanceId = instanceId;
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
		services["INodeInfo"] = nodeInfo.ToPython();
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

		var context = pyapsuite.InvokeMethod(
			"APSuiteContext",
			ionData.ToPython(),
			services,
			instanceId.ToPython());
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
