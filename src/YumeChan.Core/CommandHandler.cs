using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Unity;
using Unity.Microsoft.DependencyInjection;
using YumeChan.Core.Config;
using YumeChan.Core.Services.Formatters;
using YumeChan.Core.Infrastructure.SlashCommands;
using YumeChan.PluginBase;
using YumeChan.PluginBase.Infrastructure;
using DSharpPlus.SlashCommands.EventArgs;
using DSharpPlus.Entities;
using YumeChan.Core.Services.Plugins;

namespace YumeChan.Core;

public class CommandHandler
{
	public CommandsNextExtension Commands { get; internal set; }
	public InteractivityExtension Interactivity { get; internal set; }
	public SlashCommandsExtension SlashCommands { get; internal set; }

	public CommandsNextConfiguration CommandsConfiguration { get; internal set; }
	public InteractivityConfiguration InteractivityConfiguration { get; internal set; }
	public SlashCommandsConfiguration SlashCommandsConfiguration { get; internal set; }

	public List<IPlugin> Plugins { get; private set; }

	internal ICoreProperties Config { get; set; }

	private readonly DiscordClient _client;
	private readonly IServiceProvider _services;
	private readonly IUnityContainer _container;
	private readonly NugetPluginsFetcher _pluginsFetcher;
	private readonly PluginLifetimeListener _pluginLifetimeListener;
	private readonly ILogger _logger;
	private readonly PluginsLoader _pluginsLoader;


	public CommandHandler(DiscordClient client, ILogger<CommandHandler> logger, IServiceProvider services, IUnityContainer container, NugetPluginsFetcher pluginsFetcher,
		PluginLifetimeListener pluginLifetimeListener)
	{
		_client = client;
		_services = services;
		_container = container;
		_pluginsFetcher = pluginsFetcher;
		_pluginLifetimeListener = pluginLifetimeListener;
		_logger = logger;

		_pluginsLoader = new(string.Empty);
	}


	public async Task InstallCommandsAsync()
	{
		CommandsConfiguration ??= new()
		{
			Services = _services,
			StringPrefixes = new[] { Config.CommandPrefix }
		};

		InteractivityConfiguration ??= new()
		{
			PaginationBehaviour = PaginationBehaviour.Ignore
		};

		SlashCommandsConfiguration ??= new()
		{
			Services = _services
		};

		Commands = _client.UseCommandsNext(CommandsConfiguration);
		Interactivity = _client.UseInteractivity(InteractivityConfiguration);
		SlashCommands = _client.UseSlashCommands(SlashCommandsConfiguration);

		Commands.CommandErrored += OnCommandErroredAsync;
		Commands.CommandExecuted += OnCommandExecuted;

		SlashCommands.SlashCommandErrored += OnSlashCommandErroredAsync;

//			Commands.CommandExecuted += OnCommandExecutedAsync; // Hook execution event
//			client.MessageReceived += HandleCommandAsync; // Hook command handler

		await RegisterCommandsAsync();

		Commands.SetHelpFormatter<HelpCommandFormatter>();
	}

	private Task OnSlashCommandErroredAsync(SlashCommandsExtension _, SlashCommandErrorEventArgs e)
	{
		_logger.LogError(e.Exception, "An error occured executing SlashCommand {Command} :", e.Context.CommandName);

#if DEBUG
		throw e.Exception;
#else
			return Task.CompletedTask;
#endif
	}

	public async Task UninstallCommandsAsync()
	{
//			Commands.CommandExecuted -= OnCommandExecutedAsync;
//			client.MessageReceived -= HandleCommandAsync;

		await ReleaseCommandsAsync();
	}

