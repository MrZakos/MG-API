using MG.Services.Interfaces;
using MG.Services.Services;
using MG.Services.Repositories;
using MG.Services.Decorators;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using MG.Models.Options;

namespace MG.Services.Factories;

public class StorageFactory(IServiceProvider serviceProvider, IOptions<StorageOptions> storageOptions) : IStorageFactory {

	private readonly StorageOptions _storageOptions = storageOptions.Value;

	public TimeSpan GetCacheTTL() {
		return TimeSpan.FromMilliseconds(_storageOptions.TTL.CacheMilliseconds);
	}

	public TimeSpan GetFileStorageTTL() {
		return TimeSpan.FromMilliseconds(_storageOptions.TTL.FileStorageMilliseconds);
	}

	// Abstract Factory approach - each method creates a family of related objects
	public ICacheService CreateCacheService() {
		var service = serviceProvider.GetRequiredService<RedisCacheService>();
		return ApplyLoggingDecorator<ICacheService, LoggingCacheServiceDecorator>(service);
	}

	public IFileStorageService CreateFileStorageService() {
		var service = serviceProvider.GetRequiredService<AzureBlobFileStorageService>();
		return ApplyLoggingDecorator<IFileStorageService, LoggingFileStorageServiceDecorator>(service);
	}

	public IDataRepository CreateDataRepository() {
		var service = serviceProvider.GetRequiredService<MongoDataRepository>();
		return ApplyLoggingDecorator<IDataRepository, LoggingDataRepositoryDecorator>(service);
	}

	// Extract common decorator logic to follow DRY principle
	private T ApplyLoggingDecorator<T, TDecorator>(T service) 
		where T : class 
		where TDecorator : class, T {
		
		if (!_storageOptions.EnableLogging) {
			return service;
		}

		var logger = serviceProvider.GetRequiredService<ILogger<TDecorator>>();
		return (T)Activator.CreateInstance(typeof(TDecorator), service, logger)!;
	}
}
