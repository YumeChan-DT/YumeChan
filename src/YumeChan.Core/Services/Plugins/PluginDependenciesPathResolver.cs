#nullable enable
using System.IO;
using NuGet.Packaging;
using NuGet.Packaging.Core;

namespace YumeChan.Core.Services.Plugins;

public class PluginDependenciesPathResolver : PackagePathResolver
{
	public PluginDependenciesPathResolver(string rootDirectory, string pluginName, bool useSideBySidePaths = true) 
		: base(Path.Combine(rootDirectory, pluginName), useSideBySidePaths) { }

	
	public override string GetPackageDirectoryName(PackageIdentity packageIdentity)
	{
		// Return a flat directory structure
		return Root;
	}
}