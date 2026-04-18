using Cameca.CustomAnalysis.PythonCore.Python.Rpc.Models;
using Microsoft.Extensions.Logging;
using StreamJsonRpc;

namespace Cameca.CustomAnalysis.PythonCore.Python.Rpc;

public class HostCallbacks
{
    private readonly ILogger logger;

	public HostCallbacks(ILogger logger)
    {
        this.logger = logger;
    }

    [JsonRpcMethod("log")]
    public void Log(HostLogRecord logRecord)
	{
		using (logger.BeginScope(logRecord.CreateLogContext()))
		{
			logger.Log(logRecord.MsLogLevel, logRecord.Message);
		}
	}
}
