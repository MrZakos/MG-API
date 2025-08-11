using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MG.Models.Entities;

public class User {

	[BsonId] [BsonRepresentation(BsonType.ObjectId)] public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
	[BsonElement("email")] public string Email { get; set; } = string.Empty;
	[BsonElement("passwordHash")] public string PasswordHash { get; set; } = string.Empty;
	[BsonElement("role")] public string Role { get; set; } = "User"; // User or Admin

	[BsonElement("createdAt")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	[BsonElement("updatedAt")] public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
