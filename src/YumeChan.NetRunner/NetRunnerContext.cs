using YumeChan.PluginBase.Tools;

namespace YumeChan.NetRunner;

internal record NetRunnerContext(RunnerType RunnerType, string RunnerName, string RunnerVersion) : IRunnerContext;