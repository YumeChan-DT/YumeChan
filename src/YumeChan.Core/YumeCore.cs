using DSharpPlus;
using Microsoft.Extensions.Logging;
using System.Reflection;
using DryIoc;
using YumeChan.Core.Config;
using YumeChan.Core.Services.Config;

#nullable enable
namespace YumeChan.Core;

public enum YumeCoreState
{
	Offline = 0, Online = 1, Starting = 2, Stopping = 3, Reloading = 4
}

public sealed class YumeCore
{
	public static YumeCore Instance => _instance!;
	private static YumeCore? _instance;

	public YumeCoreState CoreState { get; private set; }

	public static string CoreVersion { get; } = typeof(YumeCore).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "Unknown"; 

	public DiscordClient Client { get; set; }
	public CommandHandler CommandHandler { get; set; }
	public LavalinkHandler LavalinkHandler { get; set; }
	public IContainer Services { get; set; }

	internal ILogger<YumeCore> Logger { get; set; }

	internal InterfaceConfigProvider<ICoreProperties> ConfigProvider { get; private set; }
	public ICoreProperties CoreProperties { get; private set; }


	public YumeCore(IContainer services)
	{
		Services = services;
		
		Logger = Services.Resolve<ILogger<YumeCore>>();
		ConfigProvider = Services.Resolve<InterfaceConfigProvider<ICoreProperties>>();

		CoreProperties = ConfigProvider.InitConfig("coreconfig.json", true).InitDefaults();
		CoreProperties.Path_Core ??= Directory.GetCurrentDirectory();
		CoreProperties.Path_Plugins ??= Path.Combine(CoreProperties.Path_Core, "Plugins");
		CoreProperties.Path_Config ??= Path.Combine(CoreProperties.Path_Core, "Config");

		Client = Services.Resolve<DiscordClient>();
		CommandHandler = Services.Resolve<CommandHandler>();
		CommandHandler.Config = CoreProperties;

		LavalinkHandler = Services.Resolve<LavalinkHandler>();
		LavalinkHandler.Config = CoreProperties.LavalinkProperties;
		
		_instance = this;
	}

	~YumeCore()
	{
		StopBotAsync().Wait();
	}

	public async Task StartBotAsync()
	{
		if (Services is null)
		{
			throw new InvalidOperationException("Service Provider has not been defined.", new ArgumentNullException(nameof(Services)));
		}

		Logger.LogInformation("YumeCore v{version}", CoreVersion);

		CoreState = YumeCoreState.Starting;

		await CommandHandler.InstallCommandsAsync();
		await Client.ConnectAsync();
		await Client.InitializeAsync();
		await LavalinkHandler.InitializeAsync();

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
}