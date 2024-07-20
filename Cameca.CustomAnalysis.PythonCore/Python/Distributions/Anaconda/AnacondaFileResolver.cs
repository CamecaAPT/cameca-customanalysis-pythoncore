using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace Cameca.CustomAnalysis.PythonCore;

/// <summary>
/// Identity Anaconda installation path from an assembly adjacent text file
/// </summary>
internal class AnacondaFileResolver
{
	private string ConfigFileNameSuffix => ".settings.json";
	private const string AnacondaLocationKey = "AnacondaDirectory";
	private readonly ILogger<AnacondaFileResolver> logger;

	public AnacondaFileResolver(ILogger<AnacondaFileResolver> logger)
	{
		this.logger = logger;
	}

	public string? CheckAnacondaLocationFile()
	{
		var configFullPath = GetConfigFullPath();
		if (File.Exists(configFullPath))
		{
			try
			{
				string jsonString = File.ReadAllText(configFullPath);
				using var doc = JsonDocument.Parse(jsonString);
				if (doc.RootElement.TryGetProperty(AnacondaLocationKey, out var locationElement)) {
					return locationElement.GetString();
				}
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Unable to resolve Anaconda location from settings file");
			}
		}
		return null;
	}

	private string? GetConfigFullPath()
	{
		var executingAssembly = Assembly.GetExecutingAssembly();
		if (executingAssembly.GetName().Name is not { } assemblyName)
		{
			return null;
		}
		var fullConfigFileName = $"{assemblyName}{ConfigFileNameSuffix}";
		return Path.Join(Path.GetDirectoryName(executingAssembly.Location), fullConfigFileName);
	}
}
