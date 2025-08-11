using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MG.Models.Options;
using MG.Services.Decorators;
using MG.Services.Factories;
using MG.Services.Interfaces;
using MG.Services.Repositories;
using MG.Services.Services;
using StackExchange.Redis;
using Azure.Storage.Blobs;
using MongoDB.Driver;
using AutoMapper;
using MediatR;
using FluentValidation;
using MG.Api.Behaviors;
using MG.Api.Mappings;

namespace MG.Tests.Unit.Infrastructure;

/// <summary>
/// Shared base class for integration tests that need real storage connections and MediatR setup.
/// Follows DRY principle by centralizing common infrastructure setup.
/// </summary>
public abstract class IntegrationTestBase : IDisposable
{
    protected readonly ServiceCollection Services;
    protected readonly ServiceProvider ServiceProvider;
    protected readonly StorageOptions StorageOptions;
    protected readonly MongoDbOptions MongoDbOptions;
    protected readonly IStorageFactory StorageFactory;
    protected readonly IMediator Mediator;
    protected readonly IMapper Mapper;
    protected readonly IConnectionMultiplexer RedisConnection;
    protected readonly BlobServiceClient BlobServiceClient;
    protected readonly IMongoClient MongoClient;

    protected IntegrationTestBase()
    {
        // Initialize options using shared configuration
        StorageOptions = TestFactoryConfiguration.CreateStorageOptions();
        MongoDbOptions = TestFactoryConfiguration.CreateMongoDbOptions();

        // Initialize real connections using shared connection strings
        RedisConnection = ConnectionMultiplexer.Connect(TestFactoryConfiguration.ConnectionStrings.Redis);
        BlobServiceClient = new BlobServiceClient(TestFactoryConfiguration.ConnectionStrings.AzureStorage);
        MongoClient = new MongoClient(TestFactoryConfiguration.ConnectionStrings.MongoDB);

        // Setup service collection
        Services = new ServiceCollection();
        RegisterServices();
        
        ServiceProvider = Services.BuildServiceProvider();
        
        // Get key services
        StorageFactory = ServiceProvider.GetRequiredService<IStorageFactory>();
        Mediator = ServiceProvider.GetRequiredService<IMediator>();
        Mapper = ServiceProvider.GetRequiredService<IMapper>();
    }

    private void RegisterServices()
    {
        // Register external connections
        Services.AddSingleton(RedisConnection);
        Services.AddSingleton(BlobServiceClient);
        Services.AddSingleton(MongoClient);
        Services.AddSingleton(MongoClient.GetDatabase(MongoDbOptions.DatabaseName));

        // Register options
        Services.AddSingleton(Options.Create(StorageOptions));
        Services.AddSingleton(Options.Create(MongoDbOptions));

        // Register concrete storage services
        Services.AddSingleton<RedisCacheService>();
        Services.AddSingleton<AzureBlobFileStorageService>();
        Services.AddSingleton<MongoDataRepository>();

        // Register storage factory
        Services.AddSingleton<IStorageFactory, StorageFactory>();

        // Register logging
        Services.AddLogging(builder => builder.AddConsole());
        RegisterLoggers();

        // Register AutoMapper with proper configuration for version 15.0.1
        Services.AddSingleton<IMapper>(provider =>
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<MappingProfile>();
            },new LoggerFactory());
            return config.CreateMapper();
        });

        // Register MediatR with behaviors (matching Program.cs exactly)
        Services.AddMediatR(cfg => 
        {
            cfg.RegisterServicesFromAssembly(typeof(MG.Api.Features.Data.GetData).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(MG.Models.Entities.DataItem).Assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(RetryBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        // Register validators
        Services.AddValidatorsFromAssembly(typeof(MG.Api.Features.Data.GetData).Assembly);
        Services.AddValidatorsFromAssembly(typeof(MG.Models.Entities.DataItem).Assembly);
        
        // Allow derived classes to register additional services
        RegisterAdditionalServices();
    }

    private void RegisterLoggers()
    {
        // Register logger-specific instances for decorators
        Services.AddSingleton<ILogger<LoggingCacheServiceDecorator>>(provider => 
            provider.GetRequiredService<ILoggerFactory>().CreateLogger<LoggingCacheServiceDecorator>());
        Services.AddSingleton<ILogger<LoggingFileStorageServiceDecorator>>(provider => 
            provider.GetRequiredService<ILoggerFactory>().CreateLogger<LoggingFileStorageServiceDecorator>());
        Services.AddSingleton<ILogger<LoggingDataRepositoryDecorator>>(provider => 
            provider.GetRequiredService<ILoggerFactory>().CreateLogger<LoggingDataRepositoryDecorator>());

        // Register loggers for concrete services
        Services.AddSingleton<ILogger<RedisCacheService>>(provider => 
            provider.GetRequiredService<ILoggerFactory>().CreateLogger<RedisCacheService>());
        Services.AddSingleton<ILogger<AzureBlobFileStorageService>>(provider => 
            provider.GetRequiredService<ILoggerFactory>().CreateLogger<AzureBlobFileStorageService>());
        Services.AddSingleton<ILogger<MongoDataRepository>>(provider => 
            provider.GetRequiredService<ILoggerFactory>().CreateLogger<MongoDataRepository>());
    }

    /// <summary>
    /// Override this method in derived classes to register additional services specific to the test
    /// </summary>
    protected virtual void RegisterAdditionalServices()
    {
        // Default implementation does nothing
    }

    /// <summary>
    /// Helper method to get a service from the container
    /// </summary>
    protected T GetService<T>() where T : notnull
    {
        return ServiceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Helper method to create a logger for the test class
    /// </summary>
    protected ILogger<T> CreateLogger<T>()
    {
        return ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<T>();
    }

    public void Dispose()
    {
        RedisConnection?.Dispose();
        ServiceProvider?.Dispose();
    }
}
