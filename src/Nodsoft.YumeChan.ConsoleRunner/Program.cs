using System.Threading.Tasks;
using Lamar;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nodsoft.YumeChan.Core;

namespace Nodsoft.YumeChan.ConsoleRunner
{
	public static class Program
	{
		public static async Task Main(string[] _)
		{
			ServiceRegistry services = ConfigureServices(new());
			YumeCore.ConfigureServices(services);

			YumeCore.Instance.Services = new Container(services);

			await YumeCore.Instance.StartBotAsync().ConfigureAwait(true);
			await Task.Delay(-1);
		}

		public static ServiceRegistry ConfigureServices(ServiceRegistry services)
		{
			services.AddLogging()
				.AddSingleton(LoggerFactory.Create(builder =>
				{
				builder.ClearProviders()
#if DEBUG
						.SetMinimumLevel(LogLevel.Trace)
#endif
						.AddConsole()
						.AddFilter("Microsoft", LogLevel.Warning)
						.AddFilter("System", LogLevel.Warning)
						.AddDebug();
				}))
				.AddSingleton(YumeCore.Instance);

			return services;
		}
	}
}
