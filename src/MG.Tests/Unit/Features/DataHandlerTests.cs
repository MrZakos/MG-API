using FluentAssertions;
using Xunit;
using MG.Tests.Unit.Infrastructure;
using MG.Api.Features.Data;
using MG.Models.DTOs.Data;
using MG.Models.Entities;
using Microsoft.Extensions.Logging;

namespace MG.Tests.Unit.Features;

/// <summary>
/// Integration tests for Data MediatR handlers.
/// Tests each endpoint command/query through the actual MediatR pipeline with real storage connections.
/// </summary>
public class DataHandlerTests : IntegrationTestBase
{
    private readonly ILogger<DataHandlerTests> _logger;

    public DataHandlerTests()
    {
        _logger = CreateLogger<DataHandlerTests>();
    }

    [Fact]
    public async Task GetData_WithValidId_ShouldReturnDataResponse()
    {
        // Arrange
        var testValue = "Test data for get operation";
        var createCommand = new CreateData.Command(testValue);
        
        // First create some test data
        var createdData = await Mediator.Send(createCommand);
        createdData.Should().NotBeNull();
        
        var query = new GetData.Query(createdData.Id);

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(createdData.Id);
        result.Value.Should().Be(testValue);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        
        _logger.LogInformation("GetData test completed successfully for ID: {Id}", createdData.Id);
    }

    [Fact]
    public async Task GetData_WithNonExistentId_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = "6898ae985b617f4eb33b8713"; // Valid ObjectId format but doesn't exist
        var query = new GetData.Query(nonExistentId);

        // Act
        var result = await Mediator.Send(query);

        // Assert
        result.Should().BeNull();
        
        _logger.LogInformation("GetData test with non-existent ID completed successfully");
    }

    [Fact]
    public async Task CreateData_WithValidValue_ShouldReturnCreatedDataResponse()
    {
        // Arrange
        var testValue = "Test data for create operation";
        var command = new CreateData.Command(testValue);

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeNullOrEmpty();
        result.Value.Should().Be(testValue);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        
        // Verify data was actually persisted by retrieving it
        var getQuery = new GetData.Query(result.Id);
        var retrievedData = await Mediator.Send(getQuery);
        retrievedData.Should().NotBeNull();
        retrievedData!.Value.Should().Be(testValue);
        
        _logger.LogInformation("CreateData test completed successfully for ID: {Id}", result.Id);
    }

    [Fact]
    public async Task CreateData_WithEmptyValue_ShouldStillCreateData()
    {
        // Arrange
        var emptyValue = "";
        var command = new CreateData.Command(emptyValue);

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeNullOrEmpty();
        result.Value.Should().Be(emptyValue);
        
        _logger.LogInformation("CreateData with empty value test completed successfully");
    }

    [Fact]
    public async Task UpdateData_WithValidIdAndValue_ShouldReturnUpdatedDataResponse()
    {
        // Arrange
        var originalValue = "Original test data";
        var updatedValue = "Updated test data";
        
        // First create some test data
        var createCommand = new CreateData.Command(originalValue);
        var createdData = await Mediator.Send(createCommand);
        
        var updateCommand = new UpdateData.Command(createdData.Id, updatedValue);

        // Act
        var result = await Mediator.Send(updateCommand);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(createdData.Id);
        result.Value.Should().Be(updatedValue);
        result.CreatedAt.Should().Be(createdData.CreatedAt); // Should preserve original creation time
        
        // Verify the update was persisted
        var getQuery = new GetData.Query(createdData.Id);
        var retrievedData = await Mediator.Send(getQuery);
        retrievedData.Should().NotBeNull();
        retrievedData!.Value.Should().Be(updatedValue);
        
        _logger.LogInformation("UpdateData test completed successfully for ID: {Id}", createdData.Id);
    }

    [Fact]
    public async Task UpdateData_WithNonExistentId_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = "507f1f77bcf86cd799439011"; // Valid ObjectId format but doesn't exist
        var updateValue = "Updated value for non-existent data";
        var command = new UpdateData.Command(nonExistentId, updateValue);

        // Act
        var result = await Mediator.Send(command);

        // Assert
        result.Should().BeNull();
        
        _logger.LogInformation("UpdateData with non-existent ID test completed successfully");
    }

    [Fact]
    public async Task CreateData_MultipleItems_ShouldCreateUniqueIds()
    {
        // Arrange
        var values = new[] { "First item", "Second item", "Third item" };
        var commands = values.Select(v => new CreateData.Command(v)).ToArray();

        // Act
        var results = new List<DataResponse>();
        foreach (var command in commands)
        {
            var result = await Mediator.Send(command);
            results.Add(result);
        }

        // Assert
        results.Should().HaveCount(3);
        results.Select(r => r.Id).Should().OnlyHaveUniqueItems();
        
        for (var i = 0; i < values.Length; i++)
        {
            results[i].Value.Should().Be(values[i]);
        }
        
        _logger.LogInformation("Multiple CreateData operations test completed successfully");
    }
}
