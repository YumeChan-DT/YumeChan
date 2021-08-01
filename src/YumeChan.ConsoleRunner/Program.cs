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

namespace YumeChan.ConsoleRunner
{
	public static class Program
	{
		private static IUnityContainer container = new UnityContainer();

		private static readonly LoggerConfiguration serilogConfiguration = new LoggerConfiguration()
				.MinimumLevel.Debug()
				.MinimumLevel.Override("Microsoft", LogEventLevel.Information)
				.Enrich.FromLogContext()
				.WriteTo.Console();

		public static async Task Main(string[] _)
		{
			Log.Logger = serilogConfiguration.CreateLogger();

			IHost host = CreateHostBuilder().Build();

			YumeCore.Instance.Services = container;

			ILogger<YumeCore> logger = container.Resolve<ILogger<YumeCore>>();
			logger.LogInformation("Yume-Chan ConsoleRunner v{version}.", typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion);

			await YumeCore.Instance.StartBotAsync().ConfigureAwait(false);
			await host.RunAsync();
		}

		public static IHostBuilder CreateHostBuilder(UnityContainer serviceRegistry = null)
		{
			return new HostBuilder()
				.UseUnityServiceProvider(serviceRegistry ?? new())
				.UseSerilog()
				.ConfigureContainer<IUnityContainer>((context, container) =>
				{
					Program.container = container;  // This assignment is necessary, as configuration only affects the child container.

					container.AddExtension(new LoggingExtension());
					container.AddServices(new ServiceCollection().AddLogging(x => x.AddSerilog()));

					YumeCore.Instance.ConfigureContainer(container);
				});
		}
	}
}
