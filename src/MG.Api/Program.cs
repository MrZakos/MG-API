using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MG.Services.Interfaces;
using MG.Services.Services;
using MG.Services.Repositories;
using MG.Services.Factories;
using MG.Api.Mappings;
using MG.Api.Behaviors;
using MG.Api.Services;
using MG.Models.Options;
using FluentValidation;
using MediatR;
using Microsoft.OpenApi.Models;
using MG.Api.Features.Authentication;
using MG.Api.Features.Data;
using MG.Services.Aspire;
using MG.Api.Middleware;
using Asp.Versioning;
using MG.Api.Features.Data.V1;
using MG.Api.Features.Data.V2;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations
builder.AddServiceDefaults();

// Add Aspire integrations with retry policies
builder.AddRedisClient("cache");
builder.AddAzureBlobServiceClient("blobs");
builder.AddMongoDBClient("mongodb");

// Register MongoDB Database for dependency injection
builder.Services.AddSingleton<MongoDB.Driver.IMongoDatabase>(provider =>
{
    var mongoClient = provider.GetRequiredService<MongoDB.Driver.IMongoClient>();
    var mongoOptions = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<MongoDbOptions>>().Value;
    return mongoClient.GetDatabase(mongoOptions.DatabaseName);
});

// Configure Options
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection(StorageOptions.SectionName));
builder.Services.Configure<MongoDbOptions>(builder.Configuration.GetSection(MongoDbOptions.SectionName));

// Add services to the container
builder.Services.AddProblemDetails();

// Add API Versioning
builder.Services.AddApiVersioning(opt =>
{
    opt.DefaultApiVersion = new ApiVersion(1, 0);
    opt.AssumeDefaultVersionWhenUnspecified = true;
    opt.ApiVersionReader = new UrlSegmentApiVersionReader();
}).AddApiExplorer(setup =>
{
    setup.GroupNameFormat = "'v'VVV";
    setup.SubstituteApiVersionInUrl = true;
});

// Add CORS
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:3000", "https://localhost:3001"];

builder.Services.AddCors(options => {
	options.AddPolicy("AllowSpecificOrigins",
					  policy => {
						  policy.WithOrigins(corsOrigins)
								 .AllowAnyHeader()
								 .AllowAnyMethod()
								 .AllowCredentials();
					  });
});

// Add Authentication & Authorization
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
	?? throw new InvalidOperationException("JWT configuration not found");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	   .AddJwtBearer(options => {
		   options.TokenValidationParameters = new TokenValidationParameters {
			   ValidateIssuer = true,
			   ValidateAudience = true,
			   ValidateLifetime = true,
			   ValidateIssuerSigningKey = true,
			   ValidIssuer = jwtOptions.Issuer,
			   ValidAudience = jwtOptions.Audience,
			   IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key))
		   };
	   });
builder.Services.AddAuthorization(options => {
	options.AddPolicy("AdminOnly",policy => policy.RequireRole("Admin"));
	options.AddPolicy("UserOrAdmin",policy => policy.RequireRole("User","Admin"));
});

// Add MediatR with validation and retry behaviors
builder.Services.AddMediatR(cfg => {
	cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
	cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(RetryBehavior<,>));
	cfg.AddBehavior(typeof(IPipelineBehavior<,>),typeof(ValidationBehavior<,>));
});

// Add FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// Add AutoMapper (manual registration to avoid version conflicts)
builder.Services.AddSingleton<AutoMapper.IMapper>(provider =>
{
	var loggerFactory = provider.GetService<ILoggerFactory>();
	var configuration = new AutoMapper.MapperConfiguration(cfg =>
	{
		cfg.AddProfile<MappingProfile>();
	}, loggerFactory);
	return configuration.CreateMapper();
});

// Register concrete services (these will be injected into the factory)
builder.Services.AddScoped<RedisCacheService>();
builder.Services.AddScoped<AzureBlobFileStorageService>();
builder.Services.AddScoped<MongoDataRepository>();
builder.Services.AddScoped<MongoUserRepository>();

// Register IUserRepository directly for AuthService (authentication needs direct access)
builder.Services.AddScoped<IUserRepository, MongoUserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Register Factory (this is the main registration point)
builder.Services.AddScoped<IStorageFactory, StorageFactory>();

// Add hosted services
builder.Services.AddHostedService<DataSeedingService>();

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
	// Configure Swagger for V1
	c.SwaggerDoc("v1",
		new OpenApiInfo {
			Title = "MG API",
			Version = "v1",
			Description = "A .NET Web API that provides a data retrieval service with caching, file storage, and database integration (Version 1)"
		});
	
	// Configure Swagger for V2
	c.SwaggerDoc("v2",
		new OpenApiInfo {
			Title = "MG API",
			Version = "v2", 
			Description = "Enhanced .NET Web API with metadata support, batch operations, and improved features (Version 2)"
		});

	// Add security definition for both versions
	c.AddSecurityDefinition("Bearer",
		new OpenApiSecurityScheme {
			Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer {token}'",
			Name = "Authorization",
			In = ParameterLocation.Header,
			Type = SecuritySchemeType.ApiKey,
			Scheme = "Bearer"
		});
	
	// Apply security requirement to all endpoints
	c.AddSecurityRequirement(new OpenApiSecurityRequirement {
		{
			new OpenApiSecurityScheme {
				Reference = new OpenApiReference {
					Type = ReferenceType.SecurityScheme,
					Id = "Bearer"
				},
			},
			Array.Empty<string>()
		}
	});
});
var app = builder.Build();

// Create API version sets
var apiVersionSet = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1, 0))
    .HasApiVersion(new ApiVersion(2, 0))
    .ReportApiVersions()
    .Build();

// Configure the HTTP request pipeline
app.UseExceptionHandler();
app.UseCors("AllowSpecificOrigins");
if (app.Environment.IsDevelopment()) {
	app.UseSwagger();
	app.UseSwaggerUI(c => {
		// Configure Swagger UI for multiple versions
		c.SwaggerEndpoint("/swagger/v1/swagger.json", "MG API V1");
		c.SwaggerEndpoint("/swagger/v2/swagger.json", "MG API V2");
		c.RoutePrefix = string.Empty; // Makes Swagger UI available at root
		c.DefaultModelsExpandDepth(-1); // Disable swagger models on startup
		c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None); // Collapse all operations by default
		c.DisplayRequestDuration(); // Show request duration in Swagger UI
	});
}
app.UseAuthentication();
app.UseMiddleware<ExpirationValidationMiddleware>();
app.UseAuthorization();

// Map endpoint groups
app.MapAuthenticationEndpoints();
app.MapDataV1Endpoints(apiVersionSet);
app.MapDataV2Endpoints(apiVersionSet);

app.MapDefaultEndpoints();
app.Run();
