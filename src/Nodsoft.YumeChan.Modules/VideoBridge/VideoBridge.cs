using Discord;
using Discord.Commands;
using System.Text;
using System.Threading.Tasks;

namespace Nodsoft.YumeChan.Modules.VideoBridge
{
	[Group("videobridge")]
	public class VideoBridge : ModuleBase<SocketCommandContext>
	{
		[Command, Alias("bridge", "interface")]
		public async Task BridgeCommandAsync()
		{
			IVoiceChannel channel = (Context.User as IGuildUser)?.VoiceChannel;

			if (Context.IsPrivate)
			{
				await ReplyAsync("Please use ``==videobridge`` in a Server to bridge a Voice Channel.");
				return;
			}

			if (channel is null)
			{
				await ReplyAsync($"{Context.User.Mention}, No Voice Channel presence detected. Please retry once connected to a Voice Channel.");
				return;
			}

			string discordLink = BuildBridgeLink(LinkTypes.Discord, channel);
			string httpsLink = BuildBridgeLink(LinkTypes.Https, channel);

			StringBuilder text = new StringBuilder(Context.User.Mention)
				.AppendLine("**Universal HTTPS Link:** (Browser and Client)")
				.AppendLine(httpsLink)
				.AppendLine("\n\n**Manual In-App Discord Link:** (Copy into Browser)")
				.AppendLine($"```{discordLink}```")
				.AppendLine("\nPlease use these links when switching back to Video Channel.");


			await ReplyAsync(text.ToString());

		}

		public static IVoiceChannel FindUserCurrentVoiceChannel(SocketCommandContext context) => (context.User as IGuildUser).VoiceChannel ?? null;

		public static ulong GetVoiceChannelId(IVoiceChannel channel) => channel.Id;
		public static ulong GetServerId(IVoiceChannel channel) => channel.Guild.Id;

		internal static string BuildBridgeLink(LinkTypes type, IVoiceChannel channel)
		{
			string typeStr = type switch
			{
				LinkTypes.Discord => "discord",
				LinkTypes.Https => "https",
				_ => throw new System.Exception()
			};

			return $"{typeStr}://discordapp.com/channels/{GetServerId(channel)}/{GetVoiceChannelId(channel)}";
		}

		internal enum LinkTypes 
		{ 
			Discord = 0,
			Https = 1
		}
	}
}
