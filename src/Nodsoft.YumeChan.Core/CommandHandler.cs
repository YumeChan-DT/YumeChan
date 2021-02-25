using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Lamar;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Nodsoft.YumeChan.Core.Config;
using Nodsoft.YumeChan.Core.TypeReaders;
using Nodsoft.YumeChan.PluginBase;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;



namespace Nodsoft.YumeChan.Core
{
	public class CommandHandler
	{
		public CommandService Commands { get; internal set; }
		public List<Plugin> Plugins { get; internal set; }

		internal ICoreProperties Config { get; set; }

		private readonly DiscordSocketClient client;
		private readonly IServiceProvider services;
		private readonly ServiceRegistry registry;
		private readonly ILogger logger;
		private readonly PluginsLoader externalModulesLoader;


		public CommandHandler(DiscordSocketClient client, CommandService commands, ILogger<CommandHandler> logger, IServiceProvider services, ServiceRegistry registry)
		{
			Commands = commands;
			this.client = client;
			this.services = services;
			this.registry = registry;
			this.logger = logger;
			externalModulesLoader = new(string.Empty);
		}


		public async Task InstallCommandsAsync()
		{
			Commands.Log += LogAsync; // Hook exception logging
			Commands.CommandExecuted += OnCommandExecutedAsync; // Hook execution event
			client.MessageReceived += HandleCommandAsync; // Hook command handler

			await RegisterCommandsAsync();
		}

		public async Task UninstallCommandsAsync()
		{
			Commands.Log -= LogAsync;
			Commands.CommandExecuted -= OnCommandExecutedAsync;
			client.MessageReceived -= HandleCommandAsync;

			await ReleaseCommandsAsync();
		}


		public Task RegisterTypeReaders()
		{
			Commands.AddTypeReader(typeof(IEmote), new EmoteTypeReader());

			return Task.CompletedTask;
		}

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
				await Commands.AddModulesAsync(plugin.GetType().Assembly, services);

				if (plugin is IMessageTap tap)
				{
					client.MessageReceived += tap.OnMessageReceived;
					client.MessageUpdated += tap.OnMessageUpdated;
					client.MessageDeleted += tap.OnMessageDeleted;
				}
			}

			await Commands.AddModulesAsync(Assembly.GetEntryAssembly(), services); // Add possible Commands from Entry Assembly (contextual)
		}


		public async Task ReleaseCommandsAsync()
		{
			foreach (ModuleInfo module in Commands.Modules.Where(m => m is not Modules.ICoreModule))
			{
				await Commands.RemoveModuleAsync(module).ConfigureAwait(false);
			}


			foreach (Plugin plugin in new List<Plugin>(Plugins.Where(p => p is not Modules.InternalPlugin)))
			{
				if (plugin is IMessageTap tap)
				{
					client.MessageReceived -= tap.OnMessageReceived;
					client.MessageUpdated -= tap.OnMessageUpdated;
					client.MessageDeleted -= tap.OnMessageDeleted;
				}

				await plugin.UnloadPlugin();
				Plugins.Remove(plugin);
			}
		}

		private async Task HandleCommandAsync(SocketMessage arg)
		{
			if (arg is SocketUserMessage message /*&& !message.Author.IsBot*/)
			{
				int argPosition = 0;

				if (message.HasStringPrefix(Config.CommandPrefix, ref argPosition) || message.HasMentionPrefix(client.CurrentUser, ref argPosition))
				{
					SocketCommandContext context = new(client, message);

					await Commands.ExecuteAsync(context, argPosition, services).ConfigureAwait(false);
				}
			}
		}

		public async Task OnCommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
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

		public async Task LogAsync(LogMessage logMessage)
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
	}
}
