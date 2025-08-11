namespace MG.Models.Options;

public class MongoDbOptions {
	public const string SectionName = "MongoDB";

	public string DatabaseName { get; set; } = string.Empty;
	public CollectionsOptions Collections { get; set; } = new();
}

public class CollectionsOptions {
	public string DataItems { get; set; } = string.Empty;
	public string Users { get; set; } = string.Empty;
}
