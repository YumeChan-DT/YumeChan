using System.Threading.Tasks;

using static Nodsoft.YumeChan.NetRunner.Services;


namespace Nodsoft.YumeChan.NetRunner.Controls
{
    public static class YumeCoreController
    {
		public static async Task StartBotButton()
		{
			if (!BotService.IsBotOnline)
			{
				await BotService.StartBotAsync();
			}
		}
    }
}