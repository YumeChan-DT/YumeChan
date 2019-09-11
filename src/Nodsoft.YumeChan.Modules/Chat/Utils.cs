using Discord;
using Discord.Commands;

namespace Nodsoft.YumeChan.Modules.Chat
{
	public static class Utils
	{
		public static IVoiceChannel FindUserCurrentVoiceChannel(SocketCommandContext context) => (context.User as IGuildUser)?.VoiceChannel;
	}
}
