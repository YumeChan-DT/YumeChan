using Microsoft.Extensions.Logging;
using YumeChan.Core.Config;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection;
using static DSharpPlus.Entities.DiscordEmbedBuilder;

namespace YumeChan.Core
{
	public static class Utilities
	{
		public static EmbedFooter DefaultCoreFooter { get; } = new()
		{
			Text = $"{YumeCore.Instance.CoreProperties.AppDisplayName} v{YumeCore.CoreVersion} - Powered by Nodsoft Systems"
		};

		public static bool ImplementsInterface(this Type type, Type interfaceType) => type.GetInterfaces().Any(t => t == interfaceType);

		public static ICoreProperties InitDefaults(this ICoreProperties properties)
		{
			properties.AppInternalName ??= "YumeChan";
			properties.AppDisplayName ??= "Yume-Chan";
			properties.BotToken ??= string.Empty;
			properties.CommandPrefix ??= "==";
			properties.MongoProperties.ConnectionString ??= "mongodb://localhost:27017";
			properties.MongoProperties.DatabaseName ??= "yc-default";
			properties.LavalinkProperties.Hostname ??= "localhost";
			properties.LavalinkProperties.Port ??= 2333;
			properties.LavalinkProperties.Password ??= string.Empty;

			return properties;
		}
		
		public static IPluginLoaderProperties InitDefaults(this IPluginLoaderProperties properties)
		{
			properties.Nuget.PackageSources ??= new() { "https://api.nuget.org/v3/index.json" };
			properties.EnabledPlugins ??= new();
			properties.DisabledPlugins ??= new();

			return properties;
		}
	}
}