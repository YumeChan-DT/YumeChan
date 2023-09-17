using NuGet.Packaging;
using NuGet.Packaging.Core;

namespace YumeChan.Core.Services.Plugins;

public sealed class PluginDependenciesPathResolver : PackagePathResolver
{
	public PluginDependenciesPathResolver(string rootDirectory, string pluginName, bool useSideBySidePaths = true) 
		: base(Path.Combine(rootDirectory, pluginName, "dl"), useSideBySidePaths) { }

	
	public override string GetPackageDirectoryName(PackageIdentity packageIdentity)
	{
		// Return a flat directory structure
		return Root;
	}
}