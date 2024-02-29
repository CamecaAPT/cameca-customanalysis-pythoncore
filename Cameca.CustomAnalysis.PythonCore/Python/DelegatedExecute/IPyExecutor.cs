using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Cameca.CustomAnalysis.PythonCore;

public interface IPyExecutor
{	Task Execute(IPyExecutable executable, IEnumerable<IPyExecutorMiddleware> middleware, CancellationToken token);
}
