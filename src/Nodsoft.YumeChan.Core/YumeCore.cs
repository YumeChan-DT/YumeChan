using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nodsoft.YumeChan.Core.Config;
using Nodsoft.YumeChan.Core.Tools;
using Nodsoft.YumeChan.PluginBase.Tools;
using Nodsoft.YumeChan.PluginBase.Tools.Data;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Nodsoft.YumeChan.Core
{
	public enum YumeCoreState
	{
		Offline = 0, Online = 1, Starting = 2, Stopping = 3, Reloading = 4
	}

	public sealed class YumeCore
	{
		// Properties

		public static YumeCore Instance { get; } = new Lazy<YumeCore>(() => new()).Value;
		public YumeCoreState CoreState { get; private set; }
		public static Version CoreVersion { get; } = typeof(YumeCore).Assembly.GetName().Version;

		public DiscordSocketClient Client { get; set; }
		public CommandHandler CommandHandler { get; set; }
		public IServiceProvider Services { get; set; }

		internal ILogger Logger { get; set; }

		internal ConfigurationProvider<ICoreProperties> ConfigProvider { get; private set; }
		public ICoreProperties CoreProperties { get; private set; }

		// Constructors
		static YumeCore() { /* Static ctor for Singleton implementation */ }


		// Destructor
		~YumeCore()
		{
			StopBotAsync().Wait();
		}


		// Methods

		public static IServiceCollection ConfigureServices() => ConfigureServices(new ServiceCollection());
		public static IServiceCollection ConfigureServices(IServiceCollection services) => services
			.AddSingleton<DiscordSocketClient>()
			.AddSingleton<CommandService>()
			.AddSingleton<CommandHandler>()
			.AddHttpClient()
			.AddSingleton(typeof(IDatabaseProvider<>), typeof(DatabaseProvider<>))
			.AddSingleton(typeof(IConfigProvider<>), typeof(ConfigurationProvider<>))
			.AddLogging();

		public async Task StartBotAsync()
		{
			if (Services is null)
			{
				throw new InvalidOperationException("Service Provider has not been defined.", new ArgumentNullException(nameof(Services)));
			}

			ResolveCoreComponents();

			CoreState = YumeCoreState.Starting;

			// Event Subscriptions
			Client.Log += Logger.Log;
			CommandHandler.Commands.Log += Logger.Log;

			await CommandHandler.RegisterTypeReaders();
			await CommandHandler.InstallCommandsAsync();

			await Client.LoginAsync(TokenType.Bot, await GetBotTokenAsync());
			await Client.StartAsync();

			CoreState = YumeCoreState.Online;
		}

		public async Task StopBotAsync()
		{
			CoreState = YumeCoreState.Stopping;



			await Client.LogoutAsync();
			await Client.StopAsync();

			Client.Log -= Logger.Log;
			CommandHandler.Commands.Log -= Logger.Log;

			CoreState = YumeCoreState.Offline;
		}

		public async Task RestartBotAsync()
		{
			// Stop Bot
			await StopBotAsync().ConfigureAwait(true);

			// Start Bot
			await StartBotAsync().ConfigureAwait(false);
		}


		public async Task ReloadCommandsAsync()
		{
			CoreState = YumeCoreState.Reloading;
			await CommandHandler.ReleaseCommandsAsync();
			await CommandHandler.RegisterCommandsAsync();
			CoreState = YumeCoreState.Online;
		}


		private async Task<string> GetBotTokenAsync()
		{
			string token = CoreProperties.BotToken;

			if (string.IsNullOrWhiteSpace(token))
			{
				string envVarName = $"{CoreProperties.AppInternalName}.Token";

				if (await TryBotTokenFromEnvironment(envVarName, out token, out EnvironmentVariableTarget target))
				{
					Logger.LogInformation($"Bot Token was read from {target} Environment Variable \"{envVarName}\", instead of \"coreproperties.json\" Config File.");
				}
				else
				{
					ApplicationException e = new("No Bot Token supplied.");
					Logger.LogCritical(e, $"No Bot Token was found in \"coreproperties.json\" Config File, and Environment Variables \"{envVarName}\" from relevant targets are empty. " +
											$"\nPlease set a Bot Token before launching the Bot.");
					throw e;
				}
			}

			return token;
		}

		private static Task<bool> TryBotTokenFromEnvironment(string envVarName, out string token, out EnvironmentVariableTarget foundFromTarget)
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				foreach (EnvironmentVariableTarget target in typeof(EnvironmentVariableTarget).GetEnumValues())
				{
					token = Environment.GetEnvironmentVariable(envVarName, target);

					if (token is not null)
					{
						foundFromTarget = target;
						return Task.FromResult(true);
					}
				}

				token = null;
				foundFromTarget = default;
				return Task.FromResult(false);
			}
			else
			{
				token = Environment.GetEnvironmentVariable(envVarName);

				foundFromTarget = EnvironmentVariableTarget.Process;
				return Task.FromResult(token is not null);
			}
		}

		private void ResolveCoreComponents()
		{
			Client ??= Services.GetRequiredService<DiscordSocketClient>();
			CommandHandler ??= Services.GetRequiredService<CommandHandler>();
			Logger ??= Services.GetRequiredService<ILoggerFactory>().CreateLogger<YumeCore>();
			ConfigProvider ??= Services.GetRequiredService<IConfigProvider<ICoreProperties>>() as ConfigurationProvider<ICoreProperties>;
			CoreProperties = ConfigProvider.InitConfig("coreconfig.json", true).PopulateCoreProperties();

			CoreProperties.Path_Core ??= Directory.GetCurrentDirectory();
			CoreProperties.Path_Plugins ??= CoreProperties.Path_Core + Path.DirectorySeparatorChar + "Plugins";
			CoreProperties.Path_Config ??= CoreProperties.Path_Core + Path.DirectorySeparatorChar + "Config";

			CommandHandler.Config ??= CoreProperties;
		}
	}
}
