using YumeChan.PluginBase;

namespace YumeChan.Core.Modules;
#nullable enable

/// <summary>
/// Represents the internal plugin, identifying the core of Yume-Chan.
/// </summary>
public sealed class InternalPlugin : IPlugin
{
	public string AssemblyName { get; } = typeof(YumeCore).Assembly.GetName().Name!;
	public string DisplayName => "YumeCore Internals";
		
	public bool Loaded => YumeCore.Instance.CoreState is YumeCoreState.Online;
	public bool ShouldUseNetRunner => false;
	public bool StealthMode => false;
		
	public string Version => YumeCore.CoreVersion;
		
	public Task LoadAsync() => Task.CompletedTask;
	public Task UnloadAsync() => Task.CompletedTask;
}