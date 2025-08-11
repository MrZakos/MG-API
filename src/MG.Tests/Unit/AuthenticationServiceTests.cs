using System.IdentityModel.Tokens.Jwt;
using System.Text;
using MG.Services.Services;
using Microsoft.Extensions.Options;
using MG.Models.Options;
using MG.Services.Interfaces;
using MG.Models.Entities;
using Moq;
using Xunit;
using Microsoft.IdentityModel.Tokens;

namespace MG.Tests.Unit;

public class AuthenticationServiceTests {
	private readonly AuthService _authService;
	private readonly Mock<IUserRepository> _mockUserRepository;
	private readonly JwtOptions _jwtOptions;

	public AuthenticationServiceTests() {
		// Setup JWT options
		_jwtOptions = new JwtOptions {
			Key = "ThisIsASecretKeyForJWTTokenGenerationWithMinimum256BitsLength",
			Issuer = "TestIssuer",
			Audience = "TestAudience",
			ExpirationMilliseconds = 3600000  // 60 minutes = 3,600,000 milliseconds
		};
		var jwtOptionsWrapper = Options.Create(_jwtOptions);

		// Setup mock repository
		_mockUserRepository = new Mock<IUserRepository>();

		// Initialize AuthService with dependencies
		_authService = new AuthService(_mockUserRepository.Object,jwtOptionsWrapper);
	}

	private bool ValidateToken(string token) {
		if (string.IsNullOrEmpty(token)) {
			return false;
		}
		try {
			var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
			var tokenHandler = new JwtSecurityTokenHandler();
			var validationParameters = new TokenValidationParameters {
				ValidateIssuerSigningKey = true,
				IssuerSigningKey = key,
				ValidateIssuer = true,
				ValidIssuer = _jwtOptions.Issuer,
				ValidateAudience = true,
				ValidAudience = _jwtOptions.Audience,
				ValidateLifetime = true,
				ClockSkew = TimeSpan.Zero
			};
			tokenHandler.ValidateToken(token,validationParameters,out _);
			return true;
		}
		catch {
			return false;
		}
	}
	
	[Fact]
	public async Task AuthenticateAsync_ShouldReturnUser_WhenCredentialsAreValid() {
		var token = await _authService.GenerateJwtTokenAsync(new User {
			Email = "user@test.com",
			Id = "123",
			Role = "User"
		});
		var isTokenValid = ValidateToken(token);
		Assert.True(isTokenValid);
	}
}
