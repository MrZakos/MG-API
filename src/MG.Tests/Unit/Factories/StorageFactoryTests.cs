using FluentAssertions;
using Xunit;
using MG.Tests.Unit.Infrastructure;
using MG.Models.DTOs.Data;
using MG.Models.Entities;
using MG.Services.Decorators;

namespace MG.Tests.Unit.Factories;

public class StorageFactoryTests : IntegrationTestBase
{
    [Fact]
    public void GetCacheTTL_ShouldReturnCorrectTimeSpan()
    {
        // Act
        var result = StorageFactory.GetCacheTTL();

        // Assert
        result.Should().Be(TimeSpan.FromMilliseconds(600000));
    }

    [Fact]
    public void GetFileStorageTTL_ShouldReturnCorrectTimeSpan()
    {
        // Act
        var result = StorageFactory.GetFileStorageTTL();

        // Assert
        result.Should().Be(TimeSpan.FromMilliseconds(1800000));
    }

    [Fact]
    public async Task CreateCacheService_WithLoggingEnabled_ShouldReturnDecoratedService()
    {
        // Act
        var result = StorageFactory.CreateCacheService();
        await result.SetAsync("test-key","test-value",TimeSpan.FromSeconds(10));

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<LoggingCacheServiceDecorator>();
    }
    
    [Fact]
    public async Task CreateFileStorageService_WithLoggingEnabled_ShouldReturnDecoratedService()
    {
        // Act
        var result = StorageFactory.CreateFileStorageService();
        var fileId = "test-file";
        result.SetAsync(fileId, "test-content", TimeSpan.FromSeconds(10)).Wait();
        var file = await result.GetAsync<DataResponse>("test4");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<LoggingFileStorageServiceDecorator>();
    }

    [Fact]
    public async Task CreateDataRepository_WithLoggingEnabled_ShouldReturnDecoratedService()
    {
        // Act
        var result = StorageFactory.CreateDataRepository();
        var newItem = new DataItem {
            Value = "test-value"
        };
        await result.CreateAsync(newItem);
        var savedData = await result.GetByIdAsync(newItem.Id);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<LoggingDataRepositoryDecorator>();
    }
}
