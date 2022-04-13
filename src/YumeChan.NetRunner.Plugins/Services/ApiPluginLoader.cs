using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using Unity;
using Unity.Microsoft.DependencyInjection;
using YumeChan.Core;
using YumeChan.Core.Services.Plugins;
using YumeChan.NetRunner.Plugins.Infrastructure.Api;
using YumeChan.NetRunner.Plugins.Infrastructure.Swagger;
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
	private readonly SwaggerEndpointEnumerator _swaggerEndpointEnumerator;
	private readonly ISwaggerProvider _swaggerProvider;
	private readonly IUnityContainer _container;

	public ApiPluginLoader(ApplicationPartManager appPartManager, PluginActionDescriptorChangeProvider descriptorChangeProvider, PluginLifetimeListener lifetimeListener,
		SwaggerEndpointEnumerator swaggerEndpointEnumerator, ISwaggerProvider swaggerProvider, IUnityContainer container)
	{
		_appPartManager = appPartManager;
		_descriptorChangeProvider = descriptorChangeProvider;
		
		// Hook up methods to listen for plugin lifetime events
		_lifetimeListener = lifetimeListener;
		_swaggerEndpointEnumerator = swaggerEndpointEnumerator;
		_swaggerProvider = swaggerProvider;
		_container = container;
	}

	/// <summary>
	/// Loads the plugin's assembly as an MVC Application Part.
	/// </summary>
	/// <param name="plugin">The plugin to load assembly for.</param>
	public virtual void LoadApiPlugin(IPlugin plugin)
	{
		Assembly assembly = plugin.GetType().Assembly;
		_appPartManager.ApplicationParts.Add(new AssemblyPart(assembly));
		
		// Notify the descriptor change provider that the plugin has been loaded.
		_descriptorChangeProvider.HasChanged = true;
		_descriptorChangeProvider.TokenSource.Cancel();
		
		// Generate a new Swagger document for the plugin
		ServiceCollection s = new();
		
		s.ConfigureSwaggerGen(o => o.SwaggerDoc(plugin.AssemblyName, new()
		{
			Title = plugin.AssemblyName, 
			Description = plugin.DisplayName,
			Version = plugin.Version
		}));
		
		_container.AddServices(s);

		// Add the document's URL to the Endpoint enumerator.
		_swaggerEndpointEnumerator.Endpoints.Add(new() { Name = plugin.AssemblyName, Url = $"/swagger/{plugin.AssemblyName}/swagger.json" });
	}
	
	/// <summary>
	/// Unloads the plugin's assembly as an MVC Application Part.
	/// </summary>
	/// <param name="plugin">The plugin to unload assembly for.</param>
	public virtual void UnloadApiPlugin(IPlugin plugin)
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
			LoadApiPlugin(plugin);
		}

		// Notify the descriptor change provider that the plugins have been loaded.
		_descriptorChangeProvider.HasChanged = true;
		_descriptorChangeProvider.TokenSource.Cancel();

		// Okay! Time for work now ^^
		_lifetimeListener.PluginLoaded += LoadApiPlugin;
		_lifetimeListener.PluginUnloaded += UnloadApiPlugin;
	}

	public async Task StopAsync(CancellationToken cancellationToken)
	{
		_lifetimeListener.PluginLoaded -= LoadApiPlugin;
		_lifetimeListener.PluginUnloaded -= UnloadApiPlugin;
	}
}