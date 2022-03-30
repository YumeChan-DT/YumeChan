using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using YumeChan.PluginBase;
using YumeChan.PluginBase.Tools;

namespace YumeChan.Core.Services.Config;

public class JsonConfigProvider<TPlugin> : IJsonConfigProvider<TPlugin> where TPlugin : IPlugin
{
	private readonly ILoggerFactory _loggerFactory;
	private static readonly PhysicalFileProvider _configFileProvider = new(Path.Combine(Directory.GetCurrentDirectory(), "config"));
	private static readonly ConcurrentDictionary<string, IChangeToken> _configReloadTokens = new();
	private readonly ILogger<JsonConfigProvider<TPlugin>> _logger;

	private const string FileExtension = ".json";
	
	private static JsonSerializerOptions JsonSerializerOptions { get; } = new()
	{
		AllowTrailingCommas = true,
		WriteIndented = true 
	};

	public JsonConfigProvider(ILoggerFactory loggerFactory)
	{
		_loggerFactory = loggerFactory;
		_logger = loggerFactory.CreateLogger<JsonConfigProvider<TPlugin>>();
	}

	public IWritableConfiguration GetConfiguration(string filename, bool autosave, bool autoreload)
	{
		filename += filename.EndsWith(FileExtension) ? string.Empty : FileExtension;
		string configFileSubpath = Path.Combine(typeof(TPlugin).Assembly.GetName().Name ?? throw new InvalidOperationException(), filename);
		IFileInfo file = _configFileProvider.GetFileInfo(configFileSubpath);

		bool firstLoad = !file.Exists;

		if (firstLoad)
		{
			string dirPath = Path.GetDirectoryName(file.PhysicalPath);

			if (!Directory.Exists(dirPath))
			{
				Directory.CreateDirectory(dirPath);
			}

			File.CreateText(file.PhysicalPath).Close();
		}

		IChangeToken reloadToken = null;

		if (autoreload)
		{
			reloadToken = _configReloadTokens.GetOrAdd(configFileSubpath, _configFileProvider.Watch);
		}

		return new JsonWritableConfig(new(file.PhysicalPath), JsonSerializerOptions, _loggerFactory.CreateLogger<JsonWritableConfig>(), 
			reloadToken, null, firstLoad, autosave, autoreload);
	}
}
