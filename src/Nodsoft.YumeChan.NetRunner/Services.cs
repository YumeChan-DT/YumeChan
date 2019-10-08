using Microsoft.Extensions.DependencyInjection;
using Nodsoft.YumeChan.Core;
using System;


namespace Nodsoft.YumeChan.NetRunner
{
	internal static class Services
	{
		internal static IServiceCollection AppServiceCollection { get; set; }
		internal static Logger LoggerService { get; set; } = new Logger();
		internal static YumeCore BotService { get; set; } = YumeCore.Instance;
	}
}