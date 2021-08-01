using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using System.Threading.Tasks;
using Unity;
using Unity.Microsoft.DependencyInjection;
using Unity.Microsoft.Logging;
using YumeChan.Core;
using YumeChan.NetRunner.Infrastructure.Blazor;

namespace YumeChan.NetRunner
{
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

			await YumeCore.Instance.StartBotAsync();
			await host.RunAsync();
		}
		public static IHostBuilder CreateHostBuilder(string[] args)
		{
			return Host.CreateDefaultBuilder(args)
				.UseUnityServiceProvider()
				.UseSerilog()
				.ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>())
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
