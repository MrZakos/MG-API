using MediatR;
using MG.Services.Interfaces;
using FluentValidation;
using MG.Models.DTOs.Data;

namespace MG.Api.Features.Data;

public static class GetData {
	public record Query(string Id) : IRequest<DataResponse?>;

	public class Handler(IStorageFactory storageFactory,ILogger<Handler> logger) : IRequestHandler<Query,DataResponse?> {

		public async Task<DataResponse?> Handle(Query request,CancellationToken cancellationToken) {
			logger.LogInformation("Starting data retrieval for ID: {Id}",request.Id);
			
			// Create context once and share across all local functions
			var context = new DataRetrievalContext {
				Id = request.Id,
				CacheKey = $"data:{request.Id}",
				FileKey = request.Id,
				CacheService = storageFactory.CreateCacheService(),
				FileStorageService = storageFactory.CreateFileStorageService(),
				DataRepository = storageFactory.CreateDataRepository(),
				CacheTTL = storageFactory.GetCacheTTL(),
				FileStorageTTL = storageFactory.GetFileStorageTTL()
			};

			// Step 1: Try to find data in storage layers (cache -> file -> database)
			await FindDataInStorageLayers();

			// Step 2: If data found, orchestrate caching strategy based on source
			if (context.ResponseData != null) {
				await OrchestrateCachingStrategy();
				logger.LogInformation("Data found in {Source} for ID: {Id}",context.DataSource,request.Id);
				return context.ResponseData;
			}
			
			logger.LogInformation("Data not found anywhere for ID: {Id}",request.Id);
			return null;

			// Local functions
			async Task FindDataInStorageLayers() {
				// Try Cache first
				await TryGetFromCache();
				if (context.ResponseData != null) {
					context.DataSource = DataSource.Cache;
					return;
				}

				// Try File Storage
				await TryGetFromFileStorage();
				if (context.ResponseData != null) {
					context.DataSource = DataSource.FileStorage;
					return;
				}

				// Try Database
				await TryGetFromDatabase();
				if (context.ResponseData != null) {
					context.DataSource = DataSource.Database;
					return;
				}
				context.DataSource = DataSource.NotFound;
			}

			async Task TryGetFromCache() {
				logger.LogInformation("Checking cache for data with ID: {Id}", context.Id);
				var data = await context.CacheService.GetAsync<DataResponse>(context.CacheKey);
				if (data != null) {
					context.ResponseData = data;
				}
			}

			async Task TryGetFromFileStorage() {
				logger.LogInformation("Checking file storage for data with ID: {Id}", context.Id);
				var data = await context.FileStorageService.GetAsync<DataResponse>(context.Id);
				if (data != null) {
					context.ResponseData = data;
				}
			}

			async Task TryGetFromDatabase() {
				logger.LogInformation("Checking database for data with ID: {Id}", context.Id);
				var dbData = await context.DataRepository.GetByIdAsync(context.Id);
				if (dbData != null) {
					context.ResponseData = new DataResponse {
						Id = dbData.Id,
						Value = dbData.Value,
						CreatedAt = dbData.CreatedAt,
						UpdatedAt = dbData.UpdatedAt
					};
				}
			}

			Task OrchestrateCachingStrategy() {
				return context.DataSource switch {
					DataSource.Cache => Task.CompletedTask,
					DataSource.FileStorage => SaveToCache(),
					DataSource.Database => Task.WhenAll(SaveToCache(), SaveToFileStorage()),
					_ => Task.CompletedTask
				};
			}

			Task SaveToCache() {
				return context.CacheService.SetAsync(context.CacheKey,context.ResponseData!,context.CacheTTL);
			}

			Task SaveToFileStorage() {
				return context.FileStorageService.SetAsync(context.FileKey,context.ResponseData!,context.FileStorageTTL);
			}
		}

		public class Validator : AbstractValidator<Query> {
			public Validator() {
				RuleFor(x => x.Id).NotEmpty().WithMessage("ID is required");
			}
		}

		// Supporting types for better organization
		private record DataRetrievalContext {
			public required string Id { get; init; }
			public required string CacheKey { get; init; }
			public required string FileKey { get; init; }
			public required ICacheService CacheService { get; init; }
			public required IFileStorageService FileStorageService { get; init; }
			public required IDataRepository DataRepository { get; init; }
			public required TimeSpan CacheTTL { get; init; }
			public required TimeSpan FileStorageTTL { get; init; }
			public DataResponse? ResponseData { get; set; }
			public DataSource DataSource { get; set; }
		}

		private enum DataSource {
			Cache,
			FileStorage,
			Database,
			NotFound
		}
	}
}
