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
				.MinimumLevel.Override("DSharpPlus", LogEventLevel.Information)
				.Enrich.FromLogContext()
				.WriteTo.Console();

		public static async Task Main(string[] _)
		{
			Log.Logger = serilogConfiguration.CreateLogger();

			IHost host = CreateHostBuilder().Build();

			YumeCore.Instance.Services = container;

			Microsoft.Extensions.Logging.ILogger logger = container.Resolve<Microsoft.Extensions.Logging.ILogger>();
			logger.LogInformation("Yume-Chan ConsoleRunner v{version}.", typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion);

			await YumeCore.Instance.StartBotAsync().ConfigureAwait(false);
			await host.RunAsync();
		}

		public static IHostBuilder CreateHostBuilder(UnityContainer serviceRegistry = null)
		{
			return new HostBuilder()
				.UseUnityServiceProvider(serviceRegistry ?? new())
				.ConfigureLogging(x => x.ClearProviders())
				.UseSerilog()
				.ConfigureContainer<IUnityContainer>((context, container) =>
				{
					Program.container = container;  // This assignment is necessary, as configuration only affects the child container.

					container.AddExtension(new LoggingExtension(new SerilogLoggerFactory()));
					container.AddServices(new ServiceCollection().AddLogging(x => x.AddSerilog()));
					//container.RegisterType<ILoggerFactory, SerilogLoggerFactory>();

					YumeCore.Instance.ConfigureContainer(container);
				});
		}
	}
}
