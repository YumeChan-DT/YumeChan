using DSharpPlus;
using DSharpPlus.CommandsNext;
using Lamar;
using Microsoft.Extensions.Logging;
using Nodsoft.YumeChan.Core.Config;
using Nodsoft.YumeChan.PluginBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;



namespace Nodsoft.YumeChan.Core
{
	public class CommandHandler
	{
		public CommandsNextExtension Commands { get; internal set; }
		public CommandsNextConfiguration CommandsConfiguration { get; internal set; }

		public List<Plugin> Plugins { get; internal set; }

		internal ICoreProperties Config { get; set; }

		private readonly DiscordClient client;
		private readonly IServiceProvider services;
		private readonly ServiceRegistry registry;
		private readonly ILogger logger;
		private readonly PluginsLoader externalModulesLoader;


		public CommandHandler(DiscordClient client, ILogger<CommandHandler> logger, IServiceProvider services, ServiceRegistry registry)
		{
			this.client = client;
			this.services = services;
			this.registry = registry;
			this.logger = logger;

			externalModulesLoader = new(string.Empty);
		}


		public async Task InstallCommandsAsync()
		{
			CommandsConfiguration = new()
			{
				Services = services,
				StringPrefixes = new[] { Config.CommandPrefix }
			};

			Commands = client.UseCommandsNext(CommandsConfiguration);

			//			Commands.CommandExecuted += OnCommandExecutedAsync; // Hook execution event
			//			client.MessageReceived += HandleCommandAsync; // Hook command handler

			await RegisterCommandsAsync();
		}

		public async Task UninstallCommandsAsync()
		{
//			Commands.CommandExecuted -= OnCommandExecutedAsync;
//			client.MessageReceived -= HandleCommandAsync;

			await ReleaseCommandsAsync();
		}


/*		public void RegisterTypeReaders()
		{
			
		}
*/

		public async Task RegisterCommandsAsync()
		{
			Plugins = new() { new Modules.InternalPlugin() }; // Add YumeCore internal commands
			externalModulesLoader.LoadPluginAssemblies();

			Plugins.AddRange(from Plugin plugin
							 in externalModulesLoader.LoadPluginManifests()
							 where !Plugins.Exists(p => p?.PluginAssemblyName == plugin.PluginAssemblyName)
							 select plugin);

			foreach (Plugin plugin in Plugins)
			{
				plugin.ConfigureServices(registry);
				await plugin.LoadPlugin();
				Commands.RegisterCommands(plugin.GetType().Assembly);
				logger.LogInformation("Loaded Plugin '{Plugin}'.", plugin.PluginAssemblyName);
			}

			Commands.RegisterCommands(Assembly.GetEntryAssembly()); // Add possible Commands from Entry Assembly (contextual)
		}


		public async Task ReleaseCommandsAsync()
		{
			Commands.UnregisterCommands(Commands.RegisteredCommands.Values.ToArray());

			foreach (Plugin plugin in new List<Plugin>(Plugins.Where(p => p is not Modules.InternalPlugin)))
			{
				await plugin.UnloadPlugin();
				Plugins.Remove(plugin);

				logger.LogInformation("Removed Plugin '{Plugin}'.", plugin.PluginAssemblyName);
			}
		}

/*	private async Task HandleCommandAsync(SocketMessage arg)
		{
			if (arg is SocketUserMessage message)
			{
				int argPosition = 0;

				if (message.HasStringPrefix(Config.CommandPrefix, ref argPosition) || message.HasMentionPrefix(client.CurrentUser, ref argPosition))
				{
					SocketCommandContext context = new(client, message);

					await Commands.ExecuteAsync(context, argPosition, services).ConfigureAwait(false);
				}
			}
		}
*/

/*		public async Task OnCommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
		{
			// We have access to the information of the command executed,
			// the context of the command, and the result returned from the
			// execution in this event.

			// We can tell the user what went wrong
			if (!string.IsNullOrEmpty(result?.ErrorReason))
			{
				await context.Channel.SendMessageAsync(result.ErrorReason);
			}

			// Log the result
			await logger.Log(new LogMessage(LogSeverity.Verbose, "Commands", $"Command '{context.Message.Content}' received from User '{context.User}'."));
		}
*/

/*		public async Task LogAsync(LogMessage logMessage)
		{
			if (logMessage.Exception is CommandException cmdException)
			{
				// Inform the user that something unexpected has happened
#if DEBUG
				await cmdException.Context.Channel.SendMessageAsync(cmdException.ToString());
#else
				await cmdException.Context.Channel.SendMessageAsync("Something went wrong.");
#endif

				// Log the incident
				await logger.Log(new LogMessage(LogSeverity.Error, "Commands", $"{cmdException.Context.User} failed to execute '{cmdException.Command.Name}' in channel {cmdException.Context.Channel}.", cmdException));
			}
		}
*/
	}
}
