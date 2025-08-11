namespace MG.Models.Options;

public class StorageOptions {
	public const string SectionName = "Storage";

	public bool EnableLogging { get; set; }
	public TtlOptions TTL { get; set; } = new();
	public AzureBlobOptions AzureBlob { get; set; } = new();
}

public class TtlOptions {
	public int CacheMilliseconds { get; set; }
	public int FileStorageMilliseconds { get; set; }
}

public class AzureBlobOptions {
	public string ContainerName { get; set; } = string.Empty;
}
