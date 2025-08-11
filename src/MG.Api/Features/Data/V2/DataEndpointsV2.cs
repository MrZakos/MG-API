using Asp.Versioning;
using Asp.Versioning.Builder;
using FluentValidation;
using MediatR;
using MG.Models.DTOs.Data;

namespace MG.Api.Features.Data.V2;

public static class DataEndpointsV2
{
    public static void MapDataV2Endpoints(this IEndpointRouteBuilder app, ApiVersionSet apiVersionSet)
    {
        var dataGroup = app.MapGroup("/v2/data")
            .WithTags("Data V2")
            .WithApiVersionSet(apiVersionSet)
            .MapToApiVersion(new ApiVersion(2, 0));

        dataGroup.MapGet("/{id}",
            async (string id, IMediator mediator, bool includeMetadata = true) =>
            {
                try
                {
                    var query = new GetData.Query(id);
                    var result = await mediator.Send(query);
                    
                    if (result == null) return Results.NotFound();
                    
                    // V2 enhancement: Include metadata if requested
                    if (includeMetadata)
                    {
                        return Results.Ok(new DataResponseV2
                        {
                            Id = result.Id,
                            Value = result.Value,
                            CreatedAt = result.CreatedAt,
                            UpdatedAt = result.UpdatedAt,
                            Metadata = new DataMetadata
                            {
                                Version = "2.0",
                                Source = "API V2",
                                RequestedAt = DateTime.UtcNow,
                                CacheHit = false // This could be enhanced to track actual cache hits
                            }
                        });
                    }
                    
                    return Results.Ok(result);
                }
                catch (ValidationException ex)
                {
                    return Results.BadRequest(ex.Errors);
                }
            })
            .RequireAuthorization("UserOrAdmin")
            .WithName("GetDataV2")
            .WithSummary("Get data by ID (V2)")
            .WithDescription("Retrieves data with optional metadata information")
            .Produces<DataResponse>()
            .Produces<DataResponseV2>()
            .Produces(404)
            .Produces(400);

        dataGroup.MapPost("/",
            async (CreateDataRequestV2 request, IMediator mediator) =>
            {
                try
                {
                    var command = new CreateData.Command(request.Value);
                    var result = await mediator.Send(command);
                    
                    // V2 enhancement: Return enhanced response with metadata
                    var enhancedResult = new DataResponseV2
                    {
                        Id = result.Id,
                        Value = result.Value,
                        CreatedAt = result.CreatedAt,
                        UpdatedAt = result.UpdatedAt,
                        Metadata = new DataMetadata
                        {
                            Version = "2.0",
                            Source = "API V2",
                            RequestedAt = DateTime.UtcNow,
                            Tags = request.Tags ?? []
                        }
                    };
                    
                    return Results.Created($"/v2/data/{result.Id}", enhancedResult);
                }
                catch (ValidationException ex)
                {
                    return Results.BadRequest(ex.Errors);
                }
            })
            .RequireAuthorization("AdminOnly")
            .WithName("CreateDataV2")
            .WithSummary("Create new data (V2) - Admin Only")
            .WithDescription("Creates new data item with enhanced metadata support. Requires Admin role.")
            .Produces<DataResponseV2>(201)
            .Produces(400)
            .Produces(403);

        dataGroup.MapPut("/{id}",
            async (string id, UpdateDataRequestV2 request, IMediator mediator) =>
            {
                try
                {
                    var command = new UpdateData.Command(id, request.Value);
                    var result = await mediator.Send(command);
                    
                    if (result == null) return Results.NotFound();
                    
                    // V2 enhancement: Return enhanced response with update metadata
                    var enhancedResult = new DataResponseV2
                    {
                        Id = result.Id,
                        Value = result.Value,
                        CreatedAt = result.CreatedAt,
                        UpdatedAt = result.UpdatedAt,
                        Metadata = new DataMetadata
                        {
                            Version = "2.0",
                            Source = "API V2",
                            RequestedAt = DateTime.UtcNow,
                            LastModifiedBy = request.ModifiedBy ?? "API",
                            Tags = request.Tags ?? []
                        }
                    };
                    
                    return Results.Ok(enhancedResult);
                }
                catch (ValidationException ex)
                {
                    return Results.BadRequest(ex.Errors);
                }
            })
            .RequireAuthorization("AdminOnly")
            .WithName("UpdateDataV2")
            .WithSummary("Update existing data (V2) - Admin Only")
            .WithDescription("Updates existing data item with enhanced metadata tracking. Requires Admin role.")
            .Produces<DataResponseV2>()
            .Produces(404)
            .Produces(400)
            .Produces(403);

        // V2 exclusive endpoint: Batch operations
        dataGroup.MapPost("/batch",
            async (BatchCreateRequestV2 request, IMediator mediator) =>
            {
                try
                {
                    var tasks = request.Items.Select(async item =>
                    {
                        var command = new CreateData.Command(item.Value);
                        return await mediator.Send(command);
                    });

                    var results = await Task.WhenAll(tasks);
                    
                    var enhancedResults = results.Select(result => new DataResponseV2
                    {
                        Id = result.Id,
                        Value = result.Value,
                        CreatedAt = result.CreatedAt,
                        UpdatedAt = result.UpdatedAt,
                        Metadata = new DataMetadata
                        {
                            Version = "2.0",
                            Source = "API V2 Batch",
                            RequestedAt = DateTime.UtcNow
                        }
                    });

                    return Results.Ok(new BatchResponseV2
                    {
                        Items = enhancedResults.ToList(),
                        TotalCount = enhancedResults.Count(),
                        ProcessedAt = DateTime.UtcNow
                    });
                }
                catch (ValidationException ex)
                {
                    return Results.BadRequest(ex.Errors);
                }
            })
            .RequireAuthorization("AdminOnly")
            .WithName("BatchCreateDataV2")
            .WithSummary("Batch create data (V2 Only) - Admin Only")
            .WithDescription("Creates multiple data items in a single request. Requires Admin role.")
            .Produces<BatchResponseV2>()
            .Produces(400)
            .Produces(403);
    }
}

// V2 Enhanced DTOs
public record DataResponseV2
{
    public string Id { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public DataMetadata? Metadata { get; init; }
}

public record DataMetadata
{
    public string Version { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public DateTime RequestedAt { get; init; }
    public bool CacheHit { get; init; }
    public string? LastModifiedBy { get; init; }
    public List<string> Tags { get; init; } = [];
}

public record CreateDataRequestV2(string Value, List<string>? Tags = null);
public record UpdateDataRequestV2(string Value, string? ModifiedBy = null, List<string>? Tags = null);
public record BatchCreateRequestV2(List<CreateDataRequestV2> Items);
public record BatchResponseV2
{
    public List<DataResponseV2> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public DateTime ProcessedAt { get; init; }
}
