using MG.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace MG.Services.Decorators;

public class LoggingFileStorageServiceDecorator(IFileStorageService fileStorageService,ILogger<LoggingFileStorageServiceDecorator> logger) : IFileStorageService {

	public async Task<T?> GetAsync<T>(string key) {
		var stopwatch = Stopwatch.StartNew();
		logger.LogDebug("File storage GET attempt for key: {Key}",key);
		try {
			var result = await fileStorageService.GetAsync<T>(key);
			stopwatch.Stop();
			if (result != null) {
				logger.LogInformation("File storage HIT for key: {Key} in {ElapsedMs}ms",key,stopwatch.ElapsedMilliseconds);
			}
			else {
				logger.LogInformation("File storage MISS for key: {Key} in {ElapsedMs}ms",key,stopwatch.ElapsedMilliseconds);
			}
			return result;
		}
		catch (Exception ex) {
			stopwatch.Stop();
			logger.LogError(ex,
							"File storage GET error for key: {Key} after {ElapsedMs}ms",
							key,
							stopwatch.ElapsedMilliseconds);
			throw;
		}
	}

	public async Task SetAsync<T>(string key,T value,TimeSpan expiration) {
		var stopwatch = Stopwatch.StartNew();
		logger.LogDebug("File storage SET attempt for key: {Key} with expiration: {Expiration}",key,expiration);
		try {
			await fileStorageService.SetAsync(key,value,expiration);
			stopwatch.Stop();
			logger.LogInformation("File storage SET successful for key: {Key} in {ElapsedMs}ms",key,stopwatch.ElapsedMilliseconds);
		}
		catch (Exception ex) {
			stopwatch.Stop();
			logger.LogError(ex,
							"File storage SET error for key: {Key} after {ElapsedMs}ms",
							key,
							stopwatch.ElapsedMilliseconds);
			throw;
		}
	}

	public async Task<bool> IsValidAsync(string key) {
		var stopwatch = Stopwatch.StartNew();
		logger.LogDebug("File storage VALIDATION attempt for key: {Key}",key);
		try {
			var result = await fileStorageService.IsValidAsync(key);
			stopwatch.Stop();
			logger.LogDebug("File storage VALIDATION for key: {Key} returned {IsValid} in {ElapsedMs}ms",
							key,
							result,
							stopwatch.ElapsedMilliseconds);
			return result;
		}
		catch (Exception ex) {
			stopwatch.Stop();
			logger.LogError(ex,
							"File storage VALIDATION error for key: {Key} after {ElapsedMs}ms",
							key,
							stopwatch.ElapsedMilliseconds);
			throw;
		}
	}

	public async Task RemoveAsync(string key) {
		var stopwatch = Stopwatch.StartNew();
		logger.LogDebug("File storage REMOVE attempt for key: {Key}", key);
		try {
			await fileStorageService.RemoveAsync(key);
			stopwatch.Stop();
			logger.LogInformation("File storage REMOVE successful for key: {Key} in {ElapsedMs}ms", key, stopwatch.ElapsedMilliseconds);
		}
		catch (Exception ex) {
			stopwatch.Stop();
			logger.LogError(ex,
							"File storage REMOVE error for key: {Key} after {ElapsedMs}ms",
							key,
							stopwatch.ElapsedMilliseconds);
			throw;
		}
	}
}
