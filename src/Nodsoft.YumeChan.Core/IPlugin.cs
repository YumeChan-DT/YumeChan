using System;
using System.Collections;
using System.Threading.Tasks;

namespace Nodsoft.YumeChan.Core
{
	public interface IPlugin
	{
		public Version PluginVersion { get; }

		public string PluginDisplayName { get; }

		public bool PluginStealth { get; }
		public bool PluginLoaded { get; }

		public Task LoadPlugin();
		public Task UnloadPlugin();
	}
}
