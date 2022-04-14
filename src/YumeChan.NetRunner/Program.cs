using System.Threading;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Unity;
using Unity.Microsoft.DependencyInjection;
using Unity.Microsoft.Logging;
using YumeChan.NetRunner.Plugins.Services;

namespace YumeChan.NetRunner;

public static class Program
{
	private static IUnityContainer container = new UnityContainer();

	public static async Task Main(string[] args)
	{
		Log.Logger = new LoggerConfiguration()
			.MinimumLevel.Debug()
			.MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
			.MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
			.Enrich.FromLogContext()
			.WriteTo.Console()
			.CreateLogger();


		IHost host = CreateHostBuilder(args).Build();

		YumeCore.Instance.Services = container;

		await YumeCore.Instance.StartBotAsync().ConfigureAwait(false);
		await host.RunAsync();
	}
	public static IHostBuilder CreateHostBuilder(string[] args)
	{
		return Host.CreateDefaultBuilder(args)
			.UseUnityServiceProvider()
			.ConfigureLogging(x => x.ClearProviders())
			.ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>())
			.ConfigureContainer<IUnityContainer>((context, container) =>
			{
				Program.container = container;  // This assignment is necessary, as configuration only affects the child container.

				container.AddExtension(new LoggingExtension(new SerilogLoggerFactory()));
				container.AddServices(new ServiceCollection().AddLogging(x => x.AddSerilog()));

				YumeCore.Instance.ConfigureContainer(container);
			});
	}
}
