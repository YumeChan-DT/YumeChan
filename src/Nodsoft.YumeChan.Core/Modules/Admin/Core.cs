using Discord.Commands;
using System.Threading.Tasks;

using static Nodsoft.YumeChan.Core.YumeCore;

namespace Nodsoft.YumeChan.Core.Modules.Admin
{
	[Group("admin"), RequireOwner]
	public partial class Admin : ModuleBase<SocketCommandContext>
	{
		internal static async Task<bool> CoreIsOnline(SocketCommandContext context)
		{
			if (Instance.CoreState != YumeCoreState.Online)
			{
				await context.Channel.SendMessageAsync("YumeCore is not Online yet. Please wait, then retry (if necessary).");
				return false;
			}
			return true;
		}

		[Group("core")]
		public class Core : ModuleBase<SocketCommandContext>
		{
			[Command("restart", RunMode = RunMode.Async)]
			public async Task RestartCoreAsync()
			{
				if (await CoreIsOnline(Context))
				{
					using (Context.Channel.EnterTypingState())
					{
						await ReplyAsync("YumeCore restart in progress...");
						await Instance.RestartBotAsync();
					}
				}
			}

			[Command("stop", RunMode = RunMode.Async)]
			public async Task ShutdownCoreAsync()
			{
				if (await CoreIsOnline(Context))
				{
					await ReplyAsync("YumeCore now shutting down.");
					await Instance.StopBotAsync();
				}
			}
		}
	}
}