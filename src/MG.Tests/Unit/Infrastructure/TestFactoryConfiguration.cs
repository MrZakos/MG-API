using MG.Models.Options;

namespace MG.Tests.Unit.Infrastructure;

/// <summary>
/// Shared test configuration for storage factory settings.
/// Centralizes all factory-related options to ensure consistency across tests.
/// </summary>
public static class TestFactoryConfiguration
{
    /// <summary>
    /// Creates the standard StorageOptions configuration used across all tests
    /// </summary>
    public static StorageOptions CreateStorageOptions()
    {
        return new StorageOptions
        {
            EnableLogging = true,
            TTL = new TtlOptions
            {
                CacheMilliseconds = 600000,
                FileStorageMilliseconds = 1800000
            },
            AzureBlob = new AzureBlobOptions
            {
                ContainerName = "files"
            }
        };
    }

    /// <summary>
    /// Creates the standard MongoDbOptions configuration used across all tests
    /// </summary>
    public static MongoDbOptions CreateMongoDbOptions()
    {
        return new MongoDbOptions
        {
            DatabaseName = "mongodb",
            Collections = new CollectionsOptions
            {
                DataItems = "DataItems",
                Users = "Users"
            }
        };
    }

    /// <summary>
    /// Connection string configurations that match Docker Compose setup
    /// </summary>
    public static class ConnectionStrings
    {
        public const string Redis = "localhost:6379,password=redis_password_123";
        public const string AzureStorage = "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://localhost:10000/devstoreaccount1;";
        public const string MongoDB = "mongodb://admin:mongo_password_123@localhost:27017/mongodb?authSource=admin&authMechanism=SCRAM-SHA-256";
    }
}
