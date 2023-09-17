using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using YumeChan.Core.Modules;
using YumeChan.PluginBase.Tools;

namespace YumeChan.Core.Services.Config;

/// <summary>
/// Represents a configuration provider for a plugin.
/// </summary>
/// <typeparam name="TConfig">The type of the configuration interface.</typeparam>
public sealed class InterfaceConfigProvider<TConfig> : IInterfaceConfigProvider<TConfig> where TConfig : class
{
	private readonly ILoggerFactory _loggerFactory;
	private const string FileExtension = ".json";
	
	private static readonly PhysicalFileProvider? _configFileProvider = new(Path.Combine(Directory.GetCurrentDirectory(), "config"));

	public InterfaceConfigProvider(ILoggerFactory loggerFactory)
	{
		_loggerFactory = loggerFactory;
	}
	
	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
	public TConfig? Configuration { get; set; }

	public FileInfo? ConfigFile { get; private set; }
	
	public TConfig InitConfig(string filename) => InitConfig(filename, false);
	internal TConfig InitConfig(string filename, bool placeFileAtConfigRoot)
	{
		filename += filename.EndsWith(FileExtension) ? string.Empty : FileExtension;
		string pluginDirectory = placeFileAtConfigRoot ? string.Empty : typeof(TConfig).Assembly.GetName().Name!;
		IFileInfo file = _configFileProvider!.GetFileInfo(Path.Join(pluginDirectory, filename));
		
		// Instantiate the configuration 
		InterfaceWritableConfigWrapper<TConfig> configWrapper = new(JsonConfigProvider<InternalPlugin>.GetConfiguration(file, true, true, _loggerFactory));
		
		return Configuration = configWrapper.CreateInstance();
	}
}