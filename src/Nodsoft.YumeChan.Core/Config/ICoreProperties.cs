using System.Runtime.CompilerServices;
using Config.Net;

[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace Nodsoft.YumeChan.Core.Config
{
	public interface ICoreProperties
	{
		[Option(DefaultValue = "YumeChan")]
		public string AppInternalName { get; }
		[Option(DefaultValue = "Yume-Chan")]
		public string AppDisplayName { get; internal set; }

		internal string BotToken { get; }

		public string Path_Core { get; internal set; }
		public string Path_Config { get; internal set; }
		public string Path_Plugins { get; internal set; }
	}
}