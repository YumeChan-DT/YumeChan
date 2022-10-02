using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using YumeChan.Core.Infrastructure.Json.Converters;
using YumeChan.PluginBase;
using YumeChan.PluginBase.Tools;

namespace YumeChan.Core.Services.Config;

public sealed class JsonConfigProvider<TPlugin> : IJsonConfigProvider<TPlugin> where TPlugin : IPlugin
{
	private readonly ILoggerFactory _loggerFactory;
	private static readonly PhysicalFileProvider _configFileProvider = new(Path.Combine(Directory.GetCurrentDirectory(), "config"));
	private static readonly ConcurrentDictionary<string, IChangeToken> _configReloadTokens = new();
	private readonly ILogger<JsonConfigProvider<TPlugin>> _logger;

	private const string FileExtension = ".json";

	private static JsonSerializerOptions JsonSerializerOptions { get; } = new JsonSerializerOptions()
	{
		AllowTrailingCommas = true,
		WriteIndented = true,
		NumberHandling = JsonNumberHandling.AllowReadingFromString,
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
		ReadCommentHandling = JsonCommentHandling.Skip,
		Converters = {  new JsonStringEnumConverter() }		
	};

	public JsonConfigProvider(ILoggerFactory loggerFactory)
	{
		_loggerFactory = loggerFactory;
		_logger = loggerFactory.CreateLogger<JsonConfigProvider<TPlugin>>();
	}

	public IWritableConfiguration GetConfiguration(string filename, bool autosave, bool autoreload) => GetConfiguration(filename, false, autosave, autoreload);

	internal JsonWritableConfig GetConfiguration(string filename, bool placeFileAtConfigRoot, bool autosave, bool autoreload)
	{
		filename += filename.EndsWith(FileExtension) ? string.Empty : FileExtension;
		string configFileSubpath = placeFileAtConfigRoot ? filename : Path.Combine(typeof(TPlugin).Assembly.GetName().Name ?? throw new InvalidOperationException(), filename);
		IFileInfo file = _configFileProvider.GetFileInfo(configFileSubpath);
		
		return GetConfiguration(file, autosave, autoreload, _loggerFactory);
	}
	
	internal static JsonWritableConfig GetConfiguration(IFileInfo file, bool autosave, bool autoreload, ILoggerFactory loggerFactory)
	{
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
			reloadToken = _configReloadTokens.GetOrAdd(file.PhysicalPath, _configFileProvider.Watch);
		}

		return new(new(file.PhysicalPath), JsonSerializerOptions, loggerFactory.CreateLogger<JsonWritableConfig>(), reloadToken, null, firstLoad, autosave, autoreload);
	}
}
