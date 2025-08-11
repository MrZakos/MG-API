using StackExchange.Redis;
using System.Text.Json;
using MG.Services.Interfaces;

namespace MG.Services.Services;

public class RedisCacheService(IConnectionMultiplexer redis) : ICacheService {

	private readonly IDatabase _database = redis.GetDatabase();

	public async Task<T?> GetAsync<T>(string key) {
		var value = await _database.StringGetAsync(key);
		if (!value.HasValue)
			return default;
		return JsonSerializer.Deserialize<T>(value!);
	}

	public async Task SetAsync<T>(string key,T value,TimeSpan expiration) {
		var serializedValue = JsonSerializer.Serialize(value);
		await _database.StringSetAsync(key,serializedValue,expiration);
	}

	public async Task RemoveAsync(string key) {
		await _database.KeyDeleteAsync(key);
	}
}
