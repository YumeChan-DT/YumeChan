using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Nodsoft.YumeChan.PluginBase;
using Nodsoft.YumeChan.Core.TypeReaders;
using Microsoft.Extensions.Logging;


namespace Nodsoft.YumeChan.Core
{
	public enum YumeCoreState
	{
		Offline = 0, Online = 1, Starting = 2, Stopping = 3, Reloading = 4
	}

	[SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
	public sealed class YumeCore
	{
		// Properties

		public static YumeCore Instance { get; } = new Lazy<YumeCore>(() => new YumeCore()).Value;

		public YumeCoreState CoreState { get; private set; }

		public static Version CoreVersion { get; } = typeof(YumeCore).Assembly.GetName().Version;

		public DiscordSocketClient Client { get; set; }
		public CommandService Commands { get; set; }
		public IServiceProvider Services { get; set; }

		internal PluginsLoader ExternalModulesLoader { get; set; }
		public List<IPlugin> Plugins { get; set; }

		public ILogger Logger { get; set; }

		/**
		 * Remember to keep token private or to read it from an 
		 *	external source! In this case, we are reading the token 
		 *	from an environment variable. If you do not know how to set-
		 *	environment variables, you may find more information on 
		 *	Internet or by using other methods such as reading 
		 *	a configuration. 
		 **/
		private string BotToken { get; } = Environment.GetEnvironmentVariable("YumeChan.Token");


		// Constructors
		private YumeCore() { /** Private ctor for Singleton implementation <see cref="Instance"> **/ }
		static YumeCore() { /** Static ctor for Singleton implementation **/ }

		// Destructor
		~YumeCore()
		{
			StopBotAsync().Wait();
		}


		// Methods

		public static Task<IServiceCollection> ConfigureServices(IServiceCollection services = null)
		{
			services ??= new ServiceCollection();
			services.AddSingleton<DiscordSocketClient>()
					.AddSingleton<CommandService>()
					.AddLogging();

			return Task.FromResult(services);
		}

		public void RunBot()
		{
			StartBotAsync().Wait();
			Task.Delay(-1).Wait();
		}

		public async Task StartBotAsync()
		{
			if (Services is null)
			{
				throw new ApplicationException("Service Provider has not been defined.");
			}

			Client ??= Services.GetRequiredService<DiscordSocketClient>();
			Commands ??= Services.GetRequiredService<CommandService>();
			Logger ??= Services.GetRequiredService<ILoggerFactory>().CreateLogger<YumeCore>();

			CoreState = YumeCoreState.Starting;

			// Event Subscriptions
			Client.Log += Logger.Log;
			Commands.Log += Logger.Log;

			await RegisterTypeReadersAsync();
			await RegisterCommandsAsync().ConfigureAwait(false);

			await Client.LoginAsync(TokenType.Bot, BotToken);
			await Client.StartAsync();

			CoreState = YumeCoreState.Online;
		}

		public async Task StopBotAsync()
		{
			CoreState = YumeCoreState.Stopping;

			Services = null;
			Commands = null;

			await Client.LogoutAsync();
			await Client.StopAsync();

			Client.Dispose();
			Client = null;

			CoreState = YumeCoreState.Offline;
		}

		public async Task RestartBotAsync()
		{
			// Stop Bot
			await StopBotAsync().ConfigureAwait(true);

			// Start Bot
			await StartBotAsync().ConfigureAwait(false);
		}

		public Task RegisterTypeReadersAsync()
		{
			Commands.AddTypeReader(typeof(IEmote), new EmoteTypeReader());

			return Task.CompletedTask;
		}

		public async Task RegisterCommandsAsync()
		{
			ExternalModulesLoader = new PluginsLoader(string.Empty);

			Client.MessageReceived += HandleCommandAsync;

			Plugins = new List<IPlugin> { new Modules.InternalPlugin() };               // Add YumeCore internal commands

			await ExternalModulesLoader.LoadPluginAssemblies();
			Plugins.AddRange(await ExternalModulesLoader.LoadPluginManifests());

			List<IPlugin> modulesCopy = new List<IPlugin>(Plugins);

			Plugins.RemoveAll(plugin => plugin is null);

			foreach (IPlugin module in modulesCopy)
			{
				await module.LoadPlugin();
					await Commands.AddModulesAsync(module.GetType().Assembly, Services);

					if (module is IMessageTap tap)
					{
						Client.MessageReceived += tap.OnMessageReceived;
						Client.MessageUpdated += tap.OnMessageUpdated;
						Client.MessageDeleted += tap.OnMessageDeleted;
					}
			}

			await Commands.AddModulesAsync(Assembly.GetEntryAssembly(), Services);      // Add possible Commands from Entry Assembly (contextual)
		}

		public Task ReleaseCommands()
		{
			Client.MessageReceived -= HandleCommandAsync;

			foreach (IPlugin plugin in Plugins)
			{
				if (plugin is IMessageTap tap)
				{
					Client.MessageReceived -= tap.OnMessageReceived;
					Client.MessageUpdated -= tap.OnMessageUpdated;
					Client.MessageDeleted -= tap.OnMessageDeleted;
				}
			}

			Commands.RemoveModuleAsync<IPlugin>();

			return Task.CompletedTask;
		}

		public async Task ReloadCommandsAsync()
		{
			CoreState = YumeCoreState.Reloading;

			await ReleaseCommands().ConfigureAwait(true);

			await RegisterCommandsAsync().ConfigureAwait(false);

			CoreState = YumeCoreState.Online;
		}

		private async Task HandleCommandAsync(SocketMessage arg)
		{
			if (arg is SocketUserMessage message && !message.Author.IsBot)
			{
				int argPosition = 0;

				if (message.HasStringPrefix("==", ref argPosition) || message.HasMentionPrefix(Client.CurrentUser, ref argPosition))
				{
					await Logger.Log(new LogMessage(LogSeverity.Verbose, "Commands", $"Command \"{message.Content}\" received from User {message.Author.Mention}."));

					SocketCommandContext context = new SocketCommandContext(Client, message);
					IResult result = await Commands.ExecuteAsync(context, argPosition, Services);

					if (!result.IsSuccess)
					{
						await Logger.Log(new LogMessage(LogSeverity.Error, new StackTrace().GetFrame(1).GetMethod().Name, result.ErrorReason));
					}
				}
			}
		}
	}
}
