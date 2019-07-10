using Nodsoft.YumeChan.PluginBase;
using System;
using System.Threading.Tasks;

namespace Nodsoft.YumeChan.Core.Modules
{
	internal class InternalPlugin : IPlugin
	{
		public Version PluginVersion { get; } = typeof(InternalPlugin).Assembly.GetName().Version;

		public string PluginDisplayName { get; } = "YumeCore Internals";

		public bool PluginStealth { get; } = false;

		public bool PluginLoaded { get; internal set; }

		public Task LoadPlugin()
		{
			PluginLoaded = true;
			return Task.CompletedTask;
		}

		public Task UnloadPlugin()
		{
			PluginLoaded = false;
			return Task.CompletedTask;
		}
	}
}
