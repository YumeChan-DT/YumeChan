using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using System;
using YumeChan.Core.Config;
using YumeChan.PluginBase;
using YumeChan.PluginBase.Tools.Data;



namespace YumeChan.Core.Services;

public class DatabaseProvider<TPlugin> : IDatabaseProvider<TPlugin> where TPlugin : IPlugin
{
	private static ICoreProperties CoreProperties => YumeCore.Instance.CoreProperties;
	private const string pluginDbPrefix = "yc-plugin-";
	private string mongoConnectionString;
	private string postgresConnectionString;
	private string databaseName;

	public DatabaseProvider()
	{
		mongoConnectionString = CoreProperties.MongoProperties.ConnectionString;
		postgresConnectionString = CoreProperties.PostgresProperties.ConnectionString;

		databaseName = pluginDbPrefix + typeof(TPlugin).Assembly.GetName().Name.ToLowerInvariant().Replace('.', '-');
	}

	public void SetMongoDb(string connectionString, string databaseName)
	{
		this.mongoConnectionString = connectionString;
		this.databaseName = databaseName;
	}

	public IMongoDatabase GetMongoDatabase()
	{
		return new MongoClient(mongoConnectionString).GetDatabase(databaseName);
	}

	public Action<DbContextOptionsBuilder> GetPostgresContextOptionsBuilder() => context =>
		context.UseNpgsql(postgresConnectionString, providerOptions =>
			providerOptions.EnableRetryOnFailure());
}