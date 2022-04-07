using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Security;
using Microsoft.Extensions.Logging;
using Unity;
using YumeChan.PluginBase;

namespace YumeChan.Core.Services.Plugins;

internal class PluginsLoader
{
	internal protected List<Assembly> PluginAssemblies { get; set; }
	internal protected List<FileInfo> PluginFiles { get; set; }
	public List<IPlugin> PluginManifests { get; set; }

	public DirectoryInfo PluginsLoadDirectory { get; set; }
	internal string PluginsLoadDiscriminator { get; set; } = string.Empty;

	public PluginsLoader(string pluginsLoadDirectoryPath)
	{
		PluginsLoadDirectory = string.IsNullOrEmpty(pluginsLoadDirectoryPath)
			? SetDefaultPluginsDirectoryEnvironmentVariable()
			: Directory.Exists(pluginsLoadDirectoryPath)
				? new DirectoryInfo(pluginsLoadDirectoryPath)
				: Directory.CreateDirectory(pluginsLoadDirectoryPath);
	}

	protected virtual DirectoryInfo SetDefaultPluginsDirectoryEnvironmentVariable()
	{
		FileInfo file = new(Assembly.GetExecutingAssembly().Location);
		PluginsLoadDirectory = Directory.CreateDirectory(Path.Join(file.DirectoryName, "Plugins"));

		try
		{
			Environment.SetEnvironmentVariable("YumeChan.PluginsLocation", PluginsLoadDirectory.FullName);
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				Environment.SetEnvironmentVariable("YumeChan.PluginsLocation", PluginsLoadDirectory.FullName, EnvironmentVariableTarget.User);
			}
		}
		catch (SecurityException e)
		{
			YumeCore.Instance.Logger.Log(LogLevel.Warning, e, "Failed to write Environment Variable \"YumeChan.PluginsLocation\".");
		}
		return PluginsLoadDirectory;
	}

	public virtual List<FileInfo> ScanDirectoryForPluginFiles()
	{
		List<FileInfo> files = new(PluginsLoadDirectory.GetFiles($"*{PluginsLoadDiscriminator}*.dll"));

		IEnumerable<DirectoryInfo> directories = PluginsLoadDirectory.GetDirectories("*", SearchOption.AllDirectories);

#if DEBUG
		directories = directories.Where(d => d.Name is not "ref");
#endif

		foreach (DirectoryInfo dir in directories)
		{
			files.AddRange(dir.GetFiles("*.dll"));
		}

		return PluginFiles = files;
	}

	public virtual void LoadPluginAssemblies()
	{
		PluginAssemblies ??= new();

		// Try to load the assemblies from the file system, warn in console if unsuccessful.
		foreach (FileInfo file in PluginFiles.DistinctBy(f => f.Name).Where(f => f is not null && f.Name != Path.GetFileName(typeof(IPlugin).Assembly.Location)))
		{
			try
			{
				PluginAssemblies.Add(AssemblyLoadContext.Default.LoadFromAssemblyPath(file.FullName));
			}
			catch (Exception e)
			{
				YumeCore.Instance.Logger.LogWarning(e,"Failed to load assembly \"{File.Name}\".", file);
			}
		}
	}

	public virtual IEnumerable<IPlugin> LoadPluginManifests()
	{
		List<IPlugin> plugins = new();

		foreach (Assembly a in PluginAssemblies)
		{
			try
			{
				// Try to load the plugin manifests from the assemblies, log error in console if unsuccessful.
				foreach (Type t in a.ExportedTypes.Where(x => x.ImplementsInterface(typeof(IPlugin))))
				{
					try
					{
						plugins.Add(InstantiateManifest(t));
					}
					catch (Exception e)
					{
						YumeCore.Instance.Logger.LogError(e,"Failed to instantiate plugin \"{PluginName}\".", t?.Name);
					}
				}
			}
			catch (Exception e)
			{
				YumeCore.Instance.Logger.LogError(e,"Failed to load plugin manifests from assembly \"{AssemblyFullName}\".", a?.FullName);
			}
		}

		return plugins;
	}

	public virtual IEnumerable<DependencyInjectionHandler> LoadDependencyInjectionHandlers()
	{
		List<DependencyInjectionHandler> handlers = new();

		foreach (Assembly a in PluginAssemblies)
		{
			try
			{
				// Try to load the dependency injection handlers from the assemblies, log error in console if unsuccessful.
				foreach (Type type in a.ExportedTypes.Where(t => t.IsSubclassOf(typeof(DependencyInjectionHandler))))
				{
					try
					{
						handlers.Add(InstantiateInjectionRegistry(type));
					}
					catch (Exception e)
					{
						YumeCore.Instance.Logger.LogError(e, "Failed to instantiate dependency injection handler \"{TypeName}\".", type?.Name);
					}
				}
			}
			catch (Exception e)
			{
				YumeCore.Instance.Logger.LogError(e, "Failed to load dependency injection handler types from assembly \"{AssemblyFullName}\".", a?.FullName);
			}
		}
		
		return handlers;
	}

	internal static IPlugin InstantiateManifest(Type type) => YumeCore.Instance.Services.Resolve(type) as IPlugin;
	internal static DependencyInjectionHandler InstantiateInjectionRegistry(Type type) => YumeCore.Instance.Services.Resolve(type) as DependencyInjectionHandler;
}