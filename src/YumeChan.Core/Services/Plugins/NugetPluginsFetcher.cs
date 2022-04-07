#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Logging;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Packaging.Signing;
using NuGet.Protocol.Core.Types;
using NuGet.Resolver;
using NuGet.Versioning;
using YumeChan.Core.Config;
using static YumeChan.Core.Services.Plugins.NugetUtilities;

namespace YumeChan.Core.Services.Plugins;

/// <summary>
/// Provides a plugins loader for NuGet package-based plugins.
/// </summary>
public class NugetPluginsFetcher : IDisposable
{
	private readonly SourceRepositoryProvider _sourceRepositoryProvider;
	private readonly SourceCacheContext _sourceCacheContext;
	private readonly ILogger<NugetPluginsFetcher> _logger;
	private readonly ICoreProperties _coreProperties;
	private readonly IPluginLoaderProperties _pluginProperties;
	private readonly NuGetFramework _nugetFramework; 

	public NugetPluginsFetcher(SourceRepositoryProvider sourceRepositoryProvider, ILogger<NugetPluginsFetcher> logger, 
		ICoreProperties coreProperties, IPluginLoaderProperties properties)
	{
		_sourceRepositoryProvider = sourceRepositoryProvider;
		_logger = logger;
		_coreProperties = coreProperties;
		_pluginProperties = properties;
		_sourceCacheContext = new();
		_nugetFramework = NuGetFramework.ParseFrameworkName(typeof(YumeCore).Assembly.GetCustomAttributes<TargetFrameworkAttribute>().First().FrameworkName, DefaultFrameworkNameProvider.Instance);
	}

	public async Task LoadPluginsAsync(CancellationToken ct = default)
	{
		ImmutableArray<SourceRepository> sourceRepositories = _sourceRepositoryProvider.GetRepositories().ToImmutableArray();

		if (_pluginProperties is { EnabledPlugins.Count: > 0 })
		{
			string pluginsDirectory = _coreProperties.Path_Plugins;
			ISettings? nugetSettings = Settings.LoadDefaultSettings(pluginsDirectory);
			
			foreach (string pluginName in _pluginProperties.EnabledPlugins.AsParallel())
			{
				PackageIdentity? pluginPackageIdentity = await GetPackageIdentityAsync(pluginName);

				if (pluginPackageIdentity is null)
				{
					continue;
				}

				// Check if the package isn't already fetched and installed locally
				if (!File.Exists(Path.Combine(pluginsDirectory, pluginPackageIdentity.Id, $"{pluginPackageIdentity.Id}.dll" )))
				{
					List<SourcePackageDependencyInfo> allPackages = new();
					await GetPackageDependenciesAsync(pluginPackageIdentity, _nugetFramework, sourceRepositories, DependencyContext.Default, allPackages, ct);
					await InstallPluginPackagesAsync(pluginPackageIdentity, GetPluginPackagesToInstall(pluginPackageIdentity, allPackages), pluginsDirectory, nugetSettings, ct);
				
					await FlattenDownloadedPackageToDirectoryStructureAsync(new(Path.Combine(pluginsDirectory, pluginName, "dl")), new(Path.Combine(pluginsDirectory, pluginName)), ct);

					_logger.LogDebug("Fetched plugin {PluginName} from NuGet.", pluginName);
				}
				else
				{
					_logger.LogDebug("Plugin {PluginName} is already present locally, ignoring NuGet fetch.", pluginName);
				}
			}
		}
	}

	private async Task<PackageIdentity?> GetPackageIdentityAsync(string packageName, string? version = null, bool allowPrerelease = false)
	{
		// Go through each repository.
		// If a repository contains only pre-release packages (e.g. AutoStep CI), and 
		// the configuration doesn't permit pre-release versions, the search will look at other ones (e.g. NuGet).
		foreach (SourceRepository? repository in _sourceRepositoryProvider.GetRepositories())
		{
			FindPackageByIdResource? findPackageResource = await repository.GetResourceAsync<FindPackageByIdResource>();
			IEnumerable<NuGetVersion>? allVersions = await findPackageResource.GetAllVersionsAsync(packageName, _sourceCacheContext, NullLogger.Instance, CancellationToken.None);

			NuGetVersion? selectedVersion;

			if (version is not null)
			{
				if (!VersionRange.TryParse(version, out VersionRange? range))
				{
					throw new InvalidOperationException($"Invalid version range provided for package {packageName}: {version}");
				}
				
				// Find the best package version match for the range.
				// Consider pre-release versions if pre-releases are allowed.
				selectedVersion = range.FindBestMatch(allVersions.Where(v => allowPrerelease || !v.IsPrerelease));
			}
			else
			{
				// No version; choose the latest, allow pre-release if configured.
				selectedVersion = allVersions.LastOrDefault(v => v.IsPrerelease == allowPrerelease);
			}
			
			if (selectedVersion is not null)
			{
				return new(packageName, selectedVersion);
			}
		}

		_logger.LogWarning("Could not find package {PackageName} {Version}.", packageName, version);
		return null;
	}

