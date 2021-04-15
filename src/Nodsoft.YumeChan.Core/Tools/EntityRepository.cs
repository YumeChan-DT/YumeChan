using MongoDB.Driver;
using Nodsoft.YumeChan.Core.Config;
using Nodsoft.YumeChan.PluginBase.Tools.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Nodsoft.YumeChan.Core.Tools
{
	public class EntityRepository<TDocument, TKey> : IEntityRepository<TDocument, TKey>
		where TDocument : IDocument<TKey>
		where TKey : IEquatable<TKey>
	{
		public IMongoCollection<TDocument> Collection { get; private set; }

		public EntityRepository(ICoreDatabaseProperties settings)
		{
			IMongoDatabase database = new MongoClient(settings.ConnectionString).GetDatabase(settings.DatabaseName);
			Collection = database.GetCollection<TDocument>(typeof(TDocument).Name);
		}

		public EntityRepository(string connectionString, string databaseName)
		{
			IMongoDatabase database = new MongoClient(connectionString).GetDatabase(databaseName);
			Collection = database.GetCollection<TDocument>(typeof(TDocument).Name);
		}



		public virtual IQueryable<TDocument> AsQueryable() => Collection.AsQueryable();

		public virtual IEnumerable<TDocument> FilterBy(Expression<Func<TDocument, bool>> filterExpression) => Collection.Find(filterExpression).ToEnumerable();
		public virtual IEnumerable<TProjected> FilterBy<TProjected>(
			Expression<Func<TDocument, bool>> filterExpression, 
			Expression<Func<TDocument, TProjected>> projectionExpression)
		{
			return Collection.Find(filterExpression).Project(projectionExpression).ToEnumerable();
		}

		public virtual TDocument FindOne(Expression<Func<TDocument, bool>> filterExpression) => Collection.Find(filterExpression).FirstOrDefault();
		public virtual Task<TDocument> FindOneAsync(Expression<Func<TDocument, bool>> filterExpression) => Task.Run(() => Collection.Find(filterExpression).FirstOrDefaultAsync());

		public virtual TDocument FindById(TKey id) => Collection.Find(Builders<TDocument>.Filter.Eq(doc => doc.Id, id)).SingleOrDefault();
		public virtual Task<TDocument> FindByIdAsync(TKey id) => Task.Run(() => Collection.Find(Builders<TDocument>.Filter.Eq(doc => doc.Id, id)).SingleOrDefaultAsync());


		public virtual void InsertOne(TDocument document) => Collection.InsertOne(document);
		public virtual Task InsertOneAsync(TDocument document) => Task.Run(() => Collection.InsertOneAsync(document));

		public void InsertMany(ICollection<TDocument> documents) => Collection.InsertMany(documents);
		public virtual async Task InsertManyAsync(ICollection<TDocument> documents) => await Collection.InsertManyAsync(documents);

		public void ReplaceOne(TDocument document) => Collection.FindOneAndReplace(Builders<TDocument>.Filter.Eq(doc => doc.Id, document.Id), document);
		public virtual async Task ReplaceOneAsync(TDocument document) => await Collection.FindOneAndReplaceAsync(Builders<TDocument>.Filter.Eq(doc => doc.Id, document.Id), document);

		public void DeleteOne(Expression<Func<TDocument, bool>> filterExpression) => Collection.FindOneAndDelete(filterExpression);
		public Task DeleteOneAsync(Expression<Func<TDocument, bool>> filterExpression) => Task.Run(() => Collection.FindOneAndDeleteAsync(filterExpression));

		public void DeleteById(TKey id) => Collection.FindOneAndDelete(Builders<TDocument>.Filter.Eq(doc => doc.Id, id));
		public Task DeleteByIdAsync(TKey id) => Task.Run(() => Collection.FindOneAndDeleteAsync(Builders<TDocument>.Filter.Eq(doc => doc.Id, id)));

		public void DeleteMany(Expression<Func<TDocument, bool>> filterExpression) => Collection.DeleteMany(filterExpression);
		public Task DeleteManyAsync(Expression<Func<TDocument, bool>> filterExpression) => Task.Run(() => Collection.DeleteManyAsync(filterExpression));
	}
}
