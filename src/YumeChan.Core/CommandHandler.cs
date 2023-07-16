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
using System.Reflection;
using DryIoc;
using DryIoc.Microsoft.DependencyInjection;
using YumeChan.Core.Config;
using YumeChan.Core.Services.Formatters;
using YumeChan.PluginBase;
using YumeChan.PluginBase.Infrastructure;
using DSharpPlus.SlashCommands.EventArgs;
using DSharpPlus.SlashCommands.Attributes;
using YumeChan.Core.Services.Plugins;

#nullable enable
namespace YumeChan.Core;

public sealed class CommandHandler
{
	public CommandsNextExtension Commands { get; internal set; } = null!;
	public InteractivityExtension Interactivity { get; internal set; } = null!;
	public SlashCommandsExtension SlashCommands { get; internal set; } = null!;

	public CommandsNextConfiguration? CommandsConfiguration { get; internal set; }
	public InteractivityConfiguration? InteractivityConfiguration { get; internal set; }
	public SlashCommandsConfiguration? SlashCommandsConfiguration { get; internal set; }

	internal ICoreProperties? Config { get; set; }

	private readonly DiscordClient _client;
	private readonly IServiceProvider _services;
	private readonly IContainer _container;
	private readonly NugetPluginsFetcher _pluginsFetcher;
	private readonly PluginLifetimeListener _pluginLifetimeListener;
	private readonly ILogger<CommandHandler> _logger;
	private readonly PluginsLoader _pluginsLoader;

	private static readonly ulong? SlashCommandsGuild; // Used for Development only

