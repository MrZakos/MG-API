using MediatR;
using MG.Services.Interfaces;
using FluentValidation;
using MG.Models.DTOs.Authentication;
using Microsoft.Extensions.Options;
using MG.Models.Options;

namespace MG.Api.Features.Authentication;

public static class Login {
	public record Command(string Email,string Password) : IRequest<LoginResponse>;

	public class Handler(IAuthService authService,IOptions<JwtOptions> jwtOptions) : IRequestHandler<Command,LoginResponse> {
		private readonly JwtOptions _jwtOptions = jwtOptions.Value;

		public async Task<LoginResponse> Handle(Command request,CancellationToken cancellationToken) {
			var user = await authService.AuthenticateAsync(request.Email,request.Password);
			if (user == null)
				throw new UnauthorizedAccessException("Invalid credentials");
			var token = await authService.GenerateJwtTokenAsync(user);
			return new LoginResponse {
				Token = token,
				Email = user.Email,
				Role = user.Role,
				ExpiresAt = DateTime.UtcNow.AddMilliseconds(_jwtOptions.ExpirationMilliseconds)
			};
		}
	}

	public class Validator : AbstractValidator<Command> {
		public Validator() {
			RuleFor(x => x.Email)
				.NotEmpty()
				.EmailAddress();
			RuleFor(x => x.Password)
				.NotEmpty()
				.MinimumLength(6);
		}
	}
}
