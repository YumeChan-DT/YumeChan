using System.Threading.Tasks;

using Nodsoft.YumeChan.Core;
using static Nodsoft.YumeChan.NetRunner.Services;


namespace Nodsoft.YumeChan.NetRunner.Controls
{
    public static class YumeCoreController
    {
		public static async Task StartBotButton()
		{
			if (BotService.CoreState == YumeCoreState.Offline)
			{
				await BotService.StartBotAsync();
			}
		}
    }
}