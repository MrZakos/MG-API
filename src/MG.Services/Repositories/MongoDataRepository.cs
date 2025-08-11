using MongoDB.Driver;
using MG.Models.Entities;
using MG.Services.Interfaces;
using Microsoft.Extensions.Options;
using MG.Models.Options;

namespace MG.Services.Repositories;

public class MongoDataRepository : IDataRepository {
	
	private readonly IMongoCollection<DataItem> _collection;

	public MongoDataRepository(IMongoDatabase database,IOptions<MongoDbOptions> mongoOptions) {
		var databaseName = mongoOptions.Value.DatabaseName;
		var collectionName = mongoOptions.Value.Collections.DataItems;
		_collection = database.GetCollection<DataItem>(collectionName);
	}

	public async Task<DataItem?> GetByIdAsync(string id) {
		try {
			var filter = Builders<DataItem>.Filter.Eq(x => x.Id,id);
			return await _collection.Find(filter).FirstOrDefaultAsync();
		}
		catch {
			return null;
		}
	}

	public async Task<DataItem> CreateAsync(DataItem dataItem) {
		await _collection.InsertOneAsync(dataItem);
		return dataItem;
	}

	public async Task<DataItem?> UpdateAsync(DataItem dataItem) {
		try {
			dataItem.UpdatedAt = DateTime.UtcNow;
			var filter = Builders<DataItem>.Filter.Eq(x => x.Id,dataItem.Id);
			var result = await _collection.ReplaceOneAsync(filter,dataItem);
			return result.MatchedCount > 0 ? dataItem : null;
		}
		catch {
			return null;
		}
	}

	public async Task<bool> DeleteAsync(string id) {
		try {
			var filter = Builders<DataItem>.Filter.Eq(x => x.Id,id);
			var result = await _collection.DeleteOneAsync(filter);
			return result.DeletedCount > 0;
		}
		catch {
			return false;
		}
	}
}
