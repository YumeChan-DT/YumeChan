using Discord;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Nodsoft.YumeChan.Core
{
	public static class Utilities
	{
		public static bool ImplementsInterface(this Type type, Type interfaceType)
		{
			foreach (Type t in type.GetInterfaces())
			{
				if (t == interfaceType)
				{
					return true;
				}
			}
			return false;
		}

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
	}
}