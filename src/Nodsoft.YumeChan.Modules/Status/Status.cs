using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using static Nodsoft.YumeChan.Modules.ModulesIndex;

namespace Nodsoft.YumeChan.Modules.Status
{
	[Group("status")]
	public class Status : ModuleBase<SocketCommandContext>
	{
		[Command]
		public async Task DefaultAsync()
		{
			EmbedBuilder embed = new EmbedBuilder()
				.WithTitle("Yume-Chan")
				.WithDescription("Status : Online")
				.AddField("Core", $"Version : {(CoreVersion != null ? CoreVersion.ToString() : MissingVersionSubstitute)}", true)
				.AddField("Modules", $"Version : {ModulesVersion}", true);
#if DEBUG
				embed.AddField("Debug", "Debug Build Active.");			
#endif

			await ReplyAsync(embed: embed.Build());
		}
	}
}
