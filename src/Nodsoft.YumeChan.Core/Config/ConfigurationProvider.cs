using System;
using System.IO;
using Config.Net;
using Discord;
using Nodsoft.YumeChan.PluginBase.Tools;


namespace Nodsoft.YumeChan.Core.Config
{
	public class ConfigurationProvider<T> : IConfigProvider<T> where T : class
	{
		internal ConfigurationBuilder<T> ConfigBuilder { get; private set; }

		public T Configuration { get; set; }

		public FileInfo ConfigFile { get; private set; }

		public T InitConfig(string filename) => InitConfig(filename, false);
		internal T InitConfig(string filename, bool placeFileAtConfigRoot)
		{
			const string fileExtension = ".json";

			string rootDirectory;
			try
			{
				rootDirectory = YumeCore.Instance.CoreProperties.Path_Config + Path.DirectorySeparatorChar;
			}
			catch (NullReferenceException e)
			{
				_ = e; // Make these warnings stahp ! Please !	(CA1031)
				rootDirectory = "Config" + Path.DirectorySeparatorChar;
			}

			string pluginDirectory = placeFileAtConfigRoot ? string.Empty : (typeof(T).Assembly.GetName().Name + Path.DirectorySeparatorChar);
			string fileName = filename.EndsWith(fileExtension) ? filename : (filename + fileExtension);

			ConfigFile = new FileInfo(rootDirectory + pluginDirectory + fileName);

			return Configuration = new ConfigurationBuilder<T>().UseJsonFile(ConfigFile.ToString()).Build();
		}
	}
}
