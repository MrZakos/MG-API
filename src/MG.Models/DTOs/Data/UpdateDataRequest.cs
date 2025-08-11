using System.ComponentModel.DataAnnotations;

namespace MG.Models.DTOs.Data;

public class UpdateDataRequest {
	[Required] [MinLength(1)] public string Value { get; set; } = string.Empty;
}
