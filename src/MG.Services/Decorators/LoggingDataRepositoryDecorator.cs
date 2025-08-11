using MG.Models.Entities;
using MG.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace MG.Services.Decorators;

public class LoggingDataRepositoryDecorator(IDataRepository repository,ILogger<LoggingDataRepositoryDecorator> logger) : IDataRepository {

	public async Task<DataItem?> GetByIdAsync(string id) {
		var stopwatch = Stopwatch.StartNew();
		logger.LogInformation("Starting GetByIdAsync for ID: {Id}",id);
		try {
			var result = await repository.GetByIdAsync(id);
			stopwatch.Stop();
			if (result != null) {
				logger.LogInformation("Successfully retrieved data for ID: {Id} in {ElapsedMs}ms",id,stopwatch.ElapsedMilliseconds);
			}
			else {
				logger.LogInformation("No data found for ID: {Id} in {ElapsedMs}ms",id,stopwatch.ElapsedMilliseconds);
			}
			return result;
		}
		catch (Exception ex) {
			stopwatch.Stop();
			logger.LogError(ex,
							"Error retrieving data for ID: {Id} after {ElapsedMs}ms",
							id,
							stopwatch.ElapsedMilliseconds);
			throw;
		}
	}

	public async Task<DataItem> CreateAsync(DataItem dataItem) {
		var stopwatch = Stopwatch.StartNew();
		logger.LogInformation("Starting CreateAsync for data with value: {Value}",dataItem.Value);
		try {
			var result = await repository.CreateAsync(dataItem);
			stopwatch.Stop();
			logger.LogInformation("Successfully created data with ID: {Id} in {ElapsedMs}ms",result.Id,stopwatch.ElapsedMilliseconds);
			return result;
		}
		catch (Exception ex) {
			stopwatch.Stop();
			logger.LogError(ex,"Error creating data after {ElapsedMs}ms",stopwatch.ElapsedMilliseconds);
			throw;
		}
	}

	public async Task<DataItem?> UpdateAsync(DataItem dataItem) {
		var stopwatch = Stopwatch.StartNew();
		logger.LogInformation("Starting UpdateAsync for ID: {Id}",dataItem.Id);
		try {
			var result = await repository.UpdateAsync(dataItem);
			stopwatch.Stop();
			if (result != null) {
				logger.LogInformation("Successfully updated data for ID: {Id} in {ElapsedMs}ms",dataItem.Id,stopwatch.ElapsedMilliseconds);
			}
			else {
				logger.LogWarning("Update failed - data not found for ID: {Id} in {ElapsedMs}ms",dataItem.Id,stopwatch.ElapsedMilliseconds);
			}
			return result;
		}
		catch (Exception ex) {
			stopwatch.Stop();
			logger.LogError(ex,
							"Error updating data for ID: {Id} after {ElapsedMs}ms",
							dataItem.Id,
							stopwatch.ElapsedMilliseconds);
			throw;
		}
	}

	public async Task<bool> DeleteAsync(string id) {
		var stopwatch = Stopwatch.StartNew();
		logger.LogInformation("Starting DeleteAsync for ID: {Id}",id);
		try {
			var result = await repository.DeleteAsync(id);
			stopwatch.Stop();
			if (result) {
				logger.LogInformation("Successfully deleted data for ID: {Id} in {ElapsedMs}ms",id,stopwatch.ElapsedMilliseconds);
			}
			else {
				logger.LogWarning("Delete failed - data not found for ID: {Id} in {ElapsedMs}ms",id,stopwatch.ElapsedMilliseconds);
			}
			return result;
		}
		catch (Exception ex) {
			stopwatch.Stop();
			logger.LogError(ex,
							"Error deleting data for ID: {Id} after {ElapsedMs}ms",
							id,
							stopwatch.ElapsedMilliseconds);
			throw;
		}
	}
}
