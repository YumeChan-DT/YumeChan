namespace Nodsoft.YumeChan.Core.Config
{
	public class CoreProperties
	{
		public string AppInternalName => _config[nameof(AppInternalName)] as string;
		public string AppDisplayName { get => _config[nameof(AppDisplayName)] as string; set => _config[nameof(AppDisplayName)] = value; }

		internal string BotToken => _config[nameof(BotToken)] as string;

		public string Path_Core { get => _config[nameof(Path_Core)] as string; internal set => _config[nameof(Path_Core)] = value; }
		public string Path_Config { get => _config[nameof(Path_Config)] as string; internal set => _config[nameof(Path_Config)] = value; }
		public string Path_Plugins { get => _config[nameof(Path_Plugins)] as string; internal set => _config[nameof(Path_Plugins)] = value; }


		private readonly ConfigurationProvider _config;
		internal CoreProperties(ConfigurationProvider configuration) => _config = configuration;
	}
}