	private async Task GetPackageDependenciesAsync(PackageIdentity package, NuGetFramework framework, ImmutableArray<SourceRepository> repositories,
		DependencyContext hostDependencies, ICollection<SourcePackageDependencyInfo> availablePackages, CancellationToken ct)
	{
		// Don't recurse over a package we've already seen.
		if (availablePackages.Contains(package))
		{
			return;
		}
 
		foreach (SourceRepository? sourceRepository in _sourceRepositoryProvider.GetRepositories())
		{
			// Get the dependency info for the package.
			DependencyInfoResource? dependencyInfoResource = await sourceRepository.GetResourceAsync<DependencyInfoResource>(ct);
			SourcePackageDependencyInfo? dependencyInfo = await dependencyInfoResource.ResolvePackage(package, framework, _sourceCacheContext, NullLogger.Instance, ct);
 
			// No info for the package in this repository.
			if (dependencyInfo is null)
			{
				continue;
			}
 
			// Filter the dependency info.
			// Don't bring in any dependencies that are provided by the host.
			SourcePackageDependencyInfo actualSourceDep = new(
				dependencyInfo.Id,
				dependencyInfo.Version,
				dependencyInfo.Dependencies.Where(dep => !DependencySuppliedByHost(hostDependencies, dep)),
				dependencyInfo.Listed,
				dependencyInfo.Source);
 
			availablePackages.Add(actualSourceDep);
			
			// Add to the list of all packages.
			availablePackages.Add(actualSourceDep);
			_logger.LogTrace("Found package {PackageName} {PackageVersion}.", dependencyInfo.Id, dependencyInfo.Version);
 
			// Recurse through each package.
			foreach (PackageDependency? dependency in actualSourceDep.Dependencies)
			{
				_logger.LogTrace("Introspecting dependency {DependencyId} {DependencyVersion} for package {PackageId} {PackageVersion}",
					dependency.Id, dependency.VersionRange, package.Id, package.Version);
				
				await GetPackageDependenciesAsync(new(dependency.Id, dependency.VersionRange.MinVersion), framework, repositories, hostDependencies, availablePackages, ct); 
			}
 
			break;
		}
	}

	
	private IEnumerable<(SourcePackageDependencyInfo, bool)> GetPackagesToInstall(IEnumerable<string> pluginNames, ICollection<SourcePackageDependencyInfo> allPackages)
	{
		// Create a package resolver context.
		PackageResolverContext resolverContext = new(
			DependencyBehavior.Highest,
			pluginNames,
			Enumerable.Empty<string>(),
			Enumerable.Empty<PackageReference>(),
			Enumerable.Empty<PackageIdentity>(),
			allPackages,
			_sourceRepositoryProvider.GetRepositories().Select(s => s.PackageSource),
			NullLogger.Instance);
		
		// Create a package resolver.
		PackageResolver resolver = new();
		
		// Resolve the packages and return the results.
		return resolver.Resolve(resolverContext, CancellationToken.None)
			.Select(p => (allPackages.Single(x => PackageIdentityComparer.Default.Equals(x, p)), pluginNames.Contains(p.Id)));
	}

	private IEnumerable<SourcePackageDependencyInfo> GetPluginPackagesToInstall(PackageIdentity pluginPackage, IEnumerable<SourcePackageDependencyInfo> allPackages)
	{
		// Create a package resolver context.
		PackageResolverContext resolverContext = new(
			DependencyBehavior.Highest,
			new[] { pluginPackage.Id },
			Enumerable.Empty<string>(),
			Enumerable.Empty<PackageReference>(),
			Enumerable.Empty<PackageIdentity>(),
			allPackages,
			_sourceRepositoryProvider.GetRepositories().Select(s => s.PackageSource),
			NullLogger.Instance);
		
		// Create a package resolver.
		PackageResolver resolver = new();
		
		// Resolve the packages and return the results.
		return resolver.Resolve(resolverContext, CancellationToken.None)
			.Select(p => allPackages.First(x => PackageIdentityComparer.Default.Equals(x, p)));
	}

