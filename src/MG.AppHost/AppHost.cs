var builder = DistributedApplication.CreateBuilder(args);

builder.AddDockerComposeEnvironment("docker-compose-app")
	   .WithDashboard(dashboard => dashboard.WithHostPort(8080));

var cache = builder.AddRedis("cache")
				   .WithDataVolume(isReadOnly: false)
				   .WithRedisInsight();

var storage = builder.AddAzureStorage("storage")
					 .RunAsEmulator(azurite => {
						 azurite.WithBlobPort(10000)
								.WithQueuePort(10001)
								.WithTablePort(10002);
					 })
					 .AddBlobs("blobs");

var mongo = builder.AddMongoDB("mongo")
                   .WithLifetime(ContainerLifetime.Persistent)
                   .WithMongoExpress();

var mongodb = mongo.AddDatabase("mongodb");

var apiService = builder.AddProject<Projects.MG_Api>("api")
						.WithReference(cache)
						.WithReference(storage)
						.WithReference(mongodb)
						.WaitFor(cache)
						.WaitFor(storage)
						.WaitFor(mongo);

try
{
	builder.Build().Run();
}
catch (TaskCanceledException)
{
	// Expected during testing scenarios when the application is shut down
}
