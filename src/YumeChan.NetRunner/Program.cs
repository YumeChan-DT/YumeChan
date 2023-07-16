using DryIoc;
using DryIoc.Microsoft.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace YumeChan.NetRunner;

public static class Program
{
	private static Container _container = new();

	public static async Task Main(string[] args)
	{
		
		
		Log.Logger = new LoggerConfiguration()
			.MinimumLevel.Debug()
			.MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
			.MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
			.Enrich.FromLogContext()
			.WriteTo.Console()
			.CreateLogger();
        
		using IHost host = CreateHostBuilder(args).Build();
		IServiceProvider services = host.Services;

		YumeCore yumeCore = services.GetRequiredService<YumeCore>();
		
		await Task.WhenAll(
			yumeCore.StartBotAsync(),
			host.RunAsync()
		);

		await host.WaitForShutdownAsync();
	}
	public static IHostBuilder CreateHostBuilder(string[] args) => Host.CreateDefaultBuilder(args)
		.UseServiceProviderFactory(new DryIocServiceProviderFactory())
		.ConfigureLogging(x => x.ClearProviders())
		.ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>())
//		.ConfigureContainer<Container>((_, container) => container
//			.WithDependencyInjectionAdapter()
//		)
	;
}
