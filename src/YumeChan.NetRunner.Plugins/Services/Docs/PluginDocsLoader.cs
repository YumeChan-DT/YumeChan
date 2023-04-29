using System.Reflection;
using Nodsoft.MoltenObsidian.Vault;
using Nodsoft.MoltenObsidian.Vaults.FileSystem;
using YumeChan.Core.Config;
using YumeChan.Core.Services.Plugins;
using YumeChan.PluginBase;
using YumeChan.PluginBase.Infrastructure;

namespace YumeChan.NetRunner.Plugins.Services.Docs;

/// <summary>
/// <p>Provides a service to load a plugin's documentation.</p>
///
/// <p>
/// Documentation is loaded from the plugin's directory,
/// and is expected to be in the form of a single or multiple Markdown files.
///
/// A Tree structure is created from the Markdown files found recursively in the plugin's <c>/docs</c> directory.
/// </p>
///
/// </summary>
public sealed class PluginDocsLoader
{
	private readonly ICoreProperties _coreProperties;
	private readonly PluginsLoader _pluginsLoader;

	public PluginDocsLoader(ICoreProperties coreProperties, PluginsLoader pluginsLoader)
	{
		_coreProperties = coreProperties;
		_pluginsLoader = pluginsLoader;
	}
	
	private readonly Dictionary<string, IVault?> _vaults = new();

	/// <summary>
	/// Gets a <see cref="IVault"/> for the specified plugin.
	/// </summary>
	/// <param name="pluginName">The internal name of the plugin to get the vault for.</param>
	/// <returns>The vault for the specified plugin.</returns>
	/// <exception cref="ArgumentException">Thrown if the specified plugin does not exist.</exception>
	public IVault? GetVault(string pluginName)
	{
		// First get from cache.
		if (_vaults.TryGetValue(pluginName, out IVault? vault))
		{
			return vault;
		}

		// Then try to instantiate from plugin's assets.
		
		// First. Does this plugin even exist?
		if (!_pluginsLoader.PluginManifests.TryGetValue(pluginName, out _))
		{
			throw new ArgumentException($"Plugin '{pluginName}' does not exist.", nameof(pluginName));
		}
		
		// Then, do we have a PluginDocsAttribute?
		Type pluginType = pluginName.GetType();

		PluginDocsAttribute? attribute = pluginType.GetCustomAttribute<PluginDocsAttribute>() ?? pluginType.Assembly.GetCustomAttribute<PluginDocsAttribute>();
		
		if (attribute is { Enabled: false })
		{
			// No, we don't. Cache the result and return null.
			return _vaults[pluginName] = null;
		}
		
		// Yes, we do. Create a new FileSystemVault.
		// Get the path to the plugin's docs directory.
		string pluginDirectory = Path.Join(_coreProperties.Path_Plugins, pluginName, attribute?.Path ?? PluginDocsAttribute.DefaultPath);
		
		// Create a new FileSystemVault, cache it, and return it.
		return _vaults[pluginName] = FileSystemVault.FromDirectory(new(pluginDirectory));
	}
}