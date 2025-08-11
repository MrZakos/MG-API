using System.ComponentModel.DataAnnotations;

namespace MG.Models.DTOs.Data;

public class CreateDataRequest {
	[Required] [MinLength(1)] public string Value { get; set; } = string.Empty;
}
