using MongoDB.Bson;
using MongoDB.Driver;
using Nodsoft.YumeChan.Core.Config;
using Nodsoft.YumeChan.PluginBase.Tools.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Nodsoft.YumeChan.Core.Tools
{
	public class EntityRepository<TDocument> : IEntityRepository<TDocument> where TDocument : IDocument
	{
		private readonly IMongoCollection<TDocument> collection;

		public EntityRepository(ICoreDatabaseProperties settings)
		{
			IMongoDatabase database = new MongoClient(settings.ConnectionString).GetDatabase(settings.DatabaseName);
			collection = database.GetCollection<TDocument>(GetCollectionName(typeof(TDocument)) ?? typeof(TDocument).Name);
		}

		private protected string GetCollectionName(Type documentType)
		{
			return ((BsonCollectionAttribute)documentType.GetCustomAttributes(
					typeof(BsonCollectionAttribute),
					true)
				.FirstOrDefault())?.CollectionName;
		}

		public virtual IQueryable<TDocument> AsQueryable()
		{
			return collection.AsQueryable();
		}

		public virtual IEnumerable<TDocument> FilterBy(
			Expression<Func<TDocument, bool>> filterExpression)
		{
			return collection.Find(filterExpression).ToEnumerable();
		}

		public virtual IEnumerable<TProjected> FilterBy<TProjected>(
			Expression<Func<TDocument, bool>> filterExpression,
			Expression<Func<TDocument, TProjected>> projectionExpression)
		{
			return collection.Find(filterExpression).Project(projectionExpression).ToEnumerable();
		}

		public virtual TDocument FindOne(Expression<Func<TDocument, bool>> filterExpression)
		{
			return collection.Find(filterExpression).FirstOrDefault();
		}

		public virtual Task<TDocument> FindOneAsync(Expression<Func<TDocument, bool>> filterExpression)
		{
			return Task.Run(() => collection.Find(filterExpression).FirstOrDefaultAsync());
		}

		public virtual TDocument FindById(string id)
		{
			var objectId = new ObjectId(id);
			var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, objectId);
			return collection.Find(filter).SingleOrDefault();
		}

		public virtual Task<TDocument> FindByIdAsync(string id)
		{
			return Task.Run(() =>
			{
				var objectId = new ObjectId(id);
				var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, objectId);
				return collection.Find(filter).SingleOrDefaultAsync();
			});
		}


		public virtual void InsertOne(TDocument document)
		{
			collection.InsertOne(document);
		}

		public virtual Task InsertOneAsync(TDocument document)
		{
			return Task.Run(() => collection.InsertOneAsync(document));
		}

		public void InsertMany(ICollection<TDocument> documents)
		{
			collection.InsertMany(documents);
		}


		public virtual async Task InsertManyAsync(ICollection<TDocument> documents)
		{
			await collection.InsertManyAsync(documents);
		}

		public void ReplaceOne(TDocument document)
		{
			var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, document.Id);
			collection.FindOneAndReplace(filter, document);
		}

		public virtual async Task ReplaceOneAsync(TDocument document)
		{
			var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, document.Id);
			await collection.FindOneAndReplaceAsync(filter, document);
		}

		public void DeleteOne(Expression<Func<TDocument, bool>> filterExpression)
		{
			collection.FindOneAndDelete(filterExpression);
		}

		public Task DeleteOneAsync(Expression<Func<TDocument, bool>> filterExpression)
		{
			return Task.Run(() => collection.FindOneAndDeleteAsync(filterExpression));
		}

		public void DeleteById(string id)
		{
			var objectId = new ObjectId(id);
			var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, objectId);
			collection.FindOneAndDelete(filter);
		}

		public Task DeleteByIdAsync(string id)
		{
			return Task.Run(() =>
			{
				var objectId = new ObjectId(id);
				var filter = Builders<TDocument>.Filter.Eq(doc => doc.Id, objectId);
				collection.FindOneAndDeleteAsync(filter);
			});
		}

		public void DeleteMany(Expression<Func<TDocument, bool>> filterExpression)
		{
			collection.DeleteMany(filterExpression);
		}

		public Task DeleteManyAsync(Expression<Func<TDocument, bool>> filterExpression)
		{
			return Task.Run(() => collection.DeleteManyAsync(filterExpression));
		}
	}
}
