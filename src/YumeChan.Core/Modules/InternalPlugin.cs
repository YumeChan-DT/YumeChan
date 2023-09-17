using YumeChan.PluginBase;

namespace YumeChan.Core.Modules;

/// <summary>
/// Represents the internal plugin, identifying the core of Yume-Chan.
/// </summary>
public sealed class InternalPlugin : IPlugin
{
	public string AssemblyName { get; } = typeof(YumeCore).Assembly.GetName().Name!;
	public string DisplayName => "YumeCore Internals";
	public string Version => YumeCore.CoreVersion;
	public string Description => "Internal Plugin for YumeChan Core";
	public string Author => "YumeChan DT (Nodsoft Systems)";
	public string AuthorContact => "admin@nodsoft.net";
	public Uri? ProjectHomepage { get; } = new("https://yumechan.app");
	public Uri? SourceCodeRepository { get; } = new("https://github.com/YumeChan-DT/YumeChan");
	
	public bool Loaded => YumeCore.Instance.CoreState is YumeCoreState.Online;
	public Uri? IconUri { get; set; }
	public bool ShouldUseNetRunner => false;
	public bool StealthMode => false;
		
	public Task LoadAsync() => Task.CompletedTask;
	public Task UnloadAsync() => Task.CompletedTask;
}