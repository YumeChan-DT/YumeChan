using Nodsoft.YumeChan.PluginBase;
using System;
using System.Threading.Tasks;

namespace Nodsoft.YumeChan.Core.Modules
{
	internal class InternalPlugin : Plugin
	{
		public override string PluginDisplayName { get; } = "YumeCore Internals";

		public override bool PluginStealth { get; } = false;
	}
}
