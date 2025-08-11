using FluentValidation;
using MediatR;
using MG.Models.DTOs.Data;

namespace MG.Api.Features.Data;

public static class DataEndpoints {
	public static void MapDataEndpoints(this IEndpointRouteBuilder app) {
		
		var dataGroup = app.MapGroup("/data").WithTags("Data");
		
		dataGroup.MapGet("/{id}",
						 async (string id,IMediator mediator) => {
							 try {
								 var query = new GetData.Query(id);
								 var result = await mediator.Send(query);
								 return result != null ? Results.Ok(result) : Results.NotFound();
							 }
							 catch (ValidationException ex) {
								 return Results.BadRequest(ex.Errors);
							 }
						 })
				 .RequireAuthorization("UserOrAdmin")
				 .WithName("GetData")
				 .Produces<DataResponse>()
				 .Produces(404)
				 .Produces(401)
				 .ProducesValidationProblem();
		
		dataGroup.MapPost("/",
						  async (CreateDataRequest request,IMediator mediator) => {
							  try {
								  var command = new CreateData.Command(request.Value);
								  var result = await mediator.Send(command);
								  return Results.Created($"/data/{result.Id}",result);
							  }
							  catch (ValidationException ex) {
								  return Results.BadRequest(ex.Errors);
							  }
						  })
				 .RequireAuthorization("AdminOnly")
				 .WithName("CreateData")
				 .Produces<DataResponse>(201)
				 .Produces(401)
				 .Produces(403)
				 .ProducesValidationProblem();
		
		dataGroup.MapPut("/{id}",
						 async (string id,
								UpdateDataRequest request,
								IMediator mediator) => {
							 try {
								 var command = new UpdateData.Command(id,request.Value);
								 var result = await mediator.Send(command);
								 return result != null ? Results.Ok(result) : Results.NotFound();
							 }
							 catch (ValidationException ex) {
								 return Results.BadRequest(ex.Errors);
							 }
						 })
				 .RequireAuthorization("AdminOnly")
				 .WithName("UpdateData")
				 .Produces<DataResponse>()
				 .Produces(404)
				 .Produces(401)
				 .Produces(403)
				 .ProducesValidationProblem();
	}
}
