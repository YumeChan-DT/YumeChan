using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

using Nodsoft.YumeChan.Modules;


namespace Nodsoft.YumeChan.Core
{
	public class YumeCore
	{
		//Properties

		public bool IsBotOnline { get; private set; }

		public DiscordSocketClient Client { get; set; }
		public CommandService Commands { get; set; }
		public IServiceProvider Services { get; set; }

		// Remember to keep token private or to read it from an 
		// external source! In this case, we are reading the token 
		// from an environment variable. If you do not know how to set-up
		// environment variables, you may find more information on the 
		// Internet or by using other methods such as reading from 
		// a configuration.
		private string BotToken { get; } = Environment.GetEnvironmentVariable("YumeChan.Token");

		public ILogger Logger { get; protected set; }

		//Constructors

		public YumeCore(ILogger logger) => Logger = logger;


		//Methods

		public void RunBot()
		{
			StartBotAsync().Wait();
			Task.Delay(-1).Wait();
		}

		public async Task StartBotAsync()
		{
			Client = new DiscordSocketClient();
			Commands = new CommandService();

			Services = new ServiceCollection()
				.AddSingleton(Client)
				.AddSingleton(Commands)
				.BuildServiceProvider();


			//Event Subscriptions
			Client.Log += Logger.Log;
			Commands.Log += Logger.Log;

			await RegisterCommandsAsync().ConfigureAwait(false);
			await Client.LoginAsync(TokenType.Bot, BotToken);
			await Client.StartAsync();

			IsBotOnline = true;
		}

		public async Task RegisterCommandsAsync()
		{
			Client.MessageReceived += HandleCommandAsync;

			ModulesIndex.CoreVersion = typeof(YumeCore).Assembly.GetName().Version;

			await Commands.AddModulesAsync(Assembly.GetEntryAssembly(), Services);		//Add possible Commands from Entry Assembly (contextual)
			await Commands.AddModulesAsync(typeof(YumeCore).Assembly, Services);		//Add Local Commands (if any)
			await Commands.AddModulesAsync(typeof(ModulesIndex).Assembly, Services);	//Add Commands from Nodsoft.YumeChan.Modules
		}

		private async Task HandleCommandAsync(SocketMessage arg)
		{
			SocketUserMessage message = arg as SocketUserMessage;

			if (message != null && !message.Author.IsBot)
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
	}
}
