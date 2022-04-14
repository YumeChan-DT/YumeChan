using System.Threading.Tasks;
using YumeChan.PluginBase;

namespace YumeChan.Core.Services.Plugins;

/// <summary>
/// Provides events to process calls to the Runners when implementing custom logic for YumeChan Plugins.
/// </summary>
public class PluginLifetimeListener
{
	/// <summary>
	/// Singleton instance of the PluginLifetimeListener.
	/// </summary>
	public static PluginLifetimeListener Instance { get; } = new();
	
	public delegate void PluginLifetimeEventHandler(IPlugin plugin);
	
	/// <summary>
	/// Occurs when a plugin is loaded.
	/// </summary>
	public event PluginLifetimeEventHandler PluginLoaded;
	
	/// <summary>
	/// Occurs when a plugin is unloaded.
	/// </summary>
	public event PluginLifetimeEventHandler PluginUnloaded;
	
	internal void NotifyPluginLoaded(IPlugin plugin) => PluginLoaded?.Invoke(plugin);
	internal void NotifyPluginUnloaded(IPlugin plugin) => PluginUnloaded?.Invoke(plugin);
}