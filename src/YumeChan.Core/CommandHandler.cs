using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Unity;
using Unity.Microsoft.DependencyInjection;
using YumeChan.Core.Config;
using YumeChan.Core.Services.Formatters;
using YumeChan.PluginBase;
using YumeChan.PluginBase.Infrastructure;



namespace YumeChan.Core
{
	public class CommandHandler
	{
		public CommandsNextExtension Commands { get; internal set; }
		public InteractivityExtension Interactivity { get; internal set; }

		public CommandsNextConfiguration CommandsConfiguration { get; internal set; }
		public InteractivityConfiguration InteractivityConfiguration { get; internal set; }

		public List<Plugin> Plugins { get; internal set; }

		internal ICoreProperties Config { get; set; }

		private readonly DiscordClient client;
		private readonly IServiceProvider services;
		private readonly IUnityContainer container;
		private readonly ILogger logger;
		private readonly PluginsLoader externalModulesLoader;


		public CommandHandler(DiscordClient client, ILogger<CommandHandler> logger, IServiceProvider services, IUnityContainer container)
		{
			this.client = client;
			this.services = services;
			this.container = container;
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

			InteractivityConfiguration = new()
			{
				PaginationBehaviour = PaginationBehaviour.Ignore
			};

			Commands = client.UseCommandsNext(CommandsConfiguration);
			Interactivity = client.UseInteractivity(InteractivityConfiguration);

			Commands.CommandErrored += OnCommandErroredAsync;
			Commands.CommandExecuted += OnCommandExecuted;

//			Commands.CommandExecuted += OnCommandExecutedAsync; // Hook execution event
//			client.MessageReceived += HandleCommandAsync; // Hook command handler

			await RegisterCommandsAsync();

			Commands.SetHelpFormatter<HelpCommandFormatter>();
		}

		public async Task UninstallCommandsAsync()
		{
//			Commands.CommandExecuted -= OnCommandExecutedAsync;
//			client.MessageReceived -= HandleCommandAsync;

			await ReleaseCommandsAsync();
		}

		public async Task RegisterCommandsAsync()
		{
			logger.LogInformation("Using PluginBase v{version}.", typeof(Plugin).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion);
			logger.LogInformation("Current Plugins directory: {pluginsDirectory}", externalModulesLoader.PluginsLoadDirectory);

			Plugins = new() { new Modules.InternalPlugin() }; // Add YumeCore internal commands
			externalModulesLoader.LoadPluginAssemblies();

			Plugins.AddRange(from Plugin plugin
							 in externalModulesLoader.LoadPluginManifests()
							 where !Plugins.Exists(p => p?.PluginAssemblyName == plugin.PluginAssemblyName)
							 select plugin);

			foreach (InjectionRegistry injectionRegistry in externalModulesLoader.LoadInjectionRegistries())
			{
				container.AddServices(injectionRegistry.ConfigureServices(new ServiceCollection()));
			}

			foreach (Plugin plugin in Plugins)
			{
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

		internal async Task OnCommandErroredAsync(CommandsNextExtension _, CommandErrorEventArgs e)
		{
			if (e.Exception is ChecksFailedException cf)
			{
				List<string> errorMessages = new();

				foreach (CheckBaseAttribute check in cf.FailedChecks)
				{
					if (check is PluginCheckBaseAttribute pluginCkeck)
					{
						errorMessages.Add(pluginCkeck.ErrorMessage);
					}
					else
					{
						errorMessages.Add(check switch
						{
							RequireOwnerAttribute => $"Sorry. You must be a Bot Owner to run this command.",
							RequireDirectMessageAttribute => "Sorry, not here. Please send me a Direct Message with that command.",
							RequireGuildAttribute => "Sorry, not here. Please send this command in a server.",
							RequireNsfwAttribute => "Sorry. As much as I'd love to, I've gotta keep the hot stuff to the right channels.",
							CooldownAttribute cd => $"Sorry. This command is on Cooldown. You can use it {cd.MaxUses} time(s) every {cd.Reset.TotalSeconds} seconds.",
							RequireUserPermissionsAttribute p => $"Sorry. You need to have permission(s) ``{p.Permissions}`` to run this.",
							_ => null
						});
					}
				}

				if (errorMessages.Any())
				{
					await e.Context.RespondAsync(string.Join('\n', errorMessages));
				}
			}

#if DEBUG
			string response = $"An error occurred : \n```{e.Exception}```";
#else
			string response = $"Something went wrong while executing your command : \n\n{e.Exception.Message}";
#endif

			await e.Context.RespondAsync(response);
			logger.LogError("An error occured executing '{0}' from user '{1}' : \n{2}", e.Command.QualifiedName, e.Context.User.Id, e.Exception);
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

		public Task OnCommandExecuted(CommandsNextExtension sender, CommandExecutionEventArgs e)
		{
			logger.LogDebug("Command '{0}' received from User '{1}'.", e.Command.QualifiedName, e.Context.User.Id);
			return Task.CompletedTask;
		}



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
