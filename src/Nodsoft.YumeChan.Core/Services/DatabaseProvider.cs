using System;
using MongoDB.Driver;
using Nodsoft.YumeChan.Core.Config;
using Nodsoft.YumeChan.PluginBase;
using Nodsoft.YumeChan.PluginBase.Tools.Data;



namespace Nodsoft.YumeChan.Core.Services
{
	public class DatabaseProvider<TPlugin> : IDatabaseProvider<TPlugin> where TPlugin : Plugin
	{
		private static ICoreDatabaseProperties BaseSettings => YumeCore.Instance.CoreProperties.DatabaseProperties;
		private const string pluginDbPrefix = "yc-plugin-";
		private string connectionString;
		private string databaseName;

		public DatabaseProvider()
		{
			connectionString = BaseSettings.ConnectionString;
			databaseName = pluginDbPrefix + typeof(TPlugin).Assembly.GetName().Name.ToLowerInvariant().Replace('.', '-');
		}
		public void SetDb(string connectionString, string databaseName)
		{
			this.connectionString = connectionString;
			this.databaseName = databaseName;
		}


		public IMongoDatabase GetMongoDatabase()
		{ 
			return new MongoClient(connectionString).GetDatabase(databaseName);
		}
	}
}
