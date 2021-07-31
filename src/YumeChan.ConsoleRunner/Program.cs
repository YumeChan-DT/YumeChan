using System.Threading.Tasks;
using Lamar;
using Lamar.Microsoft.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using YumeChan.Core;
using Serilog;
using Serilog.Events;
using Unity.Microsoft.DependencyInjection;
using Unity;
using Serilog.Extensions.Logging;

namespace YumeChan.ConsoleRunner
{
	public static class Program
	{
		private static IUnityContainer container = new UnityContainer();

		public static async Task Main(string[] _)
		{
			Log.Logger = new LoggerConfiguration()
				.MinimumLevel.Debug()
				.MinimumLevel.Override("Microsoft", LogEventLevel.Information)
				.Enrich.FromLogContext()
				.WriteTo.Console()
				.CreateLogger();

			IHost host = CreateHostBuilder().Build();

			YumeCore.Instance.Services = container;

			await YumeCore.Instance.StartBotAsync().ConfigureAwait(false);
			await host.RunAsync();
		}

		public static IHostBuilder CreateHostBuilder(UnityContainer serviceRegistry = null)
		{
			return new HostBuilder()
				.UseUnityServiceProvider(serviceRegistry ?? new())
				.UseSerilog()
				.ConfigureAppConfiguration(builder => { })
				.ConfigureContainer<IUnityContainer>((context, container) =>
				{
					Program.container = container;  // This assignment is necessary, as configuration only affects the child container.

					YumeCore.Instance.ConfigureContainer(container);
				});
		}
	}
}
