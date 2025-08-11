using MediatR;
using MG.Models.Entities;
using MG.Services.Interfaces;
using FluentValidation;
using AutoMapper;
using MG.Models.DTOs.Data;
using MongoDB.Bson;

namespace MG.Api.Features.Data;

public static class CreateData {
	public record Command(string Value) : IRequest<DataResponse>;

	public class Handler(IStorageFactory storageFactory, ILogger<Handler> logger, IMapper mapper) : IRequestHandler<Command, DataResponse> {

		public async Task<DataResponse> Handle(Command request, CancellationToken cancellationToken) {
			logger.LogInformation("Starting data creation with value: {Value}", request.Value);
			
			// Generate new ID and create entity
			var id = ObjectId.GenerateNewId().ToString();
			var dataEntity = new DataItem {
				Id = id,
				Value = request.Value,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow
			};

			// Save to database only
			var dataRepository = storageFactory.CreateDataRepository();
			var createdData = await dataRepository.CreateAsync(dataEntity);

			// Build and return response
			var response = mapper.Map<DataResponse>(createdData);
			logger.LogInformation("Data created successfully with ID: {Id}", response.Id);
			return response;
		}
	}

	public class Validator : AbstractValidator<Command> {
		public Validator() {
			RuleFor(x => x.Value)
				.NotEmpty()
				.WithMessage("Value is required")
				.MaximumLength(1000)
				.WithMessage("Value cannot exceed 1000 characters");
		}
	}
}
