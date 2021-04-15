using Discord;
using Microsoft.Extensions.Logging;
using Nodsoft.YumeChan.Core.Config;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace Nodsoft.YumeChan.Core
{
	public static class Utilities
	{
		public static bool ImplementsInterface(this Type type, Type interfaceType) => type.GetInterfaces().Where(t => t == interfaceType).Select(t => new { }).Any();

		public static Task Log(this ILogger logger, LogMessage logMessage) // Adapting MS's ILogger.Log() for Discord.NET events
		{
			logger.Log
			(
				logMessage.Severity switch
				{
					LogSeverity.Critical => LogLevel.Critical,
					LogSeverity.Debug => LogLevel.Debug,
					LogSeverity.Error => LogLevel.Error,
					LogSeverity.Info => LogLevel.Information,
					LogSeverity.Verbose => LogLevel.Trace,
					LogSeverity.Warning => LogLevel.Warning,
					_ => LogLevel.None
				},
				logMessage.Exception,
				logMessage.Message,
				logMessage.Source
			);

			return Task.CompletedTask;
		}

		public static ICoreProperties PopulateCoreProperties(this ICoreProperties properties)
		{
			properties.AppInternalName ??= "YumeChan";
			properties.AppDisplayName ??= "Yume-Chan";
			properties.BotToken ??= string.Empty;
			properties.CommandPrefix ??= "==";
			properties.DatabaseProperties.ConnectionString ??= "mongodb://localhost:27017";
			properties.DatabaseProperties.DatabaseName ??= "yc-default";

			return properties;
		}
	}
}