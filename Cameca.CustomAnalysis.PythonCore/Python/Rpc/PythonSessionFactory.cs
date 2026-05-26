using Cameca.CustomAnalysis.Interface;
using System.IO;
using System.Reflection;

namespace Cameca.CustomAnalysis.PythonCore;

public class PythonSessionFactory
{
	private readonly IOptionsAccessor optionsAccessor;

	public PythonSessionFactory(IOptionsAccessor optionsAccessor)
	{
		this.optionsAccessor = optionsAccessor;
	}

	public PythonRpcSession<THostCallbacks, TPythonApi> Start<THostCallbacks, TPythonApi>(
			string pythonModulePath,
			THostCallbacks hostCallbacks,
			string? workingDir = null)
		where THostCallbacks : class
		where TPythonApi : class, IPythonApiBase
	{
		workingDir ??= Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		var absPath = Path.IsPathFullyQualified(pythonModulePath)
			? pythonModulePath
			: Path.Join(workingDir, pythonModulePath);

		var options = optionsAccessor.GetOptions<PythonRpcOptions>();
		var consoleOptions = new ConsoleOptions(
			options.ShowPythonTerminal,
			!options.KeepTerminalOpen);
		var logOptions = new LogOptions(
			options.PythonLogLevel,
			options.PythonRpcLogLevel);
		
		var session = PythonRpcSession<THostCallbacks, TPythonApi>.Start(
			absPath,
			hostCallbacks,
			workingDirectory: workingDir,
			pythonExePath: Path.Join(options.PythonVenvDir, "Scripts", "launch-extension.exe"),
			consoleOptions: consoleOptions,
			logOptions: logOptions);
		return session;
	}
}
