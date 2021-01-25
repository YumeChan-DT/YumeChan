using Discord;
using Discord.Commands;
using Discord.WebSocket;
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
		private readonly ILogger logger;
		private readonly PluginsLoader externalModulesLoader;


		public CommandHandler(DiscordSocketClient client, CommandService commands, ILogger<CommandHandler> logger, IServiceProvider services)
		{
			Commands = commands;
			this.client = client;
			this.services = services;
			this.logger = logger;
			externalModulesLoader = new(string.Empty);
		}


		public async Task InstallCommandsAsync()
		{
			client.MessageReceived += HandleCommandAsync;
			await RegisterCommandsAsync().ConfigureAwait(false);
		}

		public async Task UninstallCommandsAsync()
		{
			client.MessageReceived -= HandleCommandAsync;
			await ReleaseCommandsAsync().ConfigureAwait(false);
		}


		public Task RegisterTypeReaders()
		{
			Commands.AddTypeReader(typeof(IEmote), new EmoteTypeReader());

			return Task.CompletedTask;
		}

		public async Task RegisterCommandsAsync()
		{
			
			Plugins = new() { new Modules.InternalPlugin() };              // Add YumeCore internal commands

			await externalModulesLoader.LoadPluginAssemblies();

			Plugins.AddRange(from Plugin plugin
							 in await externalModulesLoader.LoadPluginManifests()
							 where !Plugins.Exists(p => p?.PluginAssemblyName == plugin.PluginAssemblyName)
							 select plugin);

			foreach (Plugin plugin in new List<Plugin>(Plugins))
			{
				await plugin.LoadPlugin();
				await Commands.AddModulesAsync(plugin.GetType().Assembly, services);

				if (plugin is IMessageTap tap)
				{
					client.MessageReceived += tap.OnMessageReceived;
					client.MessageUpdated += tap.OnMessageUpdated;
					client.MessageDeleted += tap.OnMessageDeleted;
				}
			}

			await Commands.AddModulesAsync(Assembly.GetEntryAssembly(), services);      // Add possible Commands from Entry Assembly (contextual)
		}


		public async Task ReleaseCommandsAsync()
		{
			foreach (ModuleInfo module in Commands.Modules.Where(m => m is not Modules.ICoreModule))
			{
				await Commands.RemoveModuleAsync(module).ConfigureAwait(false);
			}


			foreach (Plugin plugin in Plugins.Where(p => p is not Modules.InternalPlugin))
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
			if (arg is SocketUserMessage message && !message.Author.IsBot)
			{
				int argPosition = 0;

				if (message.HasStringPrefix(Config.CommandPrefix, ref argPosition) || message.HasMentionPrefix(client.CurrentUser, ref argPosition))
				{
					await logger.Log(new LogMessage(LogSeverity.Verbose, "Commands", $"Command \"{message.Content}\" received from User {message.Author.Mention}."));

					SocketCommandContext context = new(client, message);
					IResult result = await Commands.ExecuteAsync(context, argPosition, services).ConfigureAwait(false);

					if (!result.IsSuccess)
					{
						await context.Channel.SendMessageAsync($"{context.User.Mention} {result.ErrorReason}").ConfigureAwait(false);

						LogMessage logMessage = new(LogSeverity.Verbose, new StackTrace().GetFrame(1).GetMethod().Name,
							$"{context.User.Mention} : {message} \n{result}");

						await logger.Log(logMessage).ConfigureAwait(false);
					}
				}
			}
		}
	}
}
