using Discord.Commands;
using System.Threading.Tasks;

using static Nodsoft.YumeChan.Core.YumeCore;

// FIXME : Plugins Class not loaded in Modules (Admin Partial Class interference ?)

namespace Nodsoft.YumeChan.Core.Modules.Admin
{
	public partial class Admin : ModuleBase<SocketCommandContext>
	{
		[Group("plugins")]
		public class Plugins : ModuleBase<SocketCommandContext>
		{
			[Command("reload")]
			public async Task ReloadPluginsAsync()
			{
				if (!await CoreIsOnline(Context))
				{
					using (Context.Channel.EnterTypingState())
					{
						await ReplyAsync("Reloading Plugins...");
						await Instance.ReloadCommandsAsync();
						await ReplyAsync("Done !"); 
					}
				}
			}
		}
	}
}