	static CommandHandler()
	{
#if DEBUG
		SlashCommandsGuild = 584445871413002242;
#endif
	}

	
	public CommandHandler(DiscordClient client, ILogger<CommandHandler> logger, IServiceProvider services, IContainer container, NugetPluginsFetcher pluginsFetcher,
		PluginLifetimeListener pluginLifetimeListener, PluginsLoader pluginsLoader)
	{
		_client = client;
		_services = services;
		_container = container;
		_pluginsFetcher = pluginsFetcher;
		_pluginLifetimeListener = pluginLifetimeListener;
		_logger = logger;

		_pluginsLoader = pluginsLoader;
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
		SlashCommands.ContextMenuErrored += OnContextMenuErroredAsync;

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
		_logger.LogInformation("Using PluginBase v{version}.", typeof(IPlugin).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);
		_logger.LogInformation("Current Plugins directory: {pluginsDirectory}", _pluginsLoader.PluginsLoadDirectory);

		await _pluginsFetcher.FetchPluginsAsync();
		_pluginsLoader.ScanDirectoryForPluginFiles();
		_pluginsLoader.LoadPluginAssemblies();

		foreach (DependencyInjectionHandler handler in _pluginsLoader.LoadDependencyInjectionHandlers())
		{
			_container.Populate(handler.ConfigureServices(new ServiceCollection()));
		}

		Dictionary<Assembly, Type> pluginManifestTypes = _pluginsLoader.GetPluginManifestTypes();
		_container.RegisterMany(pluginManifestTypes.Values, serviceTypeCondition: type => type.IsAssignableTo(typeof(IPlugin)));
		_pluginsLoader.LoadPluginManifests(pluginManifestTypes);
		
		// Add YumeCore internal commands
		_pluginsLoader.ImportPlugin(new Modules.InternalPlugin());

		// FIXME: Use the new IRunnerContext interface
		// Scan for NetRunner plugins when YumeCore was loaded from a ConsoleRunner, and if DisallowNetRunnerPlugins is set to true in the config.
		if (Assembly.GetEntryAssembly()?.GetName().Name == "YumeChan.ConsoleRunner" && YumeCore.Instance.CoreProperties.DisallowNetRunnerPlugins is true
			&& _pluginsLoader.PluginManifests.Values.Any(p => p.ShouldUseNetRunner))
		{
			throw new NotSupportedException("Attempted to load NetRunner-only plugins on a ConsoleRunner. \nIf this was intended, set DisallowNetRunnerPlugins to false in the YumeCore config.");
		}

		foreach (IPlugin plugin in _pluginsLoader.PluginManifests.Values)
		{
			try
			{
				await LoadPluginAsync(plugin, SlashCommandsGuild);
			}
			catch (Exception e)
			{
				_logger.LogError(e, "An error occured while loading plugin {PluginName}", plugin.AssemblyName);

				try
				{
					await plugin.UnloadAsync();
				}
				catch { /* Ignore any errors. */ }

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
		

		// FIXME: Faulty unloading of SlashCommands
		//
		// if (_slashCommandsGuild is not null)
		// {
		// 	await _client.BulkOverwriteGuildApplicationCommandsAsync(_slashCommandsGuild.Value, Array.Empty<DiscordApplicationCommand>());
		// }
		
		
		foreach (IPlugin plugin in _pluginsLoader.PluginManifestsInternal.Values.Where(p => p is not Modules.InternalPlugin).ToArray())
		{
			try
			{
				await plugin.UnloadAsync();
				_pluginsLoader.PluginManifestsInternal.Remove(plugin.AssemblyName);

				_logger.LogInformation("Removed Plugin '{Plugin}'", plugin.AssemblyName);
				_pluginLifetimeListener.NotifyPluginUnloaded(plugin);
			}
			catch (Exception e)
			{
				_logger.LogError(e, "An error occured while unloading plugin '{PluginName}'", plugin.AssemblyName);
				
#if DEBUG
				throw;
#endif
			}
		}
	}

	private async Task LoadPluginAsync(IPlugin plugin, ulong? slashCommandsGuild)
	{
		await plugin.LoadAsync();
		Commands.RegisterCommands(plugin.GetType().Assembly);

		SlashCommands.RegisterCommands(plugin.GetType().Assembly, slashCommandsGuild);
		_logger.LogInformation("Loaded Plugin '{Plugin}'", plugin.AssemblyName);

		_pluginLifetimeListener.NotifyPluginLoaded(plugin);
	}

	private async Task OnCommandErroredAsync(CommandsNextExtension _, CommandErrorEventArgs e)
	{
		if (e.Exception is ChecksFailedException cf)
		{
			string?[] errorMessages = cf.FailedChecks.Select(check => check switch
			{
				PluginCheckBaseAttribute p => p.ErrorMessage,
				RequireOwnerAttribute => "Sorry. You must be a Bot Owner to run this command.",
				RequireDirectMessageAttribute => "Sorry, not here. Please send me a Direct Message with that command.",
				RequireGuildAttribute => "Sorry, not here. Please send this command in a server.",
				RequireNsfwAttribute => "Sorry. As much as I'd love to, I've gotta keep the hot stuff to the right channels.",
				CooldownAttribute cd => $"Sorry. This command is on Cooldown. You can use it {cd.MaxUses} time(s) every {cd.Reset.TotalSeconds} seconds.",
				RequireUserPermissionsAttribute p => $"Sorry. You need to have permission(s) ``{p.Permissions}`` to run this.",
				_ => null
			}).ToArray();

			if (errorMessages.Length is not 0)
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
			_logger.LogError(e.Exception, "An error occured executing '{Command}' from user '{User}'", e.Command?.QualifiedName, e.Context.User.Id);
		}
	}

	private async Task OnSlashCommandErroredAsync(SlashCommandsExtension _, SlashCommandErrorEventArgs e)
	{
		if (e.Exception is SlashExecutionChecksFailedException cf)
		{
			string?[] errorMessages = cf.FailedChecks.Select(check => check switch
			{
				PluginSlashCheckBaseAttribute p => p.ErrorMessage,
				SlashRequireOwnerAttribute => "Sorry. You must be a Bot Owner to run this command.",
				SlashRequireDirectMessageAttribute => "Sorry, not here. Please send me a Direct Message with that command.",
				SlashRequireGuildAttribute => "Sorry, not here. Please send this command in a server.",
//				SlashRequireNsfwAttribute => "Sorry. As much as I'd love to, I've gotta keep the hot stuff to the right channels.",
//				SlashCooldownAttribute cd => $"Sorry. This command is on Cooldown. You can use it {cd.MaxUses} time(s) every {cd.Reset.TotalSeconds} seconds.",
				SlashRequireUserPermissionsAttribute p => $"Sorry. You need to have permission(s) ``{p.Permissions}`` to run this.",
				_ => null
			}).ToArray();

			if (errorMessages.Length is not 0)
			{
				await e.Context.CreateResponseAsync(string.Join('\n', errorMessages), true);
			}
		}
		else
		{
#if DEBUG
			string response = $"An error occurred : \n```{e.Exception}```";
#else
				string response = $"Something went wrong while executing your command : \n{e.Exception.Message}";
#endif

			await e.Context.CreateResponseAsync(response, true);
			_logger.LogError(e.Exception, "An error occured executing '{Command}' from user '{User}'", e.Context.CommandName, e.Context.User.Id);
		}
	}
	
	private async Task OnContextMenuErroredAsync(SlashCommandsExtension _, ContextMenuErrorEventArgs e)
	{
		if (e.Exception is ContextMenuExecutionChecksFailedException cf)
		{
			string?[] errorMessages = cf.FailedChecks.Select(check => check switch
			{
				PluginContextCheckBaseAttribute p => p.ErrorMessage,
//				RequireOwnerAttribute => "Sorry. You must be a Bot Owner to run this command.",
//				RequireDirectMessageAttribute => "Sorry, not here. Please send me a Direct Message with that command.",
//				RequireGuildAttribute => "Sorry, not here. Please send this command in a server.",
//				RequireNsfwAttribute => "Sorry. As much as I'd love to, I've gotta keep the hot stuff to the right channels.",
//				CooldownAttribute cd => $"Sorry. This command is on Cooldown. You can use it {cd.MaxUses} time(s) every {cd.Reset.TotalSeconds} seconds.",
//				RequireUserPermissionsAttribute p => $"Sorry. You need to have permission(s) ``{p.Permissions}`` to run this.",
				_ => null
			}).ToArray();

			if (errorMessages.Length is not 0)
			{
				await e.Context.CreateResponseAsync(string.Join('\n', errorMessages), true);
			}
		}
		else
		{
#if DEBUG
			string response = $"An error occurred : \n```{e.Exception}```";
#else
			string response = $"Something went wrong while executing your command : \n{e.Exception.Message}";
#endif

			await e.Context.CreateResponseAsync(response, true);
			_logger.LogError(e.Exception, "An error occured executing '{Command}' from user '{User}'", e.Context.CommandName, e.Context.User.Id);
		}
	}

	private Task OnCommandExecuted(CommandsNextExtension sender, CommandExecutionEventArgs e)
	{
		_logger.LogInformation("Command '{Command}' received from User '{User}'", e.Command.QualifiedName, e.Context.User.Id);
		return Task.CompletedTask;
	}
}