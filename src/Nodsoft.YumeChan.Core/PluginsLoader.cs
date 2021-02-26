using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Nodsoft.YumeChan.PluginBase;
using Microsoft.Extensions.Logging;
using System.Security;

namespace Nodsoft.YumeChan.Core
{
	internal class PluginsLoader
	{
		internal List<Assembly> PluginAssemblies { get; set; }
		internal List<FileInfo> PluginFiles { get; set; }
		internal List<Plugin> PluginManifests { get; set; }

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
			FileInfo file = new(Assembly.GetExecutingAssembly().Location);
			PluginsLoadDirectory = Directory.CreateDirectory(file.DirectoryName + Path.DirectorySeparatorChar + "Plugins" + Path.DirectorySeparatorChar);

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

		public void LoadPluginAssemblies()
		{
			PluginFiles = new List<FileInfo>(PluginsLoadDirectory.GetFiles($"*{PluginsLoadDiscriminator}*.dll"));
			
			PluginAssemblies ??= new List<Assembly>();
			PluginAssemblies.AddRange
			(
				from FileInfo file in PluginFiles
				where file is not null || file.Name != Path.GetFileName(typeof(Plugin).Assembly.Location)
				select Assembly.LoadFile(file.ToString())
			);
		}

		public IEnumerable<Plugin> LoadPluginManifests() =>
			from Assembly a in PluginAssemblies
			from Type t in a.ExportedTypes
			where t.IsSubclassOf(typeof(Plugin))
			select InstantiateManifest(t);

		internal static Plugin InstantiateManifest(Type typePlugin) => ActivatorUtilities.CreateInstance(YumeCore.Instance.Services, typePlugin) as Plugin;
	}
}
