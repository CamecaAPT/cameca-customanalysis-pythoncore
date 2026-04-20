using System.Collections.Generic;

namespace Cameca.CustomAnalysis.PythonCore.Python.Rpc.Models;

public sealed record class AnalysisTreeNode(
	string Id,
	string Name,
	string Title,
	string DataSectionName,
	string TypeId,
	string IonDataOwnerId,
	List<AnalysisTreeNode> Children);
