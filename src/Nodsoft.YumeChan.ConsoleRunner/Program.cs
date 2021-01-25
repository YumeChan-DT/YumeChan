using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nodsoft.YumeChan.Core;

namespace Nodsoft.YumeChan.ConsoleRunner
{
	public static class Program
	{
		public static async Task Main(string[] _)
		{
			IServiceCollection services = await ConfigureServices(new ServiceCollection());
			YumeCore.ConfigureServices(services);

			YumeCore.Instance.Services = services.BuildServiceProvider();

			await YumeCore.Instance.StartBotAsync().ConfigureAwait(true);
			await Task.Delay(-1);
		}

		public static Task<IServiceCollection> ConfigureServices(IServiceCollection services)
		{
			services.AddLogging();
			services.AddSingleton(LoggerFactory.Create(builder =>
			{
				builder.ClearProviders()
#if DEBUG
						.SetMinimumLevel(LogLevel.Trace)
#endif
						.AddConsole()
						.AddFilter("Microsoft", LogLevel.Warning)
						.AddFilter("System", LogLevel.Warning)
						.AddDebug();
			}));

			services.AddSingleton(YumeCore.Instance);

			return Task.FromResult(services);
		}
	}
}
