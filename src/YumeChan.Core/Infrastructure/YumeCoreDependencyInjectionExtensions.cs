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