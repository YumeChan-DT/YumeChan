using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using System.Reflection;
using System.Threading.Tasks;
using Unity;
using Unity.Microsoft.DependencyInjection;
using Unity.Microsoft.Logging;
using YumeChan.Core;
using YumeChan.PluginBase.Tools;

namespace YumeChan.ConsoleRunner;

public static class Program
{
	private static IUnityContainer _container = new UnityContainer();

	private static readonly LoggerConfiguration SerilogConfiguration = new LoggerConfiguration()
		.MinimumLevel.Debug()
		.MinimumLevel.Override("Microsoft", LogEventLevel.Information)
		.MinimumLevel.Override("DSharpPlus", LogEventLevel.Information)
		.Enrich.FromLogContext()
		.WriteTo.Console();

	public static async Task Main(string[] _)
	{
		string informationalVersion = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

		Log.Logger = SerilogConfiguration.CreateLogger();

		IHost host = CreateHostBuilder().Build();

		YumeCore.Instance.Services = _container;
		_container.RegisterInstance(new ConsoleRunnerContext(RunnerType.Console, typeof(Program).Assembly.GetName().Name, informationalVersion));

		Microsoft.Extensions.Logging.ILogger logger = _container.Resolve<Microsoft.Extensions.Logging.ILogger>();
		logger.LogInformation("Yume-Chan ConsoleRunner v{Version}.", informationalVersion);

		await YumeCore.Instance.StartBotAsync().ConfigureAwait(false);
		await host.RunAsync();
	}

	public static IHostBuilder CreateHostBuilder(UnityContainer serviceRegistry = null) => new HostBuilder()
		.UseUnityServiceProvider(serviceRegistry ?? new())
		.ConfigureLogging(x => x.ClearProviders())
		.UseSerilog()
		.ConfigureContainer<IUnityContainer>((_, container) =>
			{
				_container = container; // This assignment is necessary, as configuration only affects the child container.

				container.AddExtension(new LoggingExtension(new SerilogLoggerFactory()));
				container.AddServices(new ServiceCollection().AddLogging(x => x.AddSerilog()));

				YumeCore.Instance.ConfigureContainer(container);
			}
		);
}