using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Nodsoft.YumeChan.Modules.VideoBridge
{
	[Group("videobridge"), Alias ("vb")]
	public class VideoBridge : ModuleBase<SocketCommandContext>
	{
		[Command("")]
		public async Task BridgeCommandAsync(params string[] arguments)
		{
			IVoiceChannel channel = (Context.User as IGuildUser)?.VoiceChannel;

			if (!await ValidateVideoBridgeRequestAsync(Context, channel))
			{
				return;
			}

			// Flags settings
			bool includeManualLink = false;
			bool pingAllusersInChannel = false;

			if (!(arguments is null))
			{
				if (Array.Exists(arguments, arg => arg == "manual" ))
				{
					includeManualLink = true;
				}

				if (Array.Exists(arguments, arg => arg == "ping"))
				{
					SocketGuildUser user = Context.User as SocketGuildUser;
					if (user.GuildPermissions.MentionEveryone || user.GuildPermissions.PrioritySpeaker || user.GuildPermissions.ManageChannels)
					{
						pingAllusersInChannel = true;
					}
				}
			}

			string mentionList = Context.User.Mention;

			if (pingAllusersInChannel)
			{
				mentionList += ", ";

				var users = channel.GetUsersAsync().Flatten().GetEnumerator();

				while (await users.MoveNext())
				{
					mentionList += users.Current.Mention + " ";
				}
			}

			StringBuilder text = new StringBuilder(mentionList)
				.AppendLine($"\n**Videobridge link{(includeManualLink ? "s" : null)} for ``{channel.Name}`` :**")
				.AppendLine("\nUniversal HTTPS Link : (Browser & Client)")
				.AppendLine(BuildBridgeLink(LinkTypes.Https, channel));

			if (includeManualLink)
			{
				text.AppendLine("\n\nManual In-App Discord Link : (Copy to Browser)")
				.AppendLine($"```{BuildBridgeLink(LinkTypes.Discord, channel)}```");
			}

			text.AppendLine($"\nPlease use {(includeManualLink ? "these links" : "this link")} when switching back to Video Channel.");

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

		internal static async Task<bool> ValidateVideoBridgeRequestAsync(SocketCommandContext context, IVoiceChannel channel)
		{
			if (context.IsPrivate)
			{
				await context.Channel.SendMessageAsync("Please use ``==videobridge`` in a Server to bridge a Voice Channel.");
				return false;
			}

			if (channel is null)
			{
				await context.Channel.SendMessageAsync($"{context.User.Mention}, No Voice Channel presence detected. Please retry once connected to a Voice Channel.");
				return false;
			}

			return true;
		}

		internal enum LinkTypes 
		{ 
			Discord = 0,
			Https = 1
		}
	}
}
