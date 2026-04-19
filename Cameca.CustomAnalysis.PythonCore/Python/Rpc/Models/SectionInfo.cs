using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cameca.CustomAnalysis.PythonCore.Python.Rpc.Models;

public record SectionInfo(string Unit, bool Protected, bool Virtual, long RecordCount, int ValuesPerRecord, string Type);
