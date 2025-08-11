using Asp.Versioning;
using Asp.Versioning.Builder;
using FluentValidation;
using MediatR;
using MG.Models.DTOs.Data;

namespace MG.Api.Features.Data.V1;

public static class DataEndpointsV1
{
    public static void MapDataV1Endpoints(this IEndpointRouteBuilder app, ApiVersionSet apiVersionSet)
    {
        var dataGroup = app.MapGroup("/v1/data")
            .WithTags("Data V1")
            .WithApiVersionSet(apiVersionSet)
            .MapToApiVersion(new ApiVersion(1, 0));

        dataGroup.MapGet("/{id}",
            async (string id, IMediator mediator) =>
            {
                try
                {
                    var query = new GetData.Query(id);
                    var result = await mediator.Send(query);
                    return result != null ? Results.Ok(result) : Results.NotFound();
                }
                catch (ValidationException ex)
                {
                    return Results.BadRequest(ex.Errors);
                }
            })
            .RequireAuthorization("UserOrAdmin")
            .WithName("GetDataV1")
            .WithSummary("Get data by ID (V1)")
            .WithDescription("Retrieves data from cache, file storage, or database")
            .Produces<DataResponse>()
            .Produces(404)
            .Produces(400);

        dataGroup.MapPost("/",
            async (CreateDataRequest request, IMediator mediator) =>
            {
                try
                {
                    var command = new CreateData.Command(request.Value);
                    var result = await mediator.Send(command);
                    return Results.Created($"/v1/data/{result.Id}", result);
                }
                catch (ValidationException ex)
                {
                    return Results.BadRequest(ex.Errors);
                }
            })
            .RequireAuthorization("AdminOnly")
            .WithName("CreateDataV1")
            .WithSummary("Create new data (V1) - Admin Only")
            .WithDescription("Creates new data item with caching across all storage layers. Requires Admin role.")
            .Produces<DataResponse>(201)
            .Produces(400)
            .Produces(403);

        dataGroup.MapPut("/{id}",
            async (string id, UpdateDataRequest request, IMediator mediator) =>
            {
                try
                {
                    var command = new UpdateData.Command(id, request.Value);
                    var result = await mediator.Send(command);
                    return result != null ? Results.Ok(result) : Results.NotFound();
                }
                catch (ValidationException ex)
                {
                    return Results.BadRequest(ex.Errors);
                }
            })
            .RequireAuthorization("AdminOnly")
            .WithName("UpdateDataV1")
            .WithSummary("Update existing data (V1) - Admin Only")
            .WithDescription("Updates existing data item and refreshes all caches. Requires Admin role.")
            .Produces<DataResponse>()
            .Produces(404)
            .Produces(400)
            .Produces(403);
    }
}

public record CreateDataRequest(string Value);
public record UpdateDataRequest(string Value);
