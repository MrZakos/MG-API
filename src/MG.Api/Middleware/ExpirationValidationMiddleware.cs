using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace MG.Api.Middleware;

public class ExpirationValidationMiddleware(RequestDelegate next, ILogger<ExpirationValidationMiddleware> logger) {
	
	// Define paths that should be excluded from token validation
	private static readonly string[] ExcludedPaths = [
		"/auth/login",
		"/auth/register", 
		"/swagger",
		"/health",
		"/_framework",
		"/favicon.ico"
	];
	
	public async Task InvokeAsync(HttpContext context) {
		try {
			// Check if the current path should be excluded from token validation
			if (ShouldExcludeFromValidation(context.Request.Path)) {
				await next(context);
				return;
			}

			// Check if the request has an Authorization header with Bearer token
			if (context.Request.Headers.ContainsKey("Authorization")) {
				var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
				
				if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ")) {
					var token = authHeader.Substring("Bearer ".Length).Trim();
					
					if (!string.IsNullOrEmpty(token)) {
						await ValidateTokenExpiration(context, token);
					}
				}
			}
		}
		catch (Exception ex) {
			logger.LogWarning(ex, "Error occurred while validating token expiration");
			// Continue with the request even if validation fails
		}

		await next(context);
	}

	private static bool ShouldExcludeFromValidation(PathString path) {
		return ExcludedPaths.Any(excludedPath => 
			path.StartsWithSegments(excludedPath, StringComparison.OrdinalIgnoreCase));
	}

	private async Task ValidateTokenExpiration(HttpContext context, string token) {
		try {
			var tokenHandler = new JwtSecurityTokenHandler();
			
			// Check if the token is a valid JWT format
			if (!tokenHandler.CanReadToken(token)) {
				logger.LogWarning("Invalid JWT token format");
				await HandleExpiredToken(context, "Invalid token format");
				return;
			}

			var jwtToken = tokenHandler.ReadJwtToken(token);
			
			// Check if token has expired
			var expirationTime = jwtToken.ValidTo;
			var currentTime = DateTime.UtcNow;
			
			if (expirationTime <= currentTime) {
				logger.LogInformation("JWT token has expired. Expiration: {ExpirationTime}, Current: {CurrentTime}", 
					expirationTime, currentTime);
				await HandleExpiredToken(context, "Token has expired");
				return;
			}

			// Check if token is about to expire (within 2 minutes)
			var timeUntilExpiry = expirationTime - currentTime;
			if (timeUntilExpiry.TotalMinutes <= 2) {
				logger.LogInformation("JWT token expires soon. Time until expiry: {TimeUntilExpiry}", timeUntilExpiry);
				
				// Add warning header to response
				context.Response.Headers.Add("X-Token-Expires-Soon", "true");
				context.Response.Headers.Add("X-Token-Expires-In", timeUntilExpiry.TotalSeconds.ToString("0"));
			}

			// Log successful validation
			var userId = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
			var userEmail = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
			
			logger.LogDebug("Token validation successful for user: {UserId} ({UserEmail})", userId, userEmail);
		}
		catch (Exception ex) {
			logger.LogError(ex, "Error occurred while parsing JWT token");
			await HandleExpiredToken(context, "Token validation failed");
		}
	}

	private async Task HandleExpiredToken(HttpContext context, string reason) {
		logger.LogInformation("Handling expired/invalid token: {Reason}", reason);
		
		context.Response.StatusCode = 401;
		context.Response.ContentType = "application/json";
		
		var response = new {
			error = "Token expired or invalid",
			message = reason,
			timestamp = DateTime.UtcNow
		};
		
		await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
	}
}
