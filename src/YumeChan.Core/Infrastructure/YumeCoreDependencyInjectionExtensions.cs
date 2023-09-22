using DSharpPlus;
using Microsoft.Extensions.Logging;
using YumeChan.Core;
using YumeChan.Core.Config;
using YumeChan.Core.Services;
using YumeChan.Core.Services.Config;
using YumeChan.Core.Services.Plugins;
using YumeChan.PluginBase.Database.MongoDB;
using YumeChan.PluginBase.Database.Postgres;
using YumeChan.PluginBase.Tools;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for the YumeCore Dependency Injection.
/// </summary>
public static class YumeCoreDependencyInjectionExtensions
{
	/// <summary>
	/// Extension method for <see cref="IServiceCollection"/> to add custom services necessary for running the YumeCore application. 
	/// This method configures a comprehensive service pipeline including various singleton services, database providers, configuration providers, and an HTTP client.
	/// </summary>
	/// <param name="services">An instance of <see cref="IServiceCollection"/>.</param>
	/// <returns>The same service collection passed in, enabling chained configuration calls.</returns>
	///
	/// <remarks>
	/// <para>Components added to the DI pipeline:</para>
	/// <list type="bullet">
	/// <item><description><see cref="YumeCore"/> - a singleton service instance for the application.</description></item>
	/// <item><description><see cref="DiscordClient"/> - a singleton service with initial settings for intents, token type, token, logger factory, and minimum log level.</description></item>
	/// <item><description><see cref="PluginsLoader"/> - a singleton that loads plugins from the path specified in <see cref="ICoreProperties"/>.</description></item>
	/// <item><description>Singleton instances of <see cref="PluginLifetimeListener.Instance"/>, <see cref="CommandHandler"/>, <see cref="LavalinkHandler"/>, <see cref="NuGetPluginsFetcher"/>, and <see cref="DiscordBotTokenProvider"/>.</description></item>
	/// <item><description>Services for MongoDB and Postgres are set up using a class <see cref="UnifiedDatabaseProvider"/>, which implements <see cref="IMongoDatabaseProvider"/> and <see cref="IPostgresDatabaseProvider"/>.</description></item>
	/// <item><description><see cref="InterfaceConfigProvider"/> and <see cref="JsonConfigProvider"/> generic services provide configuration data.</description></item>
	/// <item><description><see cref="ICoreProperties"/> and <see cref="IPluginLoaderProperties"/> instances are added with configurations loaded from 'coreconfig.json' and 'plugins.json' respectively.</description></item>
	/// <item><description>An <see cref="HttpClient"/> is registered and configured via the HttpClientFactory for making HTTP requests, and the <see cref="NuGetPluginsFetcher"/> is added to the client.</description></item>
	/// </list>
	/// </remarks>
	public static IServiceCollection AddYumeCoreServices(this IServiceCollection services)
	{
		services.AddSingleton<YumeCore>();
		services.AddSingleton<DiscordClient>(static services => new(new()
		{
			Intents = DiscordIntents.All,
			TokenType = TokenType.Bot,
			Token = services.GetRequiredService<DiscordBotTokenProvider>().GetBotToken(), // You should find a way to get this in DI context
			LoggerFactory = services.GetService<ILoggerFactory>(),
			MinimumLogLevel = LogLevel.Information
		}));
		
		services.AddSingleton<PluginsLoader>(serviceProvider => 
			new(serviceProvider.GetRequiredService<ICoreProperties>().Path_Plugins));

		services.AddSingleton(PluginLifetimeListener.Instance);
		services.AddSingleton<CommandHandler>();
		services.AddSingleton<LavalinkHandler>();
		services.AddSingleton<NugetPluginsFetcher>();
		services.AddSingleton<DiscordBotTokenProvider>();
		
		services.AddSingleton(typeof(JsonConfigProvider<>));
		services.AddSingleton(typeof(InterfaceConfigProvider<>));
		
		services.AddSingleton(typeof(IMongoDatabaseProvider<>), typeof(UnifiedDatabaseProvider<>));
		services.AddSingleton(typeof(IPostgresDatabaseProvider<>), typeof(UnifiedDatabaseProvider<>));
		services.AddSingleton(typeof(IInterfaceConfigProvider<>), typeof(InterfaceConfigProvider<>));
		services.AddSingleton(typeof(IJsonConfigProvider<>), typeof(JsonConfigProvider<>));
		
		services.AddSingleton<ICoreProperties>(sp => 
			sp.GetRequiredService<InterfaceConfigProvider<ICoreProperties>>()
				.InitConfig("coreconfig.json", true)
				.InitDefaults());
		
		services.AddSingleton<IPluginLoaderProperties>(sp => 
			sp.GetRequiredService<InterfaceConfigProvider<IPluginLoaderProperties>>()
				.InitConfig("plugins.json", true)
				.InitDefaults());
		
		services.AddHttpClient()
			.AddNuGetPluginsFetcher(); // You should implement this in NuGetPluginsFetcher class as an extension method
		
		return services;
	}
}