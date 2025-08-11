using MG.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace MG.Services.Decorators;

public class LoggingCacheServiceDecorator(ICacheService cacheService,ILogger<LoggingCacheServiceDecorator> logger) : ICacheService {

	public async Task<T?> GetAsync<T>(string key) {
		var stopwatch = Stopwatch.StartNew();
		logger.LogDebug("Cache GET attempt for key: {Key}",key);
		try {
			var result = await cacheService.GetAsync<T>(key);
			stopwatch.Stop();
			if (result != null) {
				logger.LogInformation("Cache HIT for key: {Key} in {ElapsedMs}ms",key,stopwatch.ElapsedMilliseconds);
			}
			else {
				logger.LogInformation("Cache MISS for key: {Key} in {ElapsedMs}ms",key,stopwatch.ElapsedMilliseconds);
			}
			return result;
		}
		catch (Exception ex) {
			stopwatch.Stop();
			logger.LogError(ex,
							"Cache GET error for key: {Key} after {ElapsedMs}ms",
							key,
							stopwatch.ElapsedMilliseconds);
			throw;
		}
	}

	public async Task SetAsync<T>(string key,T value,TimeSpan expiration) {
		var stopwatch = Stopwatch.StartNew();
		logger.LogDebug("Cache SET attempt for key: {Key} with expiration: {Expiration}",key,expiration);
		try {
			await cacheService.SetAsync(key,value,expiration);
			stopwatch.Stop();
			logger.LogInformation("Cache SET successful for key: {Key} in {ElapsedMs}ms",key,stopwatch.ElapsedMilliseconds);
		}
		catch (Exception ex) {
			stopwatch.Stop();
			logger.LogError(ex,
							"Cache SET error for key: {Key} after {ElapsedMs}ms",
							key,
							stopwatch.ElapsedMilliseconds);
			throw;
		}
	}

	public async Task RemoveAsync(string key) {
		var stopwatch = Stopwatch.StartNew();
		logger.LogDebug("Cache REMOVE attempt for key: {Key}",key);
		try {
			await cacheService.RemoveAsync(key);
			stopwatch.Stop();
			logger.LogInformation("Cache REMOVE successful for key: {Key} in {ElapsedMs}ms",key,stopwatch.ElapsedMilliseconds);
		}
		catch (Exception ex) {
			stopwatch.Stop();
			logger.LogError(ex,
							"Cache REMOVE error for key: {Key} after {ElapsedMs}ms",
							key,
							stopwatch.ElapsedMilliseconds);
			throw;
		}
	}
}
