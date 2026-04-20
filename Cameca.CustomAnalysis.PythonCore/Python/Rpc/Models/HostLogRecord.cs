using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Cameca.CustomAnalysis.PythonCore.Python.Rpc.Models;

public sealed record HostLogRecord(
	int Level,
	string LoggerName,
	string Message,
	// Timestamp is ms since Unix epoch
	long Timestamp,
	string? Exception,
	string? StackInfo)
{
	[JsonIgnore]
	public LogLevel MsLogLevel => Level switch {
		1 => LogLevel.Debug,
		2 => LogLevel.Information,
		3 => LogLevel.Warning,
		4 => LogLevel.Error,
		5 => LogLevel.Critical,
		_ => LogLevel.None
	};

	public Dictionary<string, string> CreateLogContext()
	{
		return new Dictionary<string, string>
		{
			["LoggerName"] = LoggerName ?? "",
			["Timestamp"] = DateTimeOffset.FromUnixTimeMilliseconds(Timestamp).ToString("o"),
			["Exception"] = Exception ?? "",
			["StackInfo"] = StackInfo ?? ""
		};
	}
}
