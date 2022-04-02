using System.IO;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using YumeChan.Core.Modules;
using YumeChan.PluginBase.Tools;

namespace YumeChan.Core.Services.Config;

public class InterfaceConfigProvider<TConfig> : IInterfaceConfigProvider<TConfig> where TConfig : class
{
	private readonly ILoggerFactory _loggerFactory;
	private const string FileExtension = ".json";
	private InterfaceWritableConfigWrapper<TConfig> _configWrapper;
	private static readonly PhysicalFileProvider _configFileProvider = new(Path.Combine(Directory.GetCurrentDirectory(), "config"));

	public InterfaceConfigProvider(ILoggerFactory loggerFactory)
	{
		_loggerFactory = loggerFactory;
	}
	
	public TConfig Configuration { get; set; }

	public FileInfo ConfigFile { get; private set; }

	public TConfig InitConfig(string filename) => InitConfig(filename, false);
	internal TConfig InitConfig(string filename, bool placeFileAtConfigRoot)
	{
		filename += filename.EndsWith(FileExtension) ? string.Empty : FileExtension;

		string pluginDirectory = placeFileAtConfigRoot ? string.Empty : typeof(TConfig).Assembly.GetName().Name;
		IFileInfo file = _configFileProvider.GetFileInfo(Path.Join(pluginDirectory, filename));
		
		// Instantiate the configuration 
		_configWrapper = new(JsonConfigProvider<InternalPlugin>.GetConfiguration(file, true, true, _loggerFactory));
		
		return Configuration = _configWrapper.CreateInstance();
	}
}