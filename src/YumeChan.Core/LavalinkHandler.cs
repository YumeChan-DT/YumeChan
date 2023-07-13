using DSharpPlus;
using DSharpPlus.Lavalink;
using DSharpPlus.Net;
using Microsoft.Extensions.Logging;
using YumeChan.Core.Config;

#nullable enable
namespace YumeChan.Core;

/// <summary>
/// Represents the Lavalink handler.
/// </summary>
public sealed class LavalinkHandler
{
	public LavalinkExtension Lavalink { get; internal set; } = null!;
	public LavalinkConfiguration LavalinkConfiguration { get; internal set; } = null!;

	internal ICoreLavalinkProperties Config { get; set; } = null!;

	private readonly DiscordClient _client;
	private readonly ILogger<LavalinkHandler> _logger;

	public LavalinkHandler(DiscordClient client, ILogger<LavalinkHandler> logger)
	{
		_client = client;
		_logger = logger;
	}

	public async ValueTask InitializeAsync()
	{
		Lavalink = _client.UseLavalink();
		_logger.LogInformation("Initialized Lavalink Extension");

		ConnectionEndpoint lavalinkEndpoint = new()
		{
			Hostname = Config.Hostname,
			Port = Config.Port ?? 2333
		};

		LavalinkConfiguration = new()
		{
			Password = Config.Password,
			RestEndpoint = lavalinkEndpoint,
			SocketEndpoint = lavalinkEndpoint
		};

		LavalinkNodeConnection connection = await Lavalink.ConnectAsync(LavalinkConfiguration);
		_logger.LogInformation("Established Lavalink Connection (Host: {hostname}:{port})", connection.NodeEndpoint.Hostname, connection.NodeEndpoint.Port);
	}
}