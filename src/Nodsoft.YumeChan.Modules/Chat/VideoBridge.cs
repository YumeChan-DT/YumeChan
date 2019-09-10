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
			bool includeManualLink;
			bool pingAllusersInChannel;
			bool sendtoPrivateMessage;

			await ParseVideoBridgeArguments(Context, arguments, out includeManualLink, out pingAllusersInChannel, out sendtoPrivateMessage);


			StringBuilder mentionList = new StringBuilder(Context.User.Mention);

			if (pingAllusersInChannel)
			{
				mentionList.Append(", ");

				var users = channel.GetUsersAsync().Flatten().GetEnumerator();

				while (await users.MoveNext())
				{
					mentionList.Append(users.Current.Mention).Append(" ");
				}
				mentionList.Append("\n");
			}

			StringBuilder text = new StringBuilder(sendtoPrivateMessage ? string.Empty : mentionList.ToString())
				.AppendLine($"**Videobridge link{(includeManualLink ? "s" : null)} for ``{channel.Name}`` :**")
				.AppendLine("\nUniversal HTTPS Link : (Browser & Client)")
				.AppendLine(BuildBridgeLink(LinkTypes.Https, channel));

			if (includeManualLink)
			{
				text.AppendLine("\n\nManual In-App Discord Link : (Copy to Browser)")
				.AppendLine($"```{BuildBridgeLink(LinkTypes.Discord, channel)}```");
			}

			text.AppendLine($"\nPlease use {(includeManualLink ? "these links" : "this link")} when switching back to Video Channel.");

			if (sendtoPrivateMessage)
			{
				await Context.User.SendMessageAsync(text.ToString());
			}
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

		internal static Task ParseVideoBridgeArguments(SocketCommandContext context, string[] args, out bool manual, out bool ping, out bool direct)
		{
			manual = false;
			ping = false;
			direct = false;
			if (!(args is null))
			{
				if (Array.Exists(args, arg => arg == "manual"))
				{
					manual = true;
				}

				if (Array.Exists(args, arg => arg == "ping"))
				{
					SocketGuildUser user = context.User as SocketGuildUser;
					if (user.GuildPermissions.MentionEveryone || user.GuildPermissions.PrioritySpeaker || user.GuildPermissions.ManageChannels)
					{
						ping = true;
					}
				}

				if (Array.Exists(args, arg => arg == "dm"))
				{
					direct = true;
					ping = false;
				}
			}

			return Task.CompletedTask;
		}

		internal enum LinkTypes 
		{ 
			Discord = 0,
			Https = 1
		}
	}
}
