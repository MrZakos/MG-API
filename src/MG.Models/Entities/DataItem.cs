using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MG.Models.Entities;

public class DataItem {
	[BsonId] [BsonRepresentation(BsonType.ObjectId)] public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
	[BsonElement("value")] public string Value { get; set; } = string.Empty;
	[BsonElement("createdAt")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	[BsonElement("updatedAt")] public DateTime? UpdatedAt { get; set; }
}
