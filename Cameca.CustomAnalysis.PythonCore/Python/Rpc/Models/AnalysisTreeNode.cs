using System.Collections.Generic;

namespace Cameca.CustomAnalysis.PythonCore.Python.Rpc.Models;

public sealed record class AnalysisTreeNode(string Id, List<AnalysisTreeNode> Children);
