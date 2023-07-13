using Microsoft.Extensions.DependencyInjection;
using NuGet.Configuration;
using NuGet.Protocol.Core.Types;
using YumeChan.Core.Config;

namespace YumeChan.Core.Services.Plugins;

public static class NugetPluginsLoaderExtensions
{
	public static IServiceCollection AddNuGetPluginsFetcher(this IServiceCollection services)
	{
		services.AddSingleton<PackageSourceProvider>(s =>
			{
				IPluginLoaderProperties configuration = s.GetRequiredService<IPluginLoaderProperties>();
				return new(NullSettings.Instance, configuration.Nuget.PackageSources.Select(x => new PackageSource(x)));
			}
		);

		services.AddSingleton<SourceRepositoryProvider>(s => new(s.GetRequiredService<PackageSourceProvider>(), Repository.Provider.GetCoreV3()));
		
		services.AddSingleton<NugetPluginsFetcher>();
		return services;
	}
}