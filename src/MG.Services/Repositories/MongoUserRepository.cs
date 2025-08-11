using MongoDB.Driver;
using MG.Models.Entities;
using MG.Services.Interfaces;
using Microsoft.Extensions.Options;
using MG.Models.Options;

namespace MG.Services.Repositories;

public class MongoUserRepository : IUserRepository {
	
	private readonly IMongoCollection<User> _collection;

	public MongoUserRepository(IMongoDatabase database,IOptions<MongoDbOptions> mongoOptions) {
		var collectionName = mongoOptions.Value.Collections.Users;
		_collection = database.GetCollection<User>(collectionName);
	}

	public async Task<User?> GetByEmailAsync(string email) {
		try {
			var filter = Builders<User>.Filter.Eq(x => x.Email,email);
			return await _collection.Find(filter).FirstOrDefaultAsync();
		}
		catch {
			return null;
		}
	}

	public async Task<User> CreateAsync(User user) {
		await _collection.InsertOneAsync(user);
		return user;
	}
}
