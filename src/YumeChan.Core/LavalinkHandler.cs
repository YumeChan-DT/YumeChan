using Castle.Core.Logging;
using DSharpPlus;
using DSharpPlus.Lavalink;
using DSharpPlus.Net;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YumeChan.Core.Config;

namespace YumeChan.Core
{
	public class LavalinkHandler
	{
		internal ICoreLavalinkProperties Config { private get; set; }

		private readonly DiscordClient client;
		private readonly ILogger<LavalinkHandler> logger;

		public LavalinkHandler(DiscordClient client, ILogger<LavalinkHandler> logger)
		{
			this.client = client;
			this.logger = logger;
		}

		public async Task Initialize()
		{
			ConnectionEndpoint lavalinkEndpoint = new()
			{
				Hostname = Config.Hostname,
				Port = Config.Port
			};

			LavalinkConfiguration lavalinkConfiguration = new()
			{
				Password = Config.Password,
				RestEndpoint = lavalinkEndpoint,
				SocketEndpoint = lavalinkEndpoint
			};

			LavalinkExtension lavalink = client.UseLavalink();
			await lavalink.ConnectAsync(lavalinkConfiguration);
		}
	}
}
