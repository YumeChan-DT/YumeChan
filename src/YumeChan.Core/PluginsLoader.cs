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
		public List<Plugin> PluginManifests { get; set; }

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
			List<FileInfo> files = new List<FileInfo>(PluginsLoadDirectory.GetFiles($"*{PluginsLoadDiscriminator}*.dll"));

			foreach (DirectoryInfo dir in PluginsLoadDirectory.GetDirectories())
			{
				if (dir.GetFiles().FirstOrDefault(x => x.Name == $"{dir.Name}.dll") is FileInfo file)
				{
					files.Add(file);
				}
			}

			return PluginFiles = files;
		}

		public virtual void LoadPluginAssemblies()
		{
			PluginAssemblies ??= new List<Assembly>();

			PluginAssemblies.AddRange
			(
				from FileInfo file in PluginFiles
				where file is not null
				where file.Name != Path.GetFileName(typeof(Plugin).Assembly.Location)
				select AssemblyLoadContext.Default.LoadFromAssemblyPath(file.ToString())
			);
		}

		public virtual IEnumerable<Plugin> LoadPluginManifests() =>
			from Assembly a in PluginAssemblies
			from Type t in a.ExportedTypes
			where t.IsSubclassOf(typeof(Plugin))
			select InstantiateManifest(t);

		public virtual IEnumerable<DependencyInjectionHandler> LoadDependencyInjectionHandlers() =>
			from Assembly a in PluginAssemblies
			from Type t in a.ExportedTypes
			where t.IsSubclassOf(typeof(DependencyInjectionHandler))
			select InstantiateInjectionRegistry(t);

		internal static Plugin InstantiateManifest(Type type) => YumeCore.Instance.Services.Resolve(type) as Plugin;
		internal static DependencyInjectionHandler InstantiateInjectionRegistry(Type type) => YumeCore.Instance.Services.Resolve(type) as DependencyInjectionHandler;
	}
}
