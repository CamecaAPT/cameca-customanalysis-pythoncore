namespace Cameca.CustomAnalysis.PythonCore.Python.Rpc.Models;

public sealed record SectionInfo(string Unit, bool Protected, bool Virtual, long RecordCount, int ValuesPerRecord, string Type);
