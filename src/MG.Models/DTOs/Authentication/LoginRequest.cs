﻿using System.ComponentModel.DataAnnotations;

namespace MG.Models.DTOs.Authentication;

public class LoginRequest {
	[Required] [EmailAddress] public string Email { get; set; } = string.Empty;
	[Required] [MinLength(6)] public string Password { get; set; } = string.Empty;
}