	private async Task InstallPackagesAsync(IEnumerable<(SourcePackageDependencyInfo, bool)> packagesToInstall, string rootPackagesDirectory, ISettings nugetSettings, CancellationToken ct)
	{
		PackagePathResolver pluginPackagesPathResolver = new(rootPackagesDirectory);
		PackagePathResolver dependenciesPathResolver = new(Path.Combine(rootPackagesDirectory, "dependencies"));
		
		PackageExtractionContext packageExtractionContext = new(
			PackageSaveMode.Files,
			XmlDocFileSaveMode.Skip,
			ClientPolicyContext.GetClientPolicy(nugetSettings, NullLogger.Instance), NullLogger.Instance);

		foreach ((SourcePackageDependencyInfo package, bool isPlugin) in packagesToInstall)
		{
			DownloadResource? downloadResource = await package.Source.GetResourceAsync<DownloadResource>(ct);
			
			// Download the package (or fetch it from the cache).
			DownloadResourceResult downloadResult = await downloadResource.GetDownloadResourceResultAsync(
				package, new(_sourceCacheContext), SettingsUtility.GetGlobalPackagesFolder(nugetSettings), NullLogger.Instance, ct);
			
			// Extract the package into the target directory.
			await PackageExtractor.ExtractPackageAsync(downloadResult.PackageSource, downloadResult.PackageStream, 
				isPlugin ? pluginPackagesPathResolver : dependenciesPathResolver,
				packageExtractionContext, ct);
		}
	}

	private async Task InstallPluginPackagesAsync(PackageIdentity plugin, IEnumerable<SourcePackageDependencyInfo> packages, string rootPluginsDirectory, ISettings nugetSettings, CancellationToken ct)
	{
		PluginDependenciesPathResolver pluginDependenciesPathResolver = new(rootPluginsDirectory, plugin.Id);
		
		PackageExtractionContext packageExtractionContext = new(
			PackageSaveMode.Files,
			XmlDocFileSaveMode.Skip,
			ClientPolicyContext.GetClientPolicy(nugetSettings, NullLogger.Instance), 
			NullLogger.Instance);

		foreach (SourcePackageDependencyInfo package in packages)
		{
			DownloadResource? downloadResource = await package.Source.GetResourceAsync<DownloadResource>(ct);
			
			// Download the package (or fetch it from the cache).
			DownloadResourceResult downloadResult = await downloadResource.GetDownloadResourceResultAsync(
				package, new(_sourceCacheContext), SettingsUtility.GetGlobalPackagesFolder(nugetSettings), NullLogger.Instance, ct);
			
			// Extract the package into the target directory.
			await PackageExtractor.ExtractPackageAsync(downloadResult.PackageSource, downloadResult.PackageStream, 
				pluginDependenciesPathResolver,
				packageExtractionContext, ct);
		}
	}
	
	public void Dispose()
	{
		_sourceCacheContext.Dispose();
		GC.SuppressFinalize(this);
	}
	
	private static bool DependencySuppliedByHost(DependencyContext hostDependencies, PackageDependency dep)
	{
		// See if a runtime library with the same ID as the package is available in the host's runtime libraries.
		RuntimeLibrary? runtimeLib = hostDependencies.RuntimeLibraries.FirstOrDefault(r => r.Name == dep.Id);
 
		// If not, does it exist as a .dll file, in the host's runtime directory?
		if (runtimeLib is null)
		{
			if (File.Exists(Path.Combine(Assembly.GetExecutingAssembly().Location, dep.Id + ".dll")))
			{
				return true;
			}
		}
		
		return runtimeLib is not null;
	}
	
	private static async Task FlattenDownloadedPackageToDirectoryStructureAsync(DirectoryInfo downloadFolder, DirectoryInfo targetFolder, CancellationToken ct = default)
	{
		List<DirectoryInfo> directories = new(downloadFolder.GetDirectories("*", SearchOption.AllDirectories).ToList());
		List<FileInfo> files = new();

		// Sort all directories by matched moniker, then by last version, and pull all .dll files to "files" list.
		foreach (Regex regex in TargetMonikerRegexes)
		{
			foreach (DirectoryInfo dir in directories.Where(d => regex.IsMatch(d.FullName)).OrderByDescending(d => d.Name, StringComparer.OrdinalIgnoreCase))
			{
				files.AddRange(dir.GetFiles("*.dll", SearchOption.AllDirectories));
			}
		}
		
		// Move all selected files to the target folder (if not present already).
		foreach (FileInfo file in files.AsParallel())
		{
			if (!File.Exists(Path.Combine(targetFolder.FullName, file.Name)))
			{
				file.MoveTo(Path.Combine(targetFolder.FullName, file.Name));
			}
		}

		// Delete the downloaded package (download folder).
		downloadFolder.Delete(true);
	}
}

public static class NugetUtilities
{
	public static IEnumerable<Regex> TargetMonikerRegexes { get; } = new List<Regex>
	{
		// net x.x (5.0+) moniker
		new(@"net\d+\.\d+"),
		
		// netstandard x.x moniker
		new(@"netstandard\d+\.\d+"),
		
		// netcoreapp x.x moniker
		new(@"netcoreapp\d+\.\d+"),

		// net xx moniker (Framework)
		new(@"net\d+"),
		
		// Catch-all & non-monikers (last-resort default)
		new(@".*")
	};
}