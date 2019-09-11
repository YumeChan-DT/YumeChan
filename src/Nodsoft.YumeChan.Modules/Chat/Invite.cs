using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;

using static Nodsoft.YumeChan.Modules.Chat.Utils;
using static Nodsoft.YumeChan.Modules.Chat.VideoBridge;

namespace Nodsoft.YumeChan.Modules.Chat
{
	public class Invite : ModuleBase<SocketCommandContext>
	{
		IVoiceChannel CurrentChannel { get; set; }

		[Command("invite"), Alias("inv")]
		public async Task InviteCommandAsync(SocketGuildUser user)
		{
			CurrentChannel = FindUserCurrentVoiceChannel(Context);

			if (user is null)
			{
				await ReplyAsync($"{Context.User.Mention} Please quote an existing User, or enter a valid username.");
			}
			else if (CurrentChannel is null)
			{
				await ReplyAsync($"{Context.User.Mention} Please connect to a Voice Channel before inviting another user.");
			}
			else
			{
				EmbedBuilder embed = new EmbedBuilder()
					.WithAuthor(Context.User)
					.WithTitle("Invitation to Voice Channel")
					.WithDescription($"You have been invited by {Context.User.Mention} to join a voice channel.")
					.AddField("Server", Context.Guild, true);

				if (CurrentChannel.CategoryId != null)
				{
					embed.AddField("Category", Context.Guild.GetCategoryChannel((ulong)CurrentChannel.CategoryId).Name, true); 
				}

				embed.AddField("Channel", CurrentChannel, true)
					.AddField("Invite Link", $"Use this link for quick access to ``{CurrentChannel.Name}`` :\n{BuildBridgeLink(LinkTypes.Https, CurrentChannel)}");

				await user.SendMessageAsync(embed: embed.Build());
				await Context.User.SendMessageAsync($"Sent {user.Mention} an invite to ``{CurrentChannel.Name}`` !");
			}
		}
	}
}
