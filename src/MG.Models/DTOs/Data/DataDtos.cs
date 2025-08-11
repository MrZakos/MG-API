namespace MG.Models.DTOs.Data;

public class DataResponse {
	public string Id { get; set; } = string.Empty;
	public string Value { get; set; } = string.Empty;
	public DateTime CreatedAt { get; set; }
	public DateTime? UpdatedAt { get; set; }
}
