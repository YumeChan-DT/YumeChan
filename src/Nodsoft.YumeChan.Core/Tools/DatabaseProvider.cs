using System;
using Nodsoft.YumeChan.Core.Config;
using Nodsoft.YumeChan.PluginBase;
using Nodsoft.YumeChan.PluginBase.Tools.Data;

namespace Nodsoft.YumeChan.Core.Tools
{
	public class DatabaseProvider<TPlugin> : IDatabaseProvider<TPlugin> where TPlugin : Plugin
	{
		private static ICoreDatabaseProperties BaseSettings => YumeCore.Instance.CoreProperties.DatabaseProperties;

		private readonly ICoreDatabaseProperties dbProperties = BaseSettings;
		private const string pluginDbPrefix = "yc-plugin-";

		public DatabaseProvider()
		{
			dbProperties = BaseSettings;
			dbProperties.DatabaseName = pluginDbPrefix + typeof(TPlugin).Assembly.GetName().Name.ToLowerInvariant().Replace('.', '-');
		}
		public void SetDb(string connectionString, string databaseName)
		{
			dbProperties.ConnectionString = connectionString;
			dbProperties.DatabaseName = databaseName;
		}

		public IEntityRepository<TEntity> GetEntityRepository<TEntity>() where TEntity : IDocument => new EntityRepository<TEntity>(dbProperties);
	}
}
