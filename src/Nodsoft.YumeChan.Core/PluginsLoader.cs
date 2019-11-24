using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Nodsoft.YumeChan.PluginBase;

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

		private DirectoryInfo SetDefaultPluginsDirectoryEnvironmentVariable()
		{
			FileInfo file = new FileInfo(Assembly.GetExecutingAssembly().Location);
			PluginsLoadDirectory = Directory.CreateDirectory(file.DirectoryName + Path.DirectorySeparatorChar + "Plugins" + Path.DirectorySeparatorChar);

			Environment.SetEnvironmentVariable("YumeChan.PluginsLocation", PluginsLoadDirectory.FullName);
			return PluginsLoadDirectory;
		}

		public Task LoadPluginAssemblies()
		{
			PluginFiles = new List<FileInfo>(PluginsLoadDirectory.GetFiles($"*{PluginsLoadDiscriminator}*.dll"));

			if (PluginAssemblies is null)
			{
				PluginAssemblies = new List<Assembly>();
			}


			PluginAssemblies.AddRange
			(
				from FileInfo file in PluginFiles
				where file! is null || file.Name != Path.GetFileName(typeof(IPlugin).Assembly.Location)
				select Assembly.LoadFile(file.ToString())
			);

			return Task.CompletedTask;
		}

		public async Task<List<IPlugin>> LoadPluginManifests()
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
				manifestsList.Add(await InstantiateManifest(pluginType));
			}

			return manifestsList;
		}

		internal static Task<IPlugin> InstantiateManifest(Type typePlugin)
		{
			if (Activator.CreateInstance(typePlugin) is IPlugin pluginManifest)
			{
				return Task.FromResult(pluginManifest);
			}

			throw new InvalidCastException();
		}
	}
}
