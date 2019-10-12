using Discord;
using Discord.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Nodsoft.YumeChan.Modules.Chat
{
	public static class Utils
	{
		public static IEmote GreenCheckEmote = new Emoji("\u2705");
		public static IEmote GreenCrossEmote = new Emoji("\u274e");

		public static IVoiceChannel FindUserCurrentVoiceChannel(SocketCommandContext context) => (context.User as IGuildUser)?.VoiceChannel;

		public static IEmote FindEmote(SocketCommandContext context, string emoteName)
		{
			return context.Guild.Emotes.FirstOrDefault(x => x.Name.IndexOf(emoteName, StringComparison.OrdinalIgnoreCase) != -1);
		}

		public static async Task MarkCommandAsCompleted(SocketCommandContext context) => await context.Message.AddReactionAsync(GreenCheckEmote);
		public static async Task MarkCommandAsFailed(SocketCommandContext context) => await context.Message.AddReactionAsync(GreenCrossEmote);
	}
}
