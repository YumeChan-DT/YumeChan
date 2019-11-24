using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Nodsoft.YumeChan.Core;

using static Nodsoft.YumeChan.NetRunner.Properties.AppProperties;

namespace Nodsoft.YumeChan.NetRunner
{
	public static class Program
	{
		public static async Task Main(string[] args)
		{
			IHost host = CreateHostBuilder(args).Build();

			YumeCore.Instance.Services = host.Services;

			await YumeCore.Instance.StartBotAsync();
			await host.RunAsync();
		}
		public static IHostBuilder CreateHostBuilder(string[] args)
		{
			return Host	.CreateDefaultBuilder(args)
						.ConfigureLogging(builder =>
						{
							builder.ClearProviders()
									.AddConsole()
									.AddFilter("Microsoft", LogLevel.Warning)
									.AddFilter("System", LogLevel.Warning)
									.AddDebug()
									.AddEventLog(settings => 
									{
										settings.SourceName = AppName;
										settings.LogName = AppName;
									});
						})
						.ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());
		}
	}
}
