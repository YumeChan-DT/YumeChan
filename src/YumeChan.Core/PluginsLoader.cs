using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Security;
using Unity;
using YumeChan.PluginBase;

namespace YumeChan.Core
{
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

			IEnumerable<DirectoryInfo> directories = PluginsLoadDirectory.GetDirectories();

#if DEBUG
			directories = directories.Where(d => d.Name is not "ref");
#endif

			foreach (DirectoryInfo dir in directories)
			{
				files.AddRange(dir.GetFiles().Where(x => x.Extension is ".dll"));
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

		public virtual IEnumerable<IPlugin> LoadPluginManifests() =>
			from Assembly a in PluginAssemblies
			from Type t in a.ExportedTypes
			where t.ImplementsInterface(typeof(IPlugin))
			select InstantiateManifest(t);

		public virtual IEnumerable<DependencyInjectionHandler> LoadDependencyInjectionHandlers() =>
			from Assembly a in PluginAssemblies
			from Type t in a.ExportedTypes
			where t.IsSubclassOf(typeof(DependencyInjectionHandler))
			select InstantiateInjectionRegistry(t);

		internal static IPlugin InstantiateManifest(Type type) => YumeCore.Instance.Services.Resolve(type) as IPlugin;
		internal static DependencyInjectionHandler InstantiateInjectionRegistry(Type type) => YumeCore.Instance.Services.Resolve(type) as DependencyInjectionHandler;
	}
}
