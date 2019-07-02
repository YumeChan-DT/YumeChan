using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Nodsoft.YumeChan.Core
{
	internal class ModulesLoader
	{
		internal List<Assembly> ModuleAssemblies { get; set; }
		internal List<FileInfo> ModuleFiles { get; set; }

		internal DirectoryInfo ModulesLoadDirectory { get; set; }
		internal string ModulesLoadDiscriminator { get; set; } = string.Empty;

		public ModulesLoader(string modulesLoadDirectoryPath)
		{
			ModulesLoadDirectory = string.IsNullOrEmpty(modulesLoadDirectoryPath)
				? SetDefaultModulesDirectoryEnvironmentVariable()
				: Directory.Exists(modulesLoadDirectoryPath)
					? new DirectoryInfo(modulesLoadDirectoryPath)
					: Directory.CreateDirectory(modulesLoadDirectoryPath);
		}

		internal DirectoryInfo SetDefaultModulesDirectoryEnvironmentVariable()
		{
			ModulesLoadDirectory = Directory.CreateDirectory(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "Modules" + Path.DirectorySeparatorChar);

			Environment.SetEnvironmentVariable("YumeChan.ModulesLocation", ModulesLoadDirectory.FullName);
			return ModulesLoadDirectory;
		}

		public Task LoadModuleAssemblies()
		{
			ModuleFiles = new List<FileInfo>(ModulesLoadDirectory.GetFiles($"*{ModulesLoadDiscriminator}*.dll"));

			if (ModuleAssemblies is null)
			{
				ModuleAssemblies = new List<Assembly>();
			}

			foreach (FileInfo file in ModuleFiles)
			{
				ModuleAssemblies.Add(Assembly.LoadFile(file.ToString()));
			}

			return Task.CompletedTask;
		}

		public Task<List<IPlugin>> LoadModuleManifests()
		{
			List<IPlugin> manifestsList = new List<IPlugin>();

			foreach (Assembly assembly in ModuleAssemblies)
			{
				foreach (Type type in assembly.ExportedTypes)
				{
					manifestsList.Add(type as IPlugin);
				}
			}
			return Task.FromResult(manifestsList);
		}
	}
}
