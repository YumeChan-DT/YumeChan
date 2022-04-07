#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
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
		_nugetFramework = NuGetFramework.ParseFolder(typeof(YumeCore).Assembly.GetCustomAttributes<TargetFrameworkAttribute>().First().ToString());
	}

	public async Task LoadPluginsAsync(CancellationToken ct = default)
	{
		ImmutableArray<SourceRepository> sourceRepositories = _sourceRepositoryProvider.GetRepositories().ToImmutableArray();
		List<SourcePackageDependencyInfo> allPackages = new();

		if (_pluginProperties is { EnabledPlugins.Count: > 0 })
		{
			foreach (string pluginName in _pluginProperties.EnabledPlugins)
			{
				PackageIdentity? packageIdentity = await GetPackageIdentityAsync(pluginName);

				if (packageIdentity is not null)
				{
					await GetPackageDependenciesAsync(packageIdentity, _nugetFramework, sourceRepositories, allPackages, ct);
				}
			}
		
			// Get packages to install
			IEnumerable<SourcePackageDependencyInfo> packagesToInstall = GetPackagesToInstall(_pluginProperties.EnabledPlugins, allPackages);

			string pluginsDirectory = _coreProperties.Path_Plugins;
			ISettings? nugetSettings = Settings.LoadDefaultSettings(pluginsDirectory);
		
			await InstallPackagesAsync(packagesToInstall, pluginsDirectory, nugetSettings, ct);
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
		ICollection<SourcePackageDependencyInfo> availablePackages, CancellationToken ct)
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
 
			// Add to the list of all packages.
			availablePackages.Add(dependencyInfo);
 
			// Recurse through each package.
			foreach (PackageDependency? dependency in dependencyInfo.Dependencies)
			{
				await GetPackageDependenciesAsync(new(dependency.Id, dependency.VersionRange.MinVersion), framework, repositories, availablePackages, ct); 
			}
 
			break;
		}
	}

	private IEnumerable<SourcePackageDependencyInfo> GetPackagesToInstall(IEnumerable<string> pluginNames, ICollection<SourcePackageDependencyInfo> allPackages)
	{
		// Create a package resolver context.
		PackageResolverContext resolverContext = new(
			DependencyBehavior.Lowest,
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
			.Select(p => allPackages.Single(x => PackageIdentityComparer.Default.Equals(x, p)));
	}

	private async Task InstallPackagesAsync(IEnumerable<SourcePackageDependencyInfo> packagesToInstall, string rootPackagesDirectory, ISettings nugetSettings, CancellationToken ct)
	{
		PackagePathResolver packagePathResolver = new(rootPackagesDirectory);
		PackageExtractionContext packageExtractionContext = new(
			PackageSaveMode.Defaultv3,
			XmlDocFileSaveMode.Skip,
			ClientPolicyContext.GetClientPolicy(nugetSettings, NullLogger.Instance), NullLogger.Instance);

		foreach (SourcePackageDependencyInfo package in packagesToInstall)
		{
			DownloadResource? downloadResource = await package.Source.GetResourceAsync<DownloadResource>(ct);
			
			// Download the package (or fetch it from the cache).
			DownloadResourceResult downloadResult = await downloadResource.GetDownloadResourceResultAsync(
				package, new(_sourceCacheContext), SettingsUtility.GetGlobalPackagesFolder(nugetSettings), NullLogger.Instance, ct);
			
			// Extract the package into the target directory.
			await PackageExtractor.ExtractPackageAsync(downloadResult.PackageSource, downloadResult.PackageStream, packagePathResolver, packageExtractionContext, ct);
		}
	}
	
	public void Dispose()
	{
		_sourceCacheContext.Dispose();
		GC.SuppressFinalize(this);
	}
}