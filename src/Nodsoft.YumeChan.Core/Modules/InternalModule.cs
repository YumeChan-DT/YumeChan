using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Nodsoft.YumeChan.Core.Modules
{
	internal class InternalModule : IPlugin
	{
		public Version PluginVersion { get; } = typeof(InternalModule).Assembly.GetName().Version;

		public string PluginDisplayName { get; } = "YumeCore Internals";

		public bool PluginStealth { get; } = false;

		public bool PluginLoaded { get; set; }

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
