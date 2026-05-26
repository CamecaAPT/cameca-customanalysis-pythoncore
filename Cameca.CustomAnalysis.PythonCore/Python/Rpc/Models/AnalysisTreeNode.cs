using System.Collections.Generic;

namespace Cameca.CustomAnalysis.PythonCore;

public sealed record class AnalysisTreeNode(
	string Id,
	string Name,
	string Title,
	string DataSectionName,
	string TypeId,
	string IonDataOwnerId,
	List<AnalysisTreeNode> Children);
