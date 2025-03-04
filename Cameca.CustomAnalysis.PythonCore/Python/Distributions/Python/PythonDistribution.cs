using Cameca.CustomAnalysis.Interface;
using Microsoft.Extensions.Logging;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Cameca.CustomAnalysis.PythonCore;

internal class PythonDistribution : IPyDistribution
{
	private readonly ILogger<PythonDistribution> logger;
	private readonly IOptionsAccessor optionsAccessor;

	public PythonDistribution(ILogger<PythonDistribution> logger, IOptionsAccessor optionsAccessor)
	{
		this.logger = logger;
		this.optionsAccessor = optionsAccessor;
	}

	public bool Initialize()
	{

		try
		{
			return InitializeImpl();
		}
		catch (Exception ex)
		{
			logger.LogWarning(ex, "Exception initializing Python distribution");
			return false;
		}
	}

	private bool InitializeImpl()
	{
		var options = optionsAccessor.GetOptions<PythonOptions>();
		string? pathToVirtualEnv = options.PythonVenvDir;

		Runtime.PythonDLL = options.PythonDll;


		if (!string.IsNullOrEmpty(pathToVirtualEnv))
		{
			var prependPath = options.PrependPathEnvVar?.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
			string updatedPath = PrependToPathSeparatedValue(Environment.GetEnvironmentVariable("PATH"), prependPath);
			Environment.SetEnvironmentVariable("PATH", updatedPath, EnvironmentVariableTarget.Process);

			if (options.SetNoSiteFlag)
			{
				PythonEngine.SetNoSiteFlag();
			}

			var pythonHome = options.PythonHome;
			if (!string.IsNullOrWhiteSpace(pythonHome))
			{
				Environment.SetEnvironmentVariable("PYTHONHOME", pythonHome, EnvironmentVariableTarget.Process);
				PythonEngine.PythonHome = pythonHome;
			}
			PythonEngine.Initialize();

			// https://github.com/pythonnet/pythonnet/issues/1478#issuecomment-897933730
			using (Py.GIL())
			{
				// fix the prefixes to point to our venv
				// (This is for Windows, there may be some difference with sys.exec_prefix on other platforms)
				dynamic sys = Py.Import("sys");
				sys.prefix = pathToVirtualEnv;
				sys.exec_prefix = pathToVirtualEnv;

				dynamic site = Py.Import("site");
				// This has to be overwritten because site module may already have 
				// been loaded by the interpreter (but not run yet)
				site.PREFIXES = new List<PyObject> { sys.prefix, sys.exec_prefix };
				// Run site path modification with tweaked prefixes
				site.main();
			}
		}
		else
		{
			PythonEngine.Initialize();
		}

		if (options.ApplyCondaIntelMklFix)
		{
			using (Py.GIL())
			{
				var os = Py.Import("os");
				os.GetAttr("environ")["KMP_DUPLICATE_LIB_OK"] = new PyString("TRUE");
			}
		}
		return true;
	}

	private static string PrependToPathSeparatedValue(string? source, params string[] paths)
	{
		// Parse all existing value into individual path components
		var parsedCurPaths = (source?.Split(Path.PathSeparator) ?? Enumerable.Empty<string>())
			.Where(x => !string.IsNullOrEmpty(x))
			.ToList();

		// Parse all new value into individual path components (each entry could have multiple paths itself)
		var parsedNewPaths = paths.SelectMany(pth => pth.Split(Path.PathSeparator))
			.Where(x => !string.IsNullOrEmpty(x))
			.ToList();

		var missingPaths = parsedNewPaths.Except(parsedCurPaths).ToList();

		// Prepend all missing paths to the existing list and rejoin
		return string.Join(Path.PathSeparator, missingPaths.Concat(parsedCurPaths));
	}
}
