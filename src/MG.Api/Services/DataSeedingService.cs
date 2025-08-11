using MG.Models.Entities;
using MongoDB.Bson;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Driver;

namespace MG.Api.Services;

public class DataSeedingService(IServiceProvider serviceProvider, ILogger<DataSeedingService> logger) : IHostedService {

	public async Task StartAsync(CancellationToken cancellationToken) {
		try {
			logger.LogInformation("Starting data seeding process...");
			
			// Wait for MongoDB to be healthy before proceeding
			await WaitForMongoDbHealth(cancellationToken);
			
			// Use Aspire-provided MongoDB services
			await SeedDataUsingAspireServices(cancellationToken);
			
			logger.LogInformation("Data seeding completed successfully.");
		}
		catch (Exception ex) {
			logger.LogError(ex, "Error occurred while seeding data. Data seeding will be skipped.");
			// Don't throw - let the application continue without seeded data
		}
	}

	public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

	private async Task WaitForMongoDbHealth(CancellationToken cancellationToken) {
		using var scope = serviceProvider.CreateScope();
		var healthCheckService = scope.ServiceProvider.GetRequiredService<HealthCheckService>();
		
		var maxRetries = 1;
		var delay = TimeSpan.FromSeconds(1);
		
		for (var attempt = 1; attempt <= maxRetries; attempt++) {
			try {
				var healthResult = await healthCheckService.CheckHealthAsync(cancellationToken);
				var mongoHealthCheck = healthResult.Entries.FirstOrDefault(e => e.Key.Contains("mongodb", StringComparison.OrdinalIgnoreCase));
				
				if (mongoHealthCheck.Value.Status == HealthStatus.Healthy) {
					logger.LogInformation("MongoDB is healthy. Proceeding with data seeding.");
					return;
				}
				
				logger.LogInformation("MongoDB not ready yet (attempt {Attempt}/{MaxRetries}). Waiting...", attempt, maxRetries);
				await Task.Delay(delay, cancellationToken);
			}
			catch (Exception ex) {
				logger.LogWarning(ex, "Health check failed (attempt {Attempt}/{MaxRetries})", attempt, maxRetries);
				if (attempt == maxRetries) {
					throw new InvalidOperationException("MongoDB health check failed after maximum retries", ex);
				}
				await Task.Delay(delay, cancellationToken);
			}
		}
		
		throw new TimeoutException("MongoDB did not become healthy within the expected time");
	}

	private async Task SeedDataUsingAspireServices(CancellationToken cancellationToken) {
		using var scope = serviceProvider.CreateScope();
		
		try {
			// Use the properly configured MongoDB database from Aspire
			var database = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
			
			// Seed users and sample data
			await Task.WhenAll(
				SeedUsers(database, cancellationToken)
				//SeedSampleData(database, cancellationToken)
			);
		}
		catch (Exception ex) {
			logger.LogError(ex, "Failed to seed data using Aspire services");
			throw;
		}
	}

	private async Task SeedUsers(IMongoDatabase database, CancellationToken cancellationToken) {
		try {
			var usersCollection = database.GetCollection<User>("Users");

			// Check if admin user already exists
			var adminEmail = "admin@example.com";
			var existingAdmin = await usersCollection
				.Find(u => u.Email == adminEmail)
				.FirstOrDefaultAsync(cancellationToken);

			if (existingAdmin == null) {
				var adminUser = new User {
					Id = ObjectId.GenerateNewId().ToString(),
					Email = adminEmail,
					PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
					Role = "Admin",
					CreatedAt = DateTime.UtcNow,
					UpdatedAt = DateTime.UtcNow
				};

				await usersCollection.InsertOneAsync(adminUser, cancellationToken: cancellationToken);
				logger.LogInformation("Admin user seeded successfully");
			}
			else {
				logger.LogInformation("Admin user already exists, skipping seed");
			}

			// Check if regular user already exists
			var userEmail = "user@example.com";
			var existingUser = await usersCollection
				.Find(u => u.Email == userEmail)
				.FirstOrDefaultAsync(cancellationToken);

			if (existingUser == null) {
				var regularUser = new User {
					Id = ObjectId.GenerateNewId().ToString(),
					Email = userEmail,
					PasswordHash = BCrypt.Net.BCrypt.HashPassword("user123"),
					Role = "User",
					CreatedAt = DateTime.UtcNow,
					UpdatedAt = DateTime.UtcNow
				};

				await usersCollection.InsertOneAsync(regularUser, cancellationToken: cancellationToken);
				logger.LogInformation("Regular user seeded successfully");
			}
			else {
				logger.LogInformation("Regular user already exists, skipping seed");
			}
		}
		catch (Exception ex) {
			logger.LogError(ex, "Failed to seed users");
			throw;
		}
	}

	private async Task SeedSampleData(IMongoDatabase database, CancellationToken cancellationToken) {
		try {
			var dataCollection = database.GetCollection<DataItem>("DataItems");

			// Check if sample data already exists
			var existingCount = await dataCollection.CountDocumentsAsync(FilterDefinition<DataItem>.Empty, cancellationToken: cancellationToken);
			
			if (existingCount == 0) {
				var sampleData = new List<DataItem> {
					new() {
						Value = "Sample data item 1",
						CreatedAt = DateTime.UtcNow.AddDays(-5),
						UpdatedAt = DateTime.UtcNow.AddDays(-2)
					},
					new() {
						Value = "Sample data item 2",
						CreatedAt = DateTime.UtcNow.AddDays(-3),
						UpdatedAt = DateTime.UtcNow.AddDays(-1)
					},
					new() {
						Value = "Sample data item 3",
						CreatedAt = DateTime.UtcNow.AddDays(-1),
						UpdatedAt = DateTime.UtcNow
					}
				};

				await dataCollection.InsertManyAsync(sampleData, cancellationToken: cancellationToken);
				logger.LogInformation("Sample data seeded successfully ({Count} items)", sampleData.Count);
			}
			else {
				logger.LogInformation("Sample data already exists ({Count} items), skipping seed", existingCount);
			}
		}
		catch (Exception ex) {
			logger.LogError(ex, "Failed to seed sample data");
			throw;
		}
	}
}
