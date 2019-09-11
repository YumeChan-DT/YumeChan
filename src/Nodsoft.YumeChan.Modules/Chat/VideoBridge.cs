using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using static Nodsoft.YumeChan.Modules.Chat.Utils;

namespace Nodsoft.YumeChan.Modules.Chat
{
	[Group("videobridge"), Alias ("vb")]
	public class VideoBridge : ModuleBase<SocketCommandContext>
	{
		IVoiceChannel CurrentChannel { get; set; }

		// Flags settings
		bool IncludeManualLink { get; set; }
		bool PingAllusersInChannel { get; set; }
		bool SendtoPrivateMessage { get; set; }

		[Command("")]
		public async Task BridgeCommandAsync(params string[] arguments)
		{
			CurrentChannel = FindUserCurrentVoiceChannel(Context);

			if (!await ValidateVideoBridgeRequestAsync())
			{
				return;
			}

			ParseVideoBridgeArguments(arguments);



			StringBuilder mentionList = await BuildUsersMentionList();

			StringBuilder text = new StringBuilder(SendtoPrivateMessage ? string.Empty : mentionList.ToString())
				.AppendLine($"**Videobridge link{(IncludeManualLink ? "s" : null)} for ``{CurrentChannel.Name}`` :**")
				.AppendLine("\nUniversal HTTPS Link : (Browser & Client)")
				.AppendLine(BuildBridgeLink(LinkTypes.Https, CurrentChannel));

			if (IncludeManualLink)
			{
				text.AppendLine("\n\nManual In-App Discord Link : (Copy to Browser)")
				.AppendLine($"```{BuildBridgeLink(LinkTypes.Discord, CurrentChannel)}```");
			}

			text.AppendLine($"\nPlease use {(IncludeManualLink ? "these links" : "this link")} when switching back to Video Channel.");


			if (SendtoPrivateMessage)
			{
				await Context.User.SendMessageAsync(text.ToString());
			}
			else
			{
				await ReplyAsync(text.ToString());
			}
		}

		internal static string BuildBridgeLink(LinkTypes type, IVoiceChannel channel)
		{
			string typeStr = type switch
			{
				LinkTypes.Discord => "discord",
				LinkTypes.Https => "https",
				_ => throw new Exception()
			};

			return $"{typeStr}://discordapp.com/channels/{channel.GuildId}/{channel.Id}";
		}

		private async Task<bool> ValidateVideoBridgeRequestAsync()
		{
			if (Context.IsPrivate)
			{
				await Context.Channel.SendMessageAsync("Please use ``==videobridge`` in a Server to bridge a Voice Channel.");
				return false;
			}

			if (CurrentChannel is null)
			{
				await Context.Channel.SendMessageAsync($"{Context.User.Mention}, No Voice Channel presence detected. Please retry once connected to a Voice Channel.");
				return false;
			}

			return true;
		}

		private void ParseVideoBridgeArguments(string[] args)
		{
			if (!(args is null))
			{
				if (Array.Exists(args, arg => arg == "manual"))
				{
					IncludeManualLink = true;
				}

				if (Array.Exists(args, arg => arg == "ping"))
				{
					SocketGuildUser user = Context.User as SocketGuildUser;
					if (user.GuildPermissions.MentionEveryone || user.GuildPermissions.PrioritySpeaker || user.GuildPermissions.ManageChannels)
					{
						PingAllusersInChannel = true;
					}
				}

				if (Array.Exists(args, arg => arg == "dm"))
				{
					SendtoPrivateMessage = true;
					PingAllusersInChannel = false;
				}
			}
		}

		internal async Task<StringBuilder> BuildUsersMentionList()
		{
			var users = FindUserCurrentVoiceChannel(Context).GetUsersAsync().Flatten().GetEnumerator(); // I HATE var, but no choice here, as apparently IAsyncEnumerator is duped on 2 Libraries...
			StringBuilder mentionList = new StringBuilder(Context.User.Mention);

			if (PingAllusersInChannel)
			{
				mentionList.Append(", ");
				while (await users.MoveNext())
				{
					if (users.Current.Id != Context.User.Id)
					{
						mentionList.Append(users.Current.Mention).Append(" ");
					}
				} 
			}
			mentionList.Append("\n");

			return mentionList;
		}

		internal enum LinkTypes 
		{ 
			Discord = 0,
			Https = 1
		}
	}
}