	public async Task RegisterCommandsAsync()
	{
		_logger.LogInformation("Using PluginBase v{Version}.", typeof(IPlugin).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);
		_logger.LogInformation("Current Plugins directory: {PluginsDirectory}", _pluginsLoader.PluginsLoadDirectory);

		Plugins = new() { new Modules.InternalPlugin() }; // Add YumeCore internal commands

		await _pluginsFetcher.FetchPluginsAsync();
		_pluginsLoader.ScanDirectoryForPluginFiles();
		_pluginsLoader.LoadPluginAssemblies();

		foreach (DependencyInjectionHandler handler in _pluginsLoader.LoadDependencyInjectionHandlers())
		{
			_container.AddServices(handler.ConfigureServices(new ServiceCollection()));
		}

		List<IPlugin> plugins = _pluginsLoader.LoadPluginManifests().ToList();

		// Scan for NetRunner plugins when YumeCore was loaded from a ConsoleRunner, and if DisallowNetRunnerPlugins is set to true in the config.
		if (Assembly.GetEntryAssembly()?.GetName().Name == "YumeChan.ConsoleRunner" && YumeCore.Instance.CoreProperties.DisallowNetRunnerPlugins is true
			&& plugins.Any(p => p.ShouldUseNetRunner))
		{
			throw new NotSupportedException("Attempted to load NetRunner-only plugins on a ConsoleRunner. \nIf this was intended, set DisallowNetRunnerPlugins to false in the YumeCore config.");
		}
			
		Plugins.AddRange(
			from IPlugin plugin in plugins
			where !Plugins.Exists(p => p?.AssemblyName == plugin.AssemblyName)
			select plugin);

		ulong? slashCommandsGuild = null; // Used for Development only
#if DEBUG
		slashCommandsGuild = 584445871413002242;
#endif

/*
			if (SlashCommands.Client.GatewayInfo is not null)
			{
				await SlashCommands.Client.BulkOverwriteGlobalApplicationCommandsAsync(Array.Empty<DiscordApplicationCommand>());
			}
*/

		foreach (IPlugin plugin in Plugins)
		{
			try
			{
				await plugin.LoadAsync();
				Commands.RegisterCommands(plugin.GetType().Assembly);

				SlashCommands.RegisterCommands(plugin.GetType().Assembly, slashCommandsGuild);
				_logger.LogInformation("Loaded Plugin '{Plugin}'.", plugin.AssemblyName);
				
				_pluginLifetimeListener.NotifyPluginLoaded(plugin);
			}
			catch (Exception e)
			{
				_logger.LogError(e, "An error occured while loading plugin {PluginName}", plugin.AssemblyName);

				try
				{
					await plugin.UnloadAsync();
				}
				catch {	}

#if DEBUG
				throw;
#endif
			}
		}

		Commands.RegisterCommands(Assembly.GetEntryAssembly() ?? throw new InvalidOperationException()); // Add possible Commands from Entry Assembly (contextual)

		//SlashCommands.RegisterCommands<Status>(slashCommandsGuild);
		//await SlashCommands.RefreshCommands();
	}


	public async Task ReleaseCommandsAsync()
	{
		Commands.UnregisterCommands(Commands.RegisteredCommands.Values.ToArray());

		foreach (IPlugin plugin in new List<IPlugin>(Plugins.Where(p => p is not Modules.InternalPlugin)))
		{
			await plugin.UnloadAsync();
			Plugins.Remove(plugin);

			_logger.LogInformation("Removed Plugin '{Plugin}'.", plugin.AssemblyName);
			
			_pluginLifetimeListener.NotifyPluginUnloaded(plugin);
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
						RequireOwnerAttribute             => "Sorry. You must be a Bot Owner to run this command.",
						RequireDirectMessageAttribute     => "Sorry, not here. Please send me a Direct Message with that command.",
						RequireGuildAttribute             => "Sorry, not here. Please send this command in a server.",
						RequireNsfwAttribute              => "Sorry. As much as I'd love to, I've gotta keep the hot stuff to the right channels.",
						CooldownAttribute cd              => $"Sorry. This command is on Cooldown. You can use it {cd.MaxUses} time(s) every {cd.Reset.TotalSeconds} seconds.",
						RequireUserPermissionsAttribute p => $"Sorry. You need to have permission(s) ``{p.Permissions}`` to run this.",
						_                                 => null
					});
				}
			}

			if (errorMessages.Any())
			{
				await e.Context.RespondAsync(string.Join('\n', errorMessages));
			}
		}
		else
		{
#if DEBUG
			string response = $"An error occurred : \n```{e.Exception}```";
#else
				string response = $"Something went wrong while executing your command : \n{e.Exception.Message}";
#endif

			await e.Context.RespondAsync(response);
			_logger.LogError("An error occured executing '{command}' from user '{user}' : \n{exception}", e.Command.QualifiedName, e.Context.User.Id, e.Exception);
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

	public Task OnCommandExecuted(CommandsNextExtension sender, CommandExecutionEventArgs e)
	{
		_logger.LogInformation("Command '{Command}' received from User '{User}'.", e.Command.QualifiedName, e.Context.User.Id);
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