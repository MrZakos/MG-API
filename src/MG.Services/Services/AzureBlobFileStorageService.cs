using Azure.Storage.Blobs;
using System.Text.Json;
using MG.Services.Interfaces;
using Microsoft.Extensions.Options;
using MG.Models.Options;

namespace MG.Services.Services;

public class AzureBlobFileStorageService(BlobServiceClient blobServiceClient,IOptions<StorageOptions> storageOptions) : IFileStorageService {

	private readonly string _containerName = storageOptions.Value.AzureBlob.ContainerName;

	public async Task<T?> GetAsync<T>(string key) {
		try {
			var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
			await containerClient.CreateIfNotExistsAsync();

			// Search for blobs that start with the key (since they include timestamp)
			var blobs = containerClient.GetBlobsAsync(prefix:$"{key}_");
			await foreach (var blobItem in blobs) {
				var blobClient = containerClient.GetBlobClient(blobItem.Name);

				// Check if the blob is still valid based on filename timestamp
				if (await IsValidBlobAsync(blobItem.Name)) {
					var response = await blobClient.DownloadContentAsync();
					var content = response.Value.Content.ToString();
					return JsonSerializer.Deserialize<T>(content);
				}
				// Clean up expired blob
				await blobClient.DeleteIfExistsAsync();
			}
			return default;
		}
		catch {
			return default;
		}
	}

	public async Task SetAsync<T>(string key,T value,TimeSpan expiration) {
		var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
		await containerClient.CreateIfNotExistsAsync();
		var expirationTime = DateTime.UtcNow.Add(expiration);
		var fileName = $"{key}_{expirationTime:yyyyMMddHHmmss}.json";
		var blobClient = containerClient.GetBlobClient(fileName);
		var serializedValue = JsonSerializer.Serialize(value);
		using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(serializedValue));
		await blobClient.UploadAsync(stream,overwrite:true);

		// Clean up old blobs for this key
		var blobs = containerClient.GetBlobsAsync(prefix:$"{key}_");
		await foreach (var blobItem in blobs) {
			if (blobItem.Name != fileName) {
				var oldBlobClient = containerClient.GetBlobClient(blobItem.Name);
				await oldBlobClient.DeleteIfExistsAsync();
			}
		}
	}

	public async Task<bool> IsValidAsync(string key) {
		try {
			var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
			var blobs = containerClient.GetBlobsAsync(prefix:$"{key}_");
			await foreach (var blobItem in blobs) {
				if (await IsValidBlobAsync(blobItem.Name)) {
					return true;
				}
			}
			return false;
		}
		catch {
			return false;
		}
	}

	public async Task RemoveAsync(string key) {
		try {
			var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
			
			// Find and delete all blobs with this key prefix
			var blobs = containerClient.GetBlobsAsync(prefix: $"{key}_");
			await foreach (var blobItem in blobs) {
				var blobClient = containerClient.GetBlobClient(blobItem.Name);
				await blobClient.DeleteIfExistsAsync();
			}
		}
		catch {
			// Silently handle removal errors - this is for cache invalidation
			// so we don't want to fail the entire operation if cleanup fails
		}
	}

	private async Task<bool> IsValidBlobAsync(string blobName) {
		try {
			// Extract timestamp from filename: key_yyyyMMddHHmmss.json
			var lastUnderscoreIndex = blobName.LastIndexOf('_');
			var dotIndex = blobName.LastIndexOf('.');
			if (lastUnderscoreIndex == -1 ||
				dotIndex == -1 ||
				lastUnderscoreIndex >= dotIndex)
				return false;
			var timestampPart = blobName.Substring(lastUnderscoreIndex + 1,dotIndex - lastUnderscoreIndex - 1);
			if (DateTime.TryParseExact(timestampPart,
									   "yyyyMMddHHmmss",
									   null,
									   System.Globalization.DateTimeStyles.None,
									   out var expirationTime)) {
				return DateTime.UtcNow < expirationTime;
			}
			return false;
		}
		catch {
			return false;
		}
	}
}
