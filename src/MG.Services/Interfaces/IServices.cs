using MG.Models.Entities;

namespace MG.Services.Interfaces;

public interface ICacheService {
	Task<T?> GetAsync<T>(string key);

	Task SetAsync<T>(string key,T value,TimeSpan expiration);

	Task RemoveAsync(string key);
}

public interface IFileStorageService {
	Task<T?> GetAsync<T>(string key);

	Task SetAsync<T>(string key,T value, TimeSpan expiration);

	Task<bool> IsValidAsync(string key);

	Task RemoveAsync(string key);
}

public interface IStorageFactory {
	ICacheService CreateCacheService();
	IFileStorageService CreateFileStorageService();
	IDataRepository CreateDataRepository();
	TimeSpan GetCacheTTL();
	TimeSpan GetFileStorageTTL();
}

public interface IAuthService {
	Task<string> GenerateJwtTokenAsync(User user);
	Task<User?> AuthenticateAsync(string email,string password);
	string HashPassword(string password);
	bool VerifyPassword(string password,string hash);
}
