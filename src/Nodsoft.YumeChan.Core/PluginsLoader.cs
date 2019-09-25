using Nodsoft.YumeChan.PluginBase;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Linq;

namespace Nodsoft.YumeChan.Core
{
	internal class PluginsLoader
	{
		internal List<Assembly> PluginAssemblies { get; set; }
		internal List<FileInfo> PluginFiles { get; set; }
		internal List<IPlugin> PluginManifests { get; set; }

		internal DirectoryInfo PluginsLoadDirectory { get; set; }
		internal string PluginsLoadDiscriminator { get; set; } = string.Empty;

		public PluginsLoader(string pluginsLoadDirectoryPath)
		{
			PluginsLoadDirectory = string.IsNullOrEmpty(pluginsLoadDirectoryPath)
				? SetDefaultPluginsDirectoryEnvironmentVariable()
				: Directory.Exists(pluginsLoadDirectoryPath)
					? new DirectoryInfo(pluginsLoadDirectoryPath)
					: Directory.CreateDirectory(pluginsLoadDirectoryPath);
		}

		internal DirectoryInfo SetDefaultPluginsDirectoryEnvironmentVariable()
		{
			FileInfo file = new FileInfo(Assembly.GetExecutingAssembly().Location);
			PluginsLoadDirectory = Directory.CreateDirectory(file.DirectoryName + Path.DirectorySeparatorChar + "Modules" + Path.DirectorySeparatorChar);

			Environment.SetEnvironmentVariable("YumeChan.PluginsLocation", PluginsLoadDirectory.FullName);
			return PluginsLoadDirectory;
		}

		public Task LoadModuleAssemblies()
		{
			PluginFiles = new List<FileInfo>(PluginsLoadDirectory.GetFiles($"*{PluginsLoadDiscriminator}*.dll"));

			if (PluginAssemblies is null)
			{
				PluginAssemblies = new List<Assembly>();
			}

			foreach (FileInfo file in PluginFiles)
			{
				if (file !is null || file.Name != "Nodsoft.YumeChan.PluginBase.dll")
				{
					PluginAssemblies.Add(Assembly.LoadFile(file.ToString()));
				}
			}

			return Task.CompletedTask;
		}

		public Task<List<IPlugin>> LoadPluginManifests()
		{
			List<IPlugin> manifestsList = new List<IPlugin>();
			List<Type> pluginTypes = new List<Type>();
			foreach (Assembly assembly in PluginAssemblies)
			{
				pluginTypes.AddRange
				(
					from Type t in assembly.ExportedTypes
					where t.ImplementsInterface(typeof(IPlugin))
					select t
				);
			}
			foreach (Type pluginType in pluginTypes)
			{
				manifestsList.Add(InstantiateManifest(pluginType).GetAwaiter().GetResult());
			}

			return Task.FromResult(manifestsList);
		}

		internal static Task<IPlugin> InstantiateManifest(Type typePlugin)
		{
			object obj = Activator.CreateInstance(typePlugin);
			IPlugin pluginManifest = obj as IPlugin;

			if (pluginManifest is null)
			{
				throw new InvalidCastException();
			}
			return Task.FromResult(pluginManifest);
		}
	}
}
