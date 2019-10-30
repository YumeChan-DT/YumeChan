using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;

using Nodsoft.YumeChan.PluginBase;
using Nodsoft.YumeChan.Core.TypeReaders;

namespace Nodsoft.YumeChan.Core
{
	public enum YumeCoreState
	{
		Offline = 0, Online = 1, Starting = 2, Stopping = 3, Reloading = 4
	}

	public sealed class YumeCore
	{
		// Properties

		public static YumeCore Instance { get => lazyInstance.Value; }

		public YumeCoreState CoreState { get; private set; }

		public static Version CoreVersion { get; } = typeof(YumeCore).Assembly.GetName().Version;

		public DiscordSocketClient Client { get; set; }
		public CommandService Commands { get; set; }
		public IServiceProvider Services { get; set; }

		internal ModulesLoader ExternalModulesLoader { get; set; }
		public List<IPlugin> Plugins { get; set; }

		/**
		 * Remember to keep token private or to read it from an 
		 *	external source! In this case, we are reading the token 
		 *	from an environment variable. If you do not know how to set-
		 *	environment variables, you may find more information on 
		 *	Internet or by using other methods such as reading 
		 *	a configuration. 
		 **/
		private string BotToken { get; } = Environment.GetEnvironmentVariable("YumeChan.Token");

		public ILogger Logger { get; set; }

		// Fields
		private static readonly Lazy<YumeCore> lazyInstance = new Lazy<YumeCore>(() => new YumeCore());

		// Constructors

		private YumeCore() { /* Use this ctor when assigning Logger berore running. */ }
		static YumeCore() { /* Static ctor for Singleton implementation */ }

		// Destructor
		~YumeCore()
		{
			StopBotAsync().Wait();
		}


		// Methods

		public void RunBot()
		{
			StartBotAsync().Wait();
			Task.Delay(-1).Wait();
		}

		public async Task StartBotAsync()
		{
			if (Logger is null)
			{
				throw new ApplicationException();
			}

			CoreState = YumeCoreState.Starting;

			Client = new DiscordSocketClient();
			Commands = new CommandService();

			Services = new ServiceCollection()
				.AddSingleton(Client)
				.AddSingleton(Commands)
				.BuildServiceProvider();


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
			Commands.AddTypeReader(typeof(IEmote), new IEmoteTypeReader());

			return Task.CompletedTask;
		}

		public async Task RegisterCommandsAsync()
		{
			ExternalModulesLoader = new ModulesLoader(string.Empty);

			Client.MessageReceived += HandleCommandAsync;

			Plugins = new List<IPlugin> { new Modules.InternalPlugin() };               // Add YumeCore internal commands

			await ExternalModulesLoader.LoadModuleAssemblies();
			Plugins.AddRange(await ExternalModulesLoader.LoadModuleManifests());

			List<IPlugin> modulesCopy = new List<IPlugin>(Plugins);

			foreach (IPlugin module in modulesCopy)
			{
				if (module is null)
				{
					Plugins.Remove(module);
				}
				else
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

			Commands = new CommandService();
			Commands.Log += Logger.Log;

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
					SocketCommandContext context = new SocketCommandContext(Client, message);
					IResult result = await Commands.ExecuteAsync(context, argPosition, Services);

					if (!result.IsSuccess)
					{
						await Logger.Log(new LogMessage(LogSeverity.Error, new StackTrace().GetFrame(1).GetMethod().Name, result.ErrorReason));
					}
				}
			}
		}

		// Fluent Assignments
		public void SetLogger(ILogger logger) => Logger = logger;
	}
}
