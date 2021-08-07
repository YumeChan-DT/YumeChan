using DSharpPlus;
using DSharpPlus.Lavalink;
using DSharpPlus.Net;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using YumeChan.Core.Config;

namespace YumeChan.Core
{
	public class LavalinkHandler
	{
		internal ICoreLavalinkProperties Config { get; set; }

		private readonly DiscordClient client;
		private readonly ILogger<LavalinkHandler> logger;

		public LavalinkHandler(DiscordClient client, ILogger<LavalinkHandler> logger)
		{
			this.client = client;
			this.logger = logger;
		}

		public async Task Initialize()
		{
			LavalinkExtension lavalink = client.UseLavalink();
			logger.LogInformation("Initialized Lavalink Extension.");

			ConnectionEndpoint lavalinkEndpoint = new()
			{
				Hostname = Config.Hostname,
				Port = Config.Port ?? 2333
			};

			LavalinkConfiguration lavalinkConfiguration = new()
			{
				Password = Config.Password,
				RestEndpoint = lavalinkEndpoint,
				SocketEndpoint = lavalinkEndpoint
			};

			LavalinkNodeConnection connection = await lavalink.ConnectAsync(lavalinkConfiguration);
			logger.LogInformation("Established Lavalink Connection (Host: {hostname}:{port})", connection.NodeEndpoint.Hostname, connection.NodeEndpoint.Port);
		}
	}
}
