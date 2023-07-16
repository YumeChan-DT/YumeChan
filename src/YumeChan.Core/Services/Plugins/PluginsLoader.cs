using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Security;
using DryIoc;
using Microsoft.Extensions.Logging;
using YumeChan.PluginBase;

namespace YumeChan.Core.Services.Plugins;

#nullable enable

public sealed class PluginsLoader
{
	public IReadOnlyDictionary<string, Assembly> PluginAssemblies => _pluginAssemblies;
	public IReadOnlyDictionary<string, IPlugin> PluginManifests => PluginManifestsInternal;

	public DirectoryInfo PluginsLoadDirectory { get; internal set; }
	internal string PluginsLoadDiscriminator { get; set; } = string.Empty;

	internal readonly Dictionary<string, IPlugin> PluginManifestsInternal = new();
	private readonly Dictionary<string, Assembly> _pluginAssemblies = new();

	private readonly List<Assembly> _loadAssemblies = new();
	private readonly List<FileInfo> _pluginFiles = new();
	
	private const string PluginsLocationEnvVarName = "YumeChan_PluginsLocation";
	
	
	public PluginsLoader(string? pluginsLoadDirectoryPath)
	{
		PluginsLoadDirectory = string.IsNullOrEmpty(pluginsLoadDirectoryPath)
			? SetDefaultPluginsDirectoryEnvironmentVariable()
			: Directory.Exists(pluginsLoadDirectoryPath)
				? new(pluginsLoadDirectoryPath)
				: Directory.CreateDirectory(pluginsLoadDirectoryPath);
	}

	private DirectoryInfo SetDefaultPluginsDirectoryEnvironmentVariable()
	{
		FileInfo file = new(Assembly.GetExecutingAssembly().Location);
		PluginsLoadDirectory = Directory.CreateDirectory(Path.Join(file.DirectoryName, "plugins"));
		Task.Run(() => SetPluginsDirectoryEnvironmentVariables(value: PluginsLoadDirectory.FullName));
		return PluginsLoadDirectory;
	}

	private static void SetPluginsDirectoryEnvironmentVariables(string varName = PluginsLocationEnvVarName, string? value = null)
	{
		try
		{
			Environment.SetEnvironmentVariable(varName, value);
			
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				Environment.SetEnvironmentVariable(varName, value, EnvironmentVariableTarget.User);
			}
		}
		catch (SecurityException e)
		{
			YumeCore.Instance.Logger.LogWarning(e, "Failed to write Environment Variable {variableName}.", varName);
		}
	}

	internal void ScanDirectoryForPluginFiles()
	{
		_pluginFiles.Clear();
		_pluginFiles.AddRange(PluginsLoadDirectory.GetFiles($"*{PluginsLoadDiscriminator}*.dll"));
		
		IEnumerable<DirectoryInfo> directories = PluginsLoadDirectory.GetDirectories("*", SearchOption.AllDirectories);

#if DEBUG
		directories = directories.Where(d => d.Name is not "ref");
#endif

		foreach (DirectoryInfo dir in directories)
		{
			_pluginFiles.AddRange(dir.GetFiles("*.dll"));
		}
	}

	internal void LoadPluginAssemblies()
	{
		_loadAssemblies.Clear();


		// Try to load the assemblies from the file system, warn in console if unsuccessful.
		foreach (FileInfo file in _pluginFiles
					.DistinctBy(f => f.Name)
					.Where(f => f.Name != Path.GetFileName(typeof(IPlugin).Assembly.Location))
					.OrderBy(f => f.Name)) // OrderBy is necessary to prevent cross-platform issues.
		{
			try
			{
				Assembly a = AssemblyLoadContext.Default.LoadFromAssemblyPath(file.FullName);

				// Check if the assembly loads its types properly.
				if (a.GetExportedTypes().Any())
				{
					_loadAssemblies.Add(a);
				}
			}
			// Catch any assembly with bad IL.
			catch (BadImageFormatException)
			{
				YumeCore.Instance.Logger.LogDebug("Assembly {assemblyName} is not a valid MSIL assembly.", file.FullName);
			}
			
			// Catch any assemblies with dependency issues, or broken types.
			catch (ReflectionTypeLoadException e) when (e.LoaderExceptions.Any(x => x?.GetType() == typeof(FileNotFoundException)))
			{
				YumeCore.Instance.Logger.LogDebug(e, "Assembly {fileName} is not suitable for loading, skipping it.", file.FullName);
				
			}
			// Anything else is strange. Log it as a warning.
			catch (Exception e)
			{
				YumeCore.Instance.Logger.LogWarning(e,"Failed to load assembly \"{fileName}\".", file);
			}
		}
	}

	internal void ImportPlugin(IPlugin plugin, Assembly? assembly = null)
	{
		// Try add the plugin to the list of loaded plugins.
		PluginManifestsInternal.TryAdd(plugin.AssemblyName, plugin);
						
		// Also try add the assembly to the list of plugin assemblies.
		_pluginAssemblies.TryAdd(plugin.AssemblyName, assembly ?? plugin.GetType().Assembly);
	}

	internal Dictionary<Assembly, Type> GetPluginManifestTypes() => new(
		from a in _loadAssemblies
		from t in a.ExportedTypes
		where t.ImplementsInterface(typeof(IPlugin))
		select new KeyValuePair<Assembly, Type>(a, t)
	);

	internal IEnumerable<IPlugin> LoadPluginManifests(IEnumerable<KeyValuePair<Assembly, Type>>? pluginTypes = null)
	{
		PluginManifestsInternal.Clear();

		pluginTypes ??= GetPluginManifestTypes();
		
		foreach (var (a, t) in pluginTypes)
		{
			try
			{
				// Moment of truth...
				IPlugin plugin = InstantiateManifest(t)!;
				
				// It works! Import it.
				ImportPlugin(plugin, a);
			}
			catch (Exception e)
			{
				YumeCore.Instance.Logger.LogError(e,"Failed to instantiate plugin {pluginName}.", t.Name);
			}
		}

		return PluginManifestsInternal.Values;
	}

	internal IEnumerable<DependencyInjectionHandler> LoadDependencyInjectionHandlers()
	{
		List<DependencyInjectionHandler> handlers = new();

		foreach (Assembly a in _loadAssemblies)
		{
			try
			{
				// Try to load the dependency injection handlers from the assemblies, log error in console if unsuccessful.
				foreach (Type type in a.ExportedTypes.Where(static t => t.IsSubclassOf(typeof(DependencyInjectionHandler))))
				{
					try
					{
						handlers.Add(InstantiateInjectionRegistry(type)!);
					}
					catch (Exception e)
					{
						YumeCore.Instance.Logger.LogError(e, "Failed to instantiate dependency injection handler {typeName}.", type.Name);
					}
				}
			}
			catch (Exception e)
			{
				YumeCore.Instance.Logger.LogError(e, "Failed to load dependency injection handler types from assembly {assemblyFullName}.", a.FullName);
			}
		}
		
		return handlers;
	}

	private static IPlugin? InstantiateManifest(Type type) => YumeCore.Instance.Services.Resolve(type) as IPlugin;
	private static DependencyInjectionHandler? InstantiateInjectionRegistry(Type type) => YumeCore.Instance.Services.Resolve(type) as DependencyInjectionHandler;
}