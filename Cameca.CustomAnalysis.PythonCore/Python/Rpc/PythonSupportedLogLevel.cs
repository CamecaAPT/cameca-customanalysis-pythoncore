using Microsoft.Extensions.Logging;

namespace Cameca.CustomAnalysis.PythonCore.Python.Rpc;

public enum PythonSupportedLogLevel
{
	Debug = LogLevel.Debug,
	Information = LogLevel.Information,
	Warning = LogLevel.Warning,
	Error = LogLevel.Error,
	Critical = LogLevel.Critical,
}
