namespace YumeChan.Core.Config;

/// <summary>
/// Represents configuration for a plugins environment.
/// </summary>
public interface IPluginLoaderProperties
{
	/// <summary>
	/// Nuget settings to use for plugin installation.
	/// </summary>
	public INugetProperties Nuget { get; internal set; }

	/// <summary>
	/// List of disabled plugins (we ignore these).
	/// </summary>
	public IList<string> DisabledPlugins { get; internal set; }
	
	/// <summary>
	/// Dictionnary of enabled plugins (load or fetch these).
	/// </summary>
	/// <remarks>
	/// Key is the plugin name, value is the version.
	/// </remarks>
	public IDictionary<string, string?> EnabledPlugins { get; internal set; }
}

/// <summary>
/// Represents configuration for a Nuget plugins fetcher.
/// </summary>
public interface INugetProperties
{
	/// <summary>
	/// Package source feeds to use for plugin installation.
	/// </summary>
	public IList<string> PackageSources { get; internal set; }
	
	/// <summary>
	/// Packages to be excluded from fetching (dependency was already met, etc).
	/// </summary>
	public IList<string> ExcludedPackages { get; internal set; }
}