using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;

namespace Cameca.CustomAnalysis.PythonCore;

internal class AnacondaAutoResolver
{
	// https://docs.anaconda.com/anaconda/user-guide/faq/
	private static readonly string DefaultPerUserInstallationPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Anaconda3");
	// https://docs.anaconda.com/anaconda/install/multi-user/
	private static readonly string DefaultPerMachineInstallationPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Anaconda");

	private readonly ILogger<AnacondaAutoResolver> logger;
	private readonly AnacondaRegistryResolver anacondaRegistryResolver;

	public AnacondaAutoResolver(ILogger<AnacondaAutoResolver> logger, AnacondaRegistryResolver anacondaRegistryResolver)
	{
		this.logger = logger;
		this.anacondaRegistryResolver = anacondaRegistryResolver;
	}

	/// <summary>
	/// Attempts to locate an Anaconda installation. If no installation is found, return null.
	/// </summary>
	/// <returns></returns>
	public string? AutoLocateAnacondaPath()
	{
		foreach (var candidateLocation in IterateAnacondaSearchPaths())
		{
			if (candidateLocation is not null && QuickValidateAnacondaPath(candidateLocation))
			{
				return candidateLocation;
			}
		}
		return null;
	}

	private IEnumerable<string?> IterateAnacondaSearchPaths()
	{
		// Check for latest version from registry
		yield return CheckAnacondaLocationRegistrySafe();
		// Check default per-user installation path
		yield return DefaultPerUserInstallationPath;
		// Check default all-users installation path
		yield return DefaultPerMachineInstallationPath;
	}

	/// <summary>
	/// Sanity check that the given path exists, is a directory, and has python.exe at the root.
	/// This is not proof positive that we have the correct location, but failing these checks
	/// would indicate that we have the wrong path.
	/// </summary>
	/// <param name="path"></param>
	/// <returns></returns>
	private bool QuickValidateAnacondaPath(string path) => File.Exists(Path.Join(path, "python.exe"));

	private string? CheckAnacondaLocationRegistrySafe()
	{
		try
		{
			return anacondaRegistryResolver.CheckAnacondaLocationRegistry();
		}
		catch (Exception ex)
		{
			// This is only a convenience. If for any reason this fails, ignore and continue
			logger.LogWarning(ex, AnacondaResources.LogWarningGeneralCheckRegistryException);
			return null;
		}
	}
}
