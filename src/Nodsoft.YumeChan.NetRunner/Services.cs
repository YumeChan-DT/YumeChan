using Microsoft.Extensions.DependencyInjection;
using Nodsoft.YumeChan.Core;
using System;

namespace Nodsoft.YumeChan.NetRunner
{
	internal static class Services
	{
		internal static IServiceCollection AppServiceCollection { get; set; }
		internal static IServiceProvider AppServiceProvider { get; set; }

		internal static YumeCore BotService { get; set; } = AppServiceProvider.GetRequiredService<YumeCore>();
	}
}