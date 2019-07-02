using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Design;

using static Nodsoft.YumeChan.Core.YumeCore;

namespace Nodsoft.YumeChan.Core.Modules.Status
{
	[Group("status")]
	public class Status : ModuleBase<SocketCommandContext>
	{
		public static string MissingVersionSubstitute { get; } = "Unknown";

		[Command]
		public async Task DefaultAsync()
		{
			EmbedBuilder embed = new EmbedBuilder()
				.WithTitle("Yume-Chan")
				.WithDescription($"Status : {Instance.CoreState.ToString()}")
				.AddField("Core", $"Version : {(CoreVersion != null ? CoreVersion.ToString() : MissingVersionSubstitute)}", true)
				.AddField("Loaded Modules", $"Count : {(Instance.Modules != null ? Instance.Modules.Count.ToString() : "None")}", true);
#if DEBUG
			embed.AddField("Debug", "Debug Build Active.");
#endif

			await ReplyAsync(embed: embed.Build());
		}
	}
}
