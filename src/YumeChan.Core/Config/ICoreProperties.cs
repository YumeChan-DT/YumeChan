using System.Diagnostics.CodeAnalysis;

namespace YumeChan.Core.Config;
#nullable enable


/// <summary>
/// Defines the core properties of Yume-Chan.
/// </summary>
public interface ICoreProperties
{
	/// <summary>
	/// The internal name of the application.
	/// </summary>
	public string AppInternalName { get; internal set; }
	
	/// <summary>
	/// The display name of the application.
	/// </summary>
	public string AppDisplayName { get; internal set; }

	/// <summary>
	/// The Bot Token used to connect to Discord's API Gateway.
	/// </summary>
	/// <remarks>
	/// This property can be left null, however an environment variable named <c>YumeChan_Token</c> must then be set.
	/// </remarks>
	internal string? BotToken { get; set; }

	/// <summary>
	/// The path to the bot's working directory.
	/// </summary>
	public string Path_Core { get; internal set; }
	
	/// <summary>
	/// The path to the bot's configuration directory.
	/// </summary>
	/// <value>
	/// Defaults to <c>{Path_Core}/config"</c>.
	/// </value>
	public string Path_Config { get; internal set; }
	
	/// <summary>
	/// The path to the bot's plugin directory.
	/// </summary>
	/// <value>
	/// Defaults to <c>{Path_Core}/plugins"</c>.
	/// </value>
	public string Path_Plugins { get; internal set; }

	/// <summary>
	/// The prefix used to identify commands.
	/// </summary>
	/// <value>
	/// Defaults to <c>==</c>.
	/// </value>
	public string CommandPrefix { get; internal set; }
	
	/// <summary>
	/// Whether or not to allow plugins dependent on the NetRunner requirements to be loaded.
	/// This is usually answered by determining if the runner is a web application or console application.
	/// </summary>
	[DisallowNull]
	public bool? DisallowNetRunnerPlugins { get; internal set; }

	/// <summary>
	/// The properties used to connect to the MongoDB database.
	/// </summary>
	internal ICoreDatabaseProperties MongoProperties { get; set; }
	
	/// <summary>
	/// The properties used to connect to the PostgreSQL database.
	/// </summary>
	internal ICoreDatabaseProperties PostgresProperties { get; set; }
	
	/// <summary>
	/// The properties used to connect to the Lavalink server.
	/// </summary>
	internal ICoreLavalinkProperties LavalinkProperties { get; set; }
}

/// <summary>
/// Defines the properties used to connect to a database.
/// </summary>
public interface ICoreDatabaseProperties
{
	string DatabaseName { get; set; }
	string ConnectionString { get; set; }
}

/// <summary>
/// Defines the properties used to connect to a Lavalink server.
/// </summary>
public interface ICoreLavalinkProperties
{
	string Hostname { get; set; }
	ushort? Port { get; set; }
	string Password { get; set; }
}