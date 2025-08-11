using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MG.Models.Entities;
using MG.Services.Interfaces;
using MG.Models.Options;

namespace MG.Services.Services;

public class AuthService(IUserRepository userRepository,IOptions<JwtOptions> jwtOptions) : IAuthService {

	private readonly JwtOptions _jwtOptions = jwtOptions.Value;

	public Task<string> GenerateJwtTokenAsync(User user) {
		var key = Encoding.ASCII.GetBytes(_jwtOptions.Key);
		var claims = new[] { 
				new Claim(ClaimTypes.NameIdentifier,user.Id),
				new Claim(ClaimTypes.Email,user.Email),
				new Claim(ClaimTypes.Role,user.Role) 
			};
		var tokenDescriptor = new SecurityTokenDescriptor {
			Subject = new ClaimsIdentity(claims),
			Expires = DateTime.UtcNow.AddMilliseconds(_jwtOptions.ExpirationMilliseconds),
			SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),SecurityAlgorithms.HmacSha256Signature),
			Issuer = _jwtOptions.Issuer,
			Audience = _jwtOptions.Audience
		};
		var tokenHandler = new JwtSecurityTokenHandler();
		var token = tokenHandler.CreateToken(tokenDescriptor);
		return Task.FromResult(tokenHandler.WriteToken(token));
	}

	public async Task<User?> AuthenticateAsync(string email,string password) {
		var user = await userRepository.GetByEmailAsync(email);
		if (user == null ||
			!VerifyPassword(password,user.PasswordHash)) {
			return null;
		}
		return user;
	}

	public string HashPassword(string password) {
		return BCrypt.Net.BCrypt.HashPassword(password);
	}

	public bool VerifyPassword(string password,string hash) {
		return BCrypt.Net.BCrypt.Verify(password,hash);
	}
}
