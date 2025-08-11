using FluentValidation;
using MediatR;
using MG.Models.DTOs.Authentication;

namespace MG.Api.Features.Authentication;

public static class AuthenticationEndpoints {
	public static void MapAuthenticationEndpoints(this IEndpointRouteBuilder app) {
		
		var authGroup = app.MapGroup("/auth").WithTags("Authentication");
		
		authGroup.MapPost("/login",
						  async (LoginRequest request,IMediator mediator) => {
							  try {
								  var command = new Login.Command(request.Email,request.Password);
								  var result = await mediator.Send(command);
								  return Results.Ok(result);
							  }
							  catch (UnauthorizedAccessException) {
								  return Results.Unauthorized();
							  }
							  catch (ValidationException ex) {
								  return Results.BadRequest(ex.Errors);
							  }
						  })
				 .WithName("Login")
				 .Produces<LoginResponse>()
				 .Produces(401)
				 .ProducesValidationProblem();
	}
}
