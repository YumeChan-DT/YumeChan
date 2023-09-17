using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using Npgsql;
using YumeChan.PluginBase;
using YumeChan.PluginBase.Database.MongoDB;
using YumeChan.PluginBase.Database.Postgres;

namespace YumeChan.Core.Services;

/// <summary>
/// Provides a unified solution for spawning plugin-requested database instances.
/// </summary>
/// <typeparam name="TPlugin">The type of the plugin that is requesting the database.</typeparam>
public sealed class UnifiedDatabaseProvider<TPlugin> : IMongoDatabaseProvider<TPlugin>, IPostgresDatabaseProvider<TPlugin> 
	where TPlugin : IPlugin
{
	private const string PluginDbPrefix = "yc-plugin-";
	
	private readonly string _postgresConnectionString = YumeCore.Instance.CoreProperties.PostgresProperties.ConnectionString;
	private readonly string _postgresDatabaseName = $"{PluginDbPrefix}{typeof(TPlugin).Assembly.GetName().Name?.ToLowerInvariant().Replace('.', '_')}";
	
	private string _mongoConnectionString = YumeCore.Instance.CoreProperties.MongoProperties.ConnectionString;
	private string _mongoDbDatabaseName = $"{PluginDbPrefix}{typeof(TPlugin).Assembly.GetName().Name?.ToLowerInvariant().Replace('.', '-')}";


	public void SetMongoDb(string connectionString, string databaseName)
	{
		_mongoConnectionString = connectionString;
		_mongoDbDatabaseName = databaseName;
	}

	public IMongoDatabase GetMongoDatabase() => new MongoClient(_mongoConnectionString).GetDatabase(_mongoDbDatabaseName);

	public Action<DbContextOptionsBuilder<TContext>> GetPostgresContextOptionsBuilder<TContext>() where TContext : DbContext => GetPostgresContextOptionsBuilder();
	
	public Action<DbContextOptionsBuilder> GetPostgresContextOptionsBuilder() => context =>
		context.UseNpgsql(BuildPostgresConnectionString(), providerOptions =>
			providerOptions.EnableRetryOnFailure()
		);

	private string BuildPostgresConnectionString() => new NpgsqlConnectionStringBuilder(_postgresConnectionString)
	{
		BrowsableConnectionString = false,
		Database = _postgresDatabaseName,
		ApplicationName = YumeCore.Instance.CoreProperties.AppInternalName,
	}.ToString();
}