using YumeChan.PluginBase;
using System;
using System.Threading.Tasks;

namespace YumeChan.Core.Modules
{
	public class InternalPlugin : IPlugin
	{
		public string AssemblyName { get; } = typeof(YumeCore).Assembly.GetName().Name;
		public string DisplayName => "YumeCore Internals";
		public bool Loaded => YumeCore.Instance.CoreState is YumeCoreState.Online;

		public bool StealthMode => false;
		public string Version => YumeCore.CoreVersion;
		
		public Task LoadAsync() => Task.CompletedTask;
		public Task UnloadAsync() => Task.CompletedTask;
	}
}
