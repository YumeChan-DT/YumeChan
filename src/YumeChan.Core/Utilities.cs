using Microsoft.Extensions.Logging;
using YumeChan.Core.Config;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection;
using NuGet.Packaging;
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
			properties.DisallowNetRunnerPlugins ??= true;
			properties.MongoProperties.ConnectionString ??= "mongodb://localhost:27017";
			properties.MongoProperties.DatabaseName ??= "yc-default";
			properties.LavalinkProperties.Hostname ??= "localhost";
			properties.LavalinkProperties.Port ??= 2333;
			properties.LavalinkProperties.Password ??= string.Empty;

			return properties;
		}
		
		public static IPluginLoaderProperties InitDefaults(this IPluginLoaderProperties properties)
		{
			// Add default plugins if empty.
			if (properties.EnabledPlugins.Count is 0)
			{
				properties.EnabledPlugins.Add("YumeChan.PluginBase", "*");
			}

			// ...Same for package sources.
			if (properties.Nuget.PackageSources.Count is 0)
			{
				properties.Nuget.PackageSources.Add("https://api.nuget.org/v3/index.json");
			}

			return properties;
		}
	}
}