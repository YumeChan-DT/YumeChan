using System.IO;
using Config.Net;

namespace Nodsoft.YumeChan.Core.Config
{
	internal class ConfigurationProvider
	{
		internal ConfigurationBuilder<ICoreProperties> ConfigBuilder { get; private set; }
		public ICoreProperties Configuration { get; private set; }

		public ConfigurationProvider()
		{
			Configuration = InitDefaultConfiguration().Build();
		}
		public ConfigurationBuilder<ICoreProperties> InitDefaultConfiguration()
		{
			return new ConfigurationBuilder<ICoreProperties>().UseJsonFile("Config" + Path.DirectorySeparatorChar + "coreconfig.json");
		}
	}
}
