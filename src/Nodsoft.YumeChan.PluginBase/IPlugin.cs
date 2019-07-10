using System;
using System.Threading.Tasks;

namespace Nodsoft.YumeChan.PluginBase
{
	public interface IPlugin
	{
		Version PluginVersion { get; }

		string PluginDisplayName { get; }

		bool PluginStealth { get; }
		bool PluginLoaded { get; }

		Task LoadPlugin();
		Task UnloadPlugin();
	}
}
