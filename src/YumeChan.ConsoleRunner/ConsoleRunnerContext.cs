using YumeChan.PluginBase.Tools;

namespace YumeChan.ConsoleRunner;

internal sealed record ConsoleRunnerContext(RunnerType RunnerType, string RunnerName, string RunnerVersion) : IRunnerContext;