using System.IO;
using Config.Net;
using YumeChan.PluginBase.Tools;


namespace YumeChan.Core.Services
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

			if (YumeCore.Instance.CoreProperties?.Path_Config is not string rootDirectory)
			{
				rootDirectory = "Config";
			}

			string pluginDirectory = placeFileAtConfigRoot ? string.Empty : typeof(T).Assembly.GetName().Name;
			string fileName = filename.EndsWith(fileExtension) ? filename : (filename + fileExtension);

			ConfigFile = new FileInfo(Path.Join(rootDirectory, pluginDirectory, fileName));

			return Configuration = new ConfigurationBuilder<T>().UseJsonFile(ConfigFile.ToString()).Build();
		}
	}
}
