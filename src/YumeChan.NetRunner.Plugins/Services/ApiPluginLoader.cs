using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Hosting;
using YumeChan.Core;
using YumeChan.Core.Services.Plugins;
using YumeChan.NetRunner.Plugins.Infrastructure.Api;
using YumeChan.PluginBase;

namespace YumeChan.NetRunner.Plugins.Services;

/// <summary>
/// Provides loading for YumeChan Plugins with API functionalities.
/// </summary>
public class ApiPluginLoader : IHostedService
{
	private readonly ApplicationPartManager _appPartManager;
	private readonly PluginActionDescriptorChangeProvider _descriptorChangeProvider;
	private readonly PluginLifetimeListener _lifetimeListener;

	public ApiPluginLoader(ApplicationPartManager appPartManager, PluginActionDescriptorChangeProvider descriptorChangeProvider, PluginLifetimeListener lifetimeListener)
	{
		_appPartManager = appPartManager;
		_descriptorChangeProvider = descriptorChangeProvider;
		
		// Hook up methods to listen for plugin lifetime events
		_lifetimeListener = lifetimeListener;
	}

	/// <summary>
	/// Loads the plugin's assembly as an MVC Application Part.
	/// </summary>
	/// <param name="plugin">The plugin to load assembly for.</param>
	public virtual void LoadPluginApplicationPart(IPlugin plugin)
	{
		Assembly assembly = plugin.GetType().Assembly;
		_appPartManager.ApplicationParts.Add(new AssemblyPart(assembly));
		
		// Notify the descriptor change provider that the plugin has been loaded.
		_descriptorChangeProvider.HasChanged = true;
		_descriptorChangeProvider.TokenSource.Cancel();
	}
	
	/// <summary>
	/// Unloads the plugin's assembly as an MVC Application Part.
	/// </summary>
	/// <param name="plugin">The plugin to unload assembly for.</param>
	public virtual void UnloadPluginApplicationPart(IPlugin plugin)
	{
		_appPartManager.ApplicationParts.Remove(_appPartManager.ApplicationParts.First(part => part.Name == plugin.AssemblyName));
		
		// Notify the descriptor change provider that the plugin has been unloaded.
		_descriptorChangeProvider.HasChanged = true;
		_descriptorChangeProvider.TokenSource.Cancel();
	}

	public async Task StartAsync(CancellationToken cancellationToken)
	{
		// Start by catching up on previously loaded plugins
		foreach (IPlugin plugin in YumeCore.Instance.CommandHandler.Plugins)
		{
			_appPartManager.ApplicationParts.Add(new AssemblyPart(plugin.GetType().Assembly));
		}

		// Notify the descriptor change provider that the plugins have been loaded.
		_descriptorChangeProvider.HasChanged = true;
		_descriptorChangeProvider.TokenSource.Cancel();

		// Okay! Time for work now ^^
		_lifetimeListener.PluginLoaded += LoadPluginApplicationPart;
		_lifetimeListener.PluginUnloaded += UnloadPluginApplicationPart;
	}

	public async Task StopAsync(CancellationToken cancellationToken)
	{
		_lifetimeListener.PluginLoaded -= LoadPluginApplicationPart;
		_lifetimeListener.PluginUnloaded -= UnloadPluginApplicationPart;
	}
}