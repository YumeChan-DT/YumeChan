using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using YumeChan.Core;
using Lamar.Microsoft.DependencyInjection;
using Lamar;
using Serilog;
using Serilog.Events;
using Unity;
using Unity.Microsoft.DependencyInjection;
using Serilog.Extensions.Logging;

namespace YumeChan.NetRunner
{
	public static class Program
	{
		private static IUnityContainer container = new UnityContainer();

		public static async Task Main(string[] args)
		{
			Log.Logger = new LoggerConfiguration()
				.MinimumLevel.Debug()
				.MinimumLevel.Override("Microsoft", LogEventLevel.Information)
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
				.ConfigureLogging(builder =>
				{

				})
				.ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>())
				.ConfigureContainer<IUnityContainer>((context, container) =>
				{
					Program.container = container;  // This assignment is necessary, as configuration only affects 

					YumeCore.Instance.ConfigureContainer(container);
				})
				.UseSerilog();
		}
	}
}
