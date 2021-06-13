using YumeChan.PluginBase;
using System;
using System.Threading.Tasks;

namespace YumeChan.Core.Modules
{
	internal class InternalPlugin : Plugin
	{
		public override string PluginDisplayName { get; } = "YumeCore Internals";

		public override bool PluginStealth { get; } = false;
	}
}
