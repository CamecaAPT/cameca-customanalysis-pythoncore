using System.Collections.Generic;

namespace Cameca.CustomAnalysis.PythonCore.Python.Rpc.Models;

public sealed record IonRange(
	string Name,
	Dictionary<string, int> Formula,
	double Volume,
	double Min,
	double Max,
	Color Color);
