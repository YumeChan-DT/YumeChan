using System.Diagnostics.CodeAnalysis;
using System.IO;
using JetBrains.Annotations;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using YumeChan.Core.Modules;
using YumeChan.PluginBase.Tools;

namespace YumeChan.Core.Services.Config;
#nullable enable

public abstract class InterfaceConfigProviderBase
{
	protected static string ConfigPath { get; } = Path.Combine(Directory.GetCurrentDirectory(), "config");
	protected static PhysicalFileProvider? ConfigFileProvider { get; set; }
}

public sealed class InterfaceConfigProvider<TConfig> : InterfaceConfigProviderBase, IInterfaceConfigProvider<TConfig> where TConfig : class
{
	private const string FileExtension = ".json";
	
	private readonly ILoggerFactory _loggerFactory;
	private InterfaceWritableConfigWrapper<TConfig>? _configWrapper;


	public InterfaceConfigProvider(ILoggerFactory loggerFactory)
	{
		_loggerFactory = loggerFactory;
		
		// Create the config directory if it doesn't exist
		if (!Directory.Exists(ConfigPath))
		{
			Directory.CreateDirectory(ConfigPath);
		}

		ConfigFileProvider ??= new(ConfigPath);
	}
	
	public TConfig? Configuration { get; set; }

	public FileInfo? ConfigFile { get; private set; }

	public TConfig InitConfig(string filename) => InitConfig(filename, false);
	internal TConfig InitConfig(string filename, bool placeFileAtConfigRoot)
	{
		filename += filename.EndsWith(FileExtension) ? string.Empty : FileExtension;

		string pluginDirectory = placeFileAtConfigRoot ? string.Empty : typeof(TConfig).Assembly.GetName().Name!;
		IFileInfo file = ConfigFileProvider!.GetFileInfo(Path.Join(pluginDirectory, filename));
		ConfigFile = file.PhysicalPath is { } path ? new(path) : null;
		
		// Instantiate the configuration 
		_configWrapper ??= new(JsonConfigProvider<InternalPlugin>.GetConfiguration(file, true, true, _loggerFactory));
		
		return Configuration = _configWrapper.CreateInstance();
	}
}