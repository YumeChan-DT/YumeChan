using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using System.Reflection;
using System.Threading.Tasks;
using DryIoc;
using DryIoc.Microsoft.DependencyInjection;
using YumeChan.Core;
using YumeChan.PluginBase.Tools;

namespace YumeChan.ConsoleRunner;

public static class Program
{
	private static readonly LoggerConfiguration _serilogConfiguration = new LoggerConfiguration()
		.MinimumLevel.Debug()
		.MinimumLevel.Override("Microsoft", LogEventLevel.Information)
		.MinimumLevel.Override("DSharpPlus", LogEventLevel.Information)
		.Enrich.FromLogContext()
		.WriteTo.Console();

	private static readonly string _informationalVersion = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

	public static async Task Main(string[] args)
	{
		

		Log.Logger = _serilogConfiguration.CreateLogger();

		IHost host = CreateHostBuilder(args).Build();
        
		await using AsyncServiceScope scope = host.Services.CreateAsyncScope();
		IServiceProvider services = scope.ServiceProvider;

		Microsoft.Extensions.Logging.ILogger logger = services.GetRequiredService<Microsoft.Extensions.Logging.ILogger>();
		logger.LogInformation("Yume-Chan ConsoleRunner v{version}.", _informationalVersion);
		
		await Task.WhenAll(
			host.StartAsync(),
			YumeCore.Instance.StartBotAsync()
		);

		await host.WaitForShutdownAsync();
	}

	public static IHostBuilder CreateHostBuilder(string[] args) => new HostBuilder()
		.UseServiceProviderFactory(new DryIocServiceProviderFactory())
		.ConfigureLogging(static x => x.ClearProviders())
		.UseSerilog()
		.ConfigureContainer<Container>(static (_, container) =>
		{
			ServiceCollection services = new();
			
			services.AddSingleton(new ConsoleRunnerContext(RunnerType.Console, typeof(Program).Assembly.GetName().Name, _informationalVersion));
			services.AddYumeCoreServices();
			
			container.Populate(services);
		});
}