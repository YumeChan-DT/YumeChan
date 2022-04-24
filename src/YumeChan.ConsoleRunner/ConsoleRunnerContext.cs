using YumeChan.PluginBase.Tools;

namespace YumeChan.ConsoleRunner;

internal record ConsoleRunnerContext(RunnerType RunnerType, string RunnerName, string RunnerVersion) : IRunnerContext;