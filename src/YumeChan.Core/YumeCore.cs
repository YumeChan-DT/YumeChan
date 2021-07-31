using DSharpPlus;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Unity;
using YumeChan.Core.Config;
using YumeChan.Core.Services;
using YumeChan.PluginBase.Tools;
using YumeChan.PluginBase.Tools.Data;

namespace YumeChan.Core
{
	public enum YumeCoreState
	{
		Offline = 0, Online = 1, Starting = 2, Stopping = 3, Reloading = 4
	}

	public sealed class YumeCore
	{
		public static YumeCore Instance => instance ??= new();
		private static YumeCore instance;

		public YumeCoreState CoreState { get; private set; }
		public static string CoreVersion { get; } = typeof(YumeCore).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

		public DiscordClient Client { get; set; }
		public CommandHandler CommandHandler { get; set; }
		public IUnityContainer Services { get; set; }

		internal ILogger Logger { get; set; }

		internal ConfigurationProvider<ICoreProperties> ConfigProvider { get; private set; }
		public ICoreProperties CoreProperties { get; private set; }


		public YumeCore() { }

		~YumeCore()
		{
			StopBotAsync().Wait();
		}

		public IUnityContainer ConfigureContainer(IUnityContainer services) => services
			.RegisterFactory<DiscordClient>((services) => new DiscordClient(new()
			{
				Intents = DiscordIntents.All,
				TokenType = TokenType.Bot,
				Token = GetBotToken(),
				LoggerFactory = services.Resolve<ILoggerFactory>(),
				MinimumLogLevel = LogLevel.Information
			}))
			.RegisterSingleton<CommandHandler>()
			.RegisterSingleton(typeof(IDatabaseProvider<>), typeof(DatabaseProvider<>))
			.RegisterSingleton(typeof(IConfigProvider<>), typeof(ConfigurationProvider<>));


		public async Task StartBotAsync()
		{
			if (Services is null)
			{
				throw new InvalidOperationException("Service Provider has not been defined.", new ArgumentNullException(nameof(Services)));
			}

			ResolveCoreComponents();

			CoreState = YumeCoreState.Starting;


			await Client.ConnectAsync();
			await Client.InitializeAsync();

//			await CommandHandler.RegisterTypeReaders();

			await CommandHandler.InstallCommandsAsync();

			CoreState = YumeCoreState.Online;
		}

		public async Task StopBotAsync()
		{
			CoreState = YumeCoreState.Stopping;

			await CommandHandler.ReleaseCommandsAsync();

			await Client.DisconnectAsync();
			Client.Dispose();

			CoreState = YumeCoreState.Offline;
		}

		public async Task RestartBotAsync()
		{
			// Stop Bot
			await StopBotAsync();

			// Start Bot
			await StartBotAsync();
		}


		public async Task ReloadCommandsAsync()
		{
			CoreState = YumeCoreState.Reloading;
			await CommandHandler.ReleaseCommandsAsync();
			await CommandHandler.RegisterCommandsAsync();
			CoreState = YumeCoreState.Online;
		}


		private string GetBotToken()
		{
			string token = CoreProperties.BotToken;

			if (string.IsNullOrWhiteSpace(token))
			{
				string envVarName = $"{CoreProperties.AppInternalName}.Token";

				if (TryBotTokenFromEnvironment(envVarName, out token, out EnvironmentVariableTarget target))
				{
					Logger.LogInformation($"Bot Token was read from {target} Environment Variable \"{envVarName}\", instead of \"coreproperties.json\" Config File.");
				}
				else
				{
					ApplicationException e = new("No Bot Token supplied.");
					Logger.LogCritical(e, $"No Bot Token was found in \"coreconfig.json\" Config File, and Environment Variables \"{envVarName}\" from relevant targets are empty. " +
											$"\nPlease set a Bot Token before launching the Bot.");
					throw e;
				}
			}

			return token;
		}

		private static bool TryBotTokenFromEnvironment(string envVarName, out string token, out EnvironmentVariableTarget foundFromTarget)
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				foreach (EnvironmentVariableTarget target in typeof(EnvironmentVariableTarget).GetEnumValues())
				{
					token = Environment.GetEnvironmentVariable(envVarName, target);

					if (token is not null)
					{
						foundFromTarget = target;
						return true;
					}
				}

				token = null;
				foundFromTarget = default;
				return false;
			}
			else
			{
				token = Environment.GetEnvironmentVariable(envVarName);

				foundFromTarget = EnvironmentVariableTarget.Process;
				return token is not null;
			}
		}

		private void ResolveCoreComponents()
		{
			Logger ??= Services.Resolve<ILogger<YumeCore>>();
			ConfigProvider ??= new();

			CoreProperties = ConfigProvider.InitConfig("coreconfig.json", true).PopulateCoreProperties();
			CoreProperties.Path_Core ??= Directory.GetCurrentDirectory();
			CoreProperties.Path_Plugins ??= Path.Combine(CoreProperties.Path_Core, "Plugins");
			CoreProperties.Path_Config ??= Path.Combine(CoreProperties.Path_Core, "Config");

			Client ??= Services.Resolve<DiscordClient>();
			CommandHandler ??= Services.Resolve<CommandHandler>();
			CommandHandler.Config ??= CoreProperties;
		}
	}
}
