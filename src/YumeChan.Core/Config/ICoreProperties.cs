using System.Runtime.CompilerServices;


[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
namespace YumeChan.Core.Config
{
	public interface ICoreProperties
	{
		public string AppInternalName { get; internal set; }
		public string AppDisplayName { get; internal set; }

		internal string BotToken { get; set; }

		public string Path_Core { get; internal set; }
		public string Path_Config { get; internal set; }
		public string Path_Plugins { get; internal set; }

		public string CommandPrefix { get; internal set; }

		internal ICoreDatabaseProperties DatabaseProperties { get; set; }
		internal ICoreLavalinkProperties LavalinkProperties { get; set; }
	}

	public interface ICoreDatabaseProperties
	{
		string DatabaseName { get; set; }
		string ConnectionString { get; set; }
	}

	public interface ICoreLavalinkProperties
	{
		string Hostname { get; set; }
		ushort? Port { get; set; }
		string Password { get; set; }
	}
}