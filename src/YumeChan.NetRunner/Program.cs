using DryIoc;
using DryIoc.Microsoft.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace YumeChan.NetRunner;

public static class Program
{
	public static async Task Main(string[] args)
	{
		Log.Logger = new LoggerConfiguration()
			.MinimumLevel.Override("Microsoft", LogEventLevel.Information)
			.Enrich.FromLogContext()
			.WriteTo.Console(applyThemeToRedirectedOutput: true)
			.CreateBootstrapLogger();
        
		WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args });
		
		builder.Host.ConfigureHost();
		
		builder.ConfigureServices();
		await using WebApplication host = builder.Build();

		host.UseApplicationPipeline();
		
		YumeCore yumeCore = host.Services.GetRequiredService<YumeCore>();
		
		await Task.WhenAll(
			yumeCore.StartBotAsync(),
			host.RunAsync()
		);
	}
	
	private static void ConfigureHost(this ConfigureHostBuilder builder) => builder
		.UseServiceProviderFactory(new DryIocServiceProviderFactory())
		.ConfigureLogging(x => x.ClearProviders())
		.ConfigureContainer<Container>((_, container) => container
			.WithDependencyInjectionAdapter()
		);
}
