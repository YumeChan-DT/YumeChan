using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using YumeChan.Core;
using YumeChan.Core.Services.Plugins;
using YumeChan.NetRunner.Plugins.Infrastructure;
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
	private readonly SwaggerDocumentEnumerator _swaggerDocumentEnumerator;
	private readonly IOptions<SwaggerGenOptions> _swaggerGenOptions;
	private readonly SwaggerGeneratorOptions _swaggerGeneratorOptions;
	private readonly PluginsLoader _pluginsLoader;

	public ApiPluginLoader(ApplicationPartManager appPartManager, PluginActionDescriptorChangeProvider descriptorChangeProvider, PluginLifetimeListener lifetimeListener,
		SwaggerEndpointEnumerator swaggerEndpointEnumerator, SwaggerDocumentEnumerator swaggerDocumentEnumerator, IOptions<SwaggerGenOptions> swaggerGenOptions,
		SwaggerGeneratorOptions swaggerGeneratorOptions, PluginsLoader pluginsLoader)
	{
		_appPartManager = appPartManager;
		_descriptorChangeProvider = descriptorChangeProvider;
		_lifetimeListener = lifetimeListener;
		_swaggerEndpointEnumerator = swaggerEndpointEnumerator;
		_swaggerDocumentEnumerator = swaggerDocumentEnumerator;
		_swaggerGenOptions = swaggerGenOptions;
		_swaggerGeneratorOptions = swaggerGeneratorOptions;
		_pluginsLoader = pluginsLoader;
		_swaggerGenOptions.Value.SwaggerGeneratorOptions = swaggerGeneratorOptions;
	}

	/// <summary>
	/// Loads the plugin's assembly as an MVC Application Part.
	/// </summary>
	/// <param name="plugin">The plugin to load assembly for.</param>
	public virtual void LoadApiPlugin(IPlugin plugin)
	{
		Type type = plugin.GetType();
		Assembly assembly = type.Assembly;

		// First, let's make sure that our plugin's assembly actually defines any API controllers/endponts.
		if (assembly.ExportedTypes.Any(t => t.IsAssignableTo(typeof(ControllerBase))))
		{
			_appPartManager.ApplicationParts.Add(new AssemblyPart(assembly));

			// Notify the descriptor change provider that the plugin has been loaded.
			_descriptorChangeProvider.HasChanged = true;
			_descriptorChangeProvider.TokenSource.Cancel();

			// Generate a new Swagger document for the plugin
			_swaggerDocumentEnumerator.Documents.Add(plugin.AssemblyName, new()
				{
					Title = plugin.AssemblyName,
					Description = plugin.DisplayName,
					Version = plugin.Version
				}
			);

			// Attach XML documentation from controllers to the Swagger document
			_swaggerGenOptions.Value.TryIncludeXmlCommentsFromAssembly(assembly);
			

			// Add the document's URL to the Endpoint enumerator.
			_swaggerEndpointEnumerator.Endpoints.Add(new() { Name = plugin.AssemblyName, Url = $"/swagger/{plugin.AssemblyName}/swagger.json" });
		}
		
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
		foreach (IPlugin plugin in _pluginsLoader.PluginManifests.Values)
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