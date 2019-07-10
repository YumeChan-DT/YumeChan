using System;
using System.Threading.Tasks;

using Nodsoft.YumeChan.Core;
using Nodsoft.YumeChan.NetRunner.Controls.Assets;
using static Nodsoft.YumeChan.NetRunner.Services;


namespace Nodsoft.YumeChan.NetRunner.Controls
{
	public static class YumeCoreController
	{
		public static Task<string[]> DisplayStatusAlert()
		{
			switch (BotService.CoreState)
			{
				case YumeCoreState.Offline:
					return Task.FromResult(new string[] { Alerts.danger, "Bot is offline." });
				case YumeCoreState.Online:
					return Task.FromResult(new string[] { Alerts.success, "Bot is online." });
				case YumeCoreState.Starting:
					return Task.FromResult(new string[] { Alerts.info, "Bot is starting..." });
				case YumeCoreState.Stopping:
					return Task.FromResult(new string[] { Alerts.warning, "Bot is Stopping..." });
				case YumeCoreState.Reloading:
					return Task.FromResult(new string[] { Alerts.warning, "Bot is Reloading..." });
				default:
					return Task.FromResult(new string[] { Alerts.danger, "Bot Status is Unknown." });
			}
		}

		public static async Task StartBotButton()
		{
			if (BotService.CoreState == YumeCoreState.Offline)
			{
				await BotService.StartBotAsync();
			}
		}

		public static async Task StopBotButton()
		{
			if (BotService.CoreState != YumeCoreState.Offline)
			{
				await BotService.StopBotAsync();
			}
		}

		public static async Task RestartBotButton()
		{
			if (BotService.CoreState == YumeCoreState.Online)
			{
				await BotService.RestartBotAsync();
			}
		}

		public static async Task ReloadModulesButton()
		{
			if (BotService.CoreState == YumeCoreState.Online)
			{
				await BotService.ReloadCommandsAsync();
			}
		}
	}
}