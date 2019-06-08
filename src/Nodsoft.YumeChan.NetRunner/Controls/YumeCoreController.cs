using System.Threading.Tasks;

using Nodsoft.YumeChan.Core;
using Nodsoft.YumeChan.NetRunner.Controls.Assets;
using static Nodsoft.YumeChan.NetRunner.Services;


namespace Nodsoft.YumeChan.NetRunner.Controls
{
    public static class YumeCoreController
    {
		public static async Task<string[]> DisplayStatusAlert()
		{
			switch (BotService.CoreState)
			{
				case YumeCoreState.Offline:
					return new string[] { Alerts.danger , "Bot is offline." };
				case YumeCoreState.Online:
					return new string[] { Alerts.success, "Bot is online." };
				case YumeCoreState.Starting:
					return new string[] { Alerts.info, "Bot is starting..." };
				case YumeCoreState.Stopping:
					return new string[] { Alerts.warning, "Bot is Stopping..." };
				case YumeCoreState.Reloading:
					return new string[] { Alerts.warning, "Bot is Reloading..." };
				default:
					return new string[] { Alerts.danger, "Bot Status is Unknown." };
			}
		}

		public static async Task StartBotButton()
		{
			if (BotService.CoreState == YumeCoreState.Offline)
			{
				await BotService.StartBotAsync();
			}
		}
    }
}