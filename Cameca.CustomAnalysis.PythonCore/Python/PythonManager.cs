using Python.Runtime;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Cameca.CustomAnalysis.PythonCore;


/// <summary>
/// Abstracts high level setup and control of Python.NET.
/// Delegates responsibilities to a Python distribution wrapper instance,
/// allowing for extensibility of different Python distributions or configurations.
/// </summary>
internal sealed class PythonManager : IPythonManager
{
	private const string BundledPythonPackagesDirectoryRelativePath = "PythonModules";

	private readonly ICollection<IPyDistribution> registeredDistributions;

	public PythonManager(IEnumerable<IPyDistribution> registeredDistributions)
	{
		this.registeredDistributions = registeredDistributions.ToList();
	}

	/// <summary>
	/// Select and call <see cref="IPyDistribution.Initialize" /> to delegate initialization
	/// to a registered <see cref="IPyDistribution" /> instance.
	/// </summary>
	/// <returns></returns>
	public bool Initialize()
	{
		if (PythonEngine.IsInitialized)
		{
			return true;
		}

		// TODO: Support user selection from multiple registered distributions
		if (registeredDistributions.FirstOrDefault() is not { } distribution)
		{
			return false;
		}

		if (distribution.Initialize())
		{
			// If successfully initialized, paths must be added to sys.path so that included extension scripts can be located and imported
			var addPythonPaths = new string[]
			{
				Path.Join(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), BundledPythonPackagesDirectoryRelativePath),
			};
			using (Py.GIL())
			{
				var sys = Py.Import("sys");
				foreach (var searchPath in addPythonPaths)
				{
					sys.GetAttr("path").InvokeMethod("append", searchPath.ToPython());
				}
			}
			return true;
		}
		return false;
	}

	/// <summary>
	/// Shutdown the PythonEngine if it is initialized
	/// </summary>
	public void Shutdown()
	{
		if (PythonEngine.IsInitialized)
		{
			PythonEngine.Shutdown();
		}
	}

	/// <summary>
	/// Call <see cref="Shutdown" /> to cleanup PythonEngine
	/// </summary>
	public void Dispose() => Shutdown();
}
