using PolyType;
using StreamJsonRpc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cameca.CustomAnalysis.PythonCore.Python.Rpc;

public interface IPythonApiBase
{
    //[JsonRpcMethod("demo.hello")]
    //Task<string> HelloAsync(string name);
    ////Task<string> HelloAsync(string name, ReadOnlyMemory<float> massHistData);

    [JsonRpcMethod("sys.shutdown")]
    Task ShutdownAsync();

}
