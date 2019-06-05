using System;


namespace Nodsoft.YumeChan.NetRunner
{
	internal static class Services
	{
		internal static IServiceProvider AppServiceProvider { get; set; }
		internal static YumeCoreSingleton BotService { get; set; }
	}
}
