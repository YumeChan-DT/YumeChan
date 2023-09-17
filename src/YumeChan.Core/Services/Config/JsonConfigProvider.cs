using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System.Collections.Concurrent;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using YumeChan.Core.Config;
using YumeChan.PluginBase;
using YumeChan.PluginBase.Tools;

namespace YumeChan.Core.Services.Config;

/// <summary>
/// Common base class for all typed <see cref="JsonConfigProvider{TPlugin}"/>.
/// </summary>
public abstract class JsonConfigProvider
{
	protected const string FileExtension = ".json";

	protected static readonly ConcurrentDictionary<string, IChangeToken> ConfigReloadTokens = new();
	protected static readonly JsonSerializerOptions JsonSerializerOptions = new()
	{
		AllowTrailingCommas = true,
		WriteIndented = true,
		NumberHandling = JsonNumberHandling.AllowReadingFromString,
		Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
		ReadCommentHandling = JsonCommentHandling.Skip,
		Converters = {  new JsonStringEnumConverter() }
	};

	protected static PhysicalFileProvider ConfigFileProvider => _configFileProvider ??= _defaultConfigFileProvider;
	
	private static PhysicalFileProvider? _configFileProvider;
	private static readonly PhysicalFileProvider _defaultConfigFileProvider = new(Path.Combine(Directory.GetCurrentDirectory(), "config"));


	protected JsonConfigProvider(ICoreProperties coreProperties)
	{
		_configFileProvider ??= new(coreProperties.Path_Config);
	}
}

/// <summary>
/// Provides a <see cref="IWritableConfiguration"/> for a plugin.
/// </summary>
/// <typeparam name="TPlugin">The plugin type.</typeparam>
public sealed class JsonConfigProvider<TPlugin> : JsonConfigProvider, IJsonConfigProvider<TPlugin> where TPlugin : IPlugin
{
	private readonly ILoggerFactory _loggerFactory;
	private readonly ILogger<JsonConfigProvider<TPlugin>> _logger;

	public JsonConfigProvider(ICoreProperties coreProperties, ILoggerFactory loggerFactory) : base(coreProperties)
	{
		_loggerFactory = loggerFactory;
		_logger = loggerFactory.CreateLogger<JsonConfigProvider<TPlugin>>();
	}
	
	/// <inheritdoc/>
	public IWritableConfiguration GetConfiguration(string filename, bool autosave = true, bool autoreload = true) 
		=> GetConfiguration(filename, false, autosave, autoreload);

	
	internal JsonWritableConfig GetConfiguration(string filename, bool placeFileAtConfigRoot, bool autosave, bool autoreload)
	{
		filename += filename.EndsWith(FileExtension) ? string.Empty : FileExtension;
		string configFileSubpath = placeFileAtConfigRoot ? filename : Path.Combine(typeof(TPlugin).Assembly.GetName().Name ?? throw new InvalidOperationException(), filename);
		IFileInfo file = ConfigFileProvider.GetFileInfo(configFileSubpath);
		
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
			reloadToken = ConfigReloadTokens.GetOrAdd(file.PhysicalPath, ConfigFileProvider.Watch);
		}

		return new(new(file.PhysicalPath), JsonSerializerOptions, loggerFactory.CreateLogger<JsonWritableConfig>(), reloadToken, null, firstLoad, autosave, autoreload);
	}
}
