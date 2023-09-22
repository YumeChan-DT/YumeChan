using YumeChan.PluginBase.Tools;

namespace YumeChan.NetRunner;

internal sealed record NetRunnerContext(RunnerType RunnerType, string RunnerName, string RunnerVersion) : IRunnerContext;