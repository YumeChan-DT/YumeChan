using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace Nodsoft.YumeChan.Core.Config
{
	internal class ConfigurationProvider
	{
		internal IConfigurationBuilder ConfigBuilder { get; private set; }

		internal IConfigurationRoot Configuration { get; private set; }

		public ConfigurationProvider()
		{
			ConfigBuilder = InitDefaultConfiguration();
			Configuration = ConfigBuilder.Build();
		}

		public IConfigurationBuilder InitDefaultConfiguration()
		{
			return new ConfigurationBuilder()
						.SetBasePath(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "Config")
						.AddJsonFile("botconfig.json", false, true);
		}

		internal void BindConfigToProperties(CoreProperties properties) => Configuration.Bind(properties, options => options.BindNonPublicProperties = true);

		internal object this[string key]
		{
			get => Configuration.GetSection(nameof(CoreProperties))[key];
			set
			{
				Configuration.GetSection(nameof(CoreProperties))[key] = value as string;
				Configuration.Reload();
			}
		}
	}
}
