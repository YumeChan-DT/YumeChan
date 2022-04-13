using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using YumeChan.NetRunner.Plugins.Infrastructure.Api;
using YumeChan.NetRunner.Plugins.Services;

namespace YumeChan.NetRunner.Plugins.Infrastructure;

public static class ApiPluginDependencyExtensions
{
	public static IServiceCollection AddApiPluginSupport(this IServiceCollection services)
	{
		services.AddSingleton<IActionDescriptorChangeProvider>(PluginActionDescriptorChangeProvider.Instance);
		services.AddSingleton(PluginActionDescriptorChangeProvider.Instance);

		services.AddHostedService<ApiPluginLoader>();

		return services;
	} 
	
	public static void ConfigurePluginNameRoutingToken(this MvcOptions options, string tokenName = "plugin") 
		=> options.Conventions.Add(new CustomRouteToken(tokenName, c => c.ControllerType.Namespace));
}