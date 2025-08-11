using MediatR;
using MG.Models.Entities;
using MG.Services.Interfaces;
using FluentValidation;
using AutoMapper;
using MG.Models.DTOs.Data;

namespace MG.Api.Features.Data;

public static class UpdateData {
	public record Command(string Id, string Value) : IRequest<DataResponse?>;

	public class Handler(IStorageFactory storageFactory, ILogger<Handler> logger, IMapper mapper) : IRequestHandler<Command, DataResponse?> {

		public async Task<DataResponse?> Handle(Command request, CancellationToken cancellationToken) {
			logger.LogInformation("Starting data update for ID: {Id} with new value: {Value}", request.Id, request.Value);
			
			var dataRepository = storageFactory.CreateDataRepository();
			var cacheService = storageFactory.CreateCacheService();
			var fileStorageService = storageFactory.CreateFileStorageService();

			// Step 1: Verify data exists
			var existingData = await dataRepository.GetByIdAsync(request.Id);
			if (existingData == null) {
				logger.LogWarning("Data with ID: {Id} not found for update", request.Id);
				return null;
			}

			// Step 2: Update the data entity
			var updatedEntity = new DataItem {
				Id = existingData.Id,
				Value = request.Value,
				CreatedAt = existingData.CreatedAt,
				UpdatedAt = DateTime.UtcNow
			};

			// Step 3: Update database and clear cache/file storage
			var updatedData = await dataRepository.UpdateAsync(updatedEntity);
			
			// Clear cache and file storage in parallel
			var cacheKey = $"data:{request.Id}";
			await Task.WhenAll(
				cacheService.RemoveAsync(cacheKey),
				fileStorageService.RemoveAsync(request.Id)
			);

			// Step 4: Build and return response
			var response = mapper.Map<DataResponse>(updatedData);
			logger.LogInformation("Data updated successfully for ID: {Id}", response.Id);
			return response;
		}
	}

	public class Validator : AbstractValidator<Command> {
		public Validator() {
			RuleFor(x => x.Id)
				.NotEmpty()
				.WithMessage("ID is required");
			RuleFor(x => x.Value)
				.NotEmpty()
				.WithMessage("Value is required")
				.MaximumLength(1000)
				.WithMessage("Value cannot exceed 1000 characters");
		}
	}
}
