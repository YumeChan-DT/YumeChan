using YumeChan.PluginBase;
using System;
using System.Threading.Tasks;

namespace YumeChan.Core.Modules
{
	internal class InternalPlugin : Plugin
	{
		public override string DisplayName { get; } = "YumeCore Internals";

		public override bool StealthMode { get; } = false;
	}
}
