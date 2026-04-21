using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure;
using Microsoft.Extensions.Options;

namespace MonEndoVue.Server.Services;

public class AzureBlobStorageOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = string.Empty;
}

public class AzureBlobStorageService
{
    private readonly BlobContainerClient _containerClient;

    public AzureBlobStorageService(IOptions<AzureBlobStorageOptions> options)
    {
        var connectionString = options.Value.ConnectionString;
        var containerName = options.Value.ContainerName;

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Azure Blob connection string is missing.");
        }

        if (string.IsNullOrWhiteSpace(containerName))
        {
            throw new InvalidOperationException("Azure Blob container name is missing.");
        }

        var blobServiceClient = new BlobServiceClient(connectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        _containerClient.CreateIfNotExists(PublicAccessType.None);
    }

    public async Task<string> UploadFileAsync(IFormFile file, string fileName)
    {
        var blobClient = _containerClient.GetBlobClient(fileName);
        await using var stream = file.OpenReadStream();
        var headers = new BlobHttpHeaders
        {
            ContentType = ResolveContentType(file)
        };

        await blobClient.UploadAsync(stream, new BlobUploadOptions
        {
            HttpHeaders = headers
        });

        return blobClient.Uri.ToString();
    }

    private static string ResolveContentType(IFormFile file)
    {
        if (!string.IsNullOrWhiteSpace(file.ContentType))
        {
            return file.ContentType;
        }

        return Path.GetExtension(file.FileName).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".heic" => "image/heic",
            ".heif" => "image/heif",
            _ => "application/octet-stream"
        };
    }

    public string GetBlobNameFromUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return string.Empty;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return url;
        }

        var path = uri.AbsolutePath.Trim('/');
        var containerPrefix = $"{_containerClient.Name}/";

        if (path.StartsWith(containerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return path[containerPrefix.Length..];
        }

        return path;
    }

    public async Task DeleteFileAsync(string blobName)
    {
        if (string.IsNullOrWhiteSpace(blobName)) return;

        var blobClient = _containerClient.GetBlobClient(blobName);
        await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
    }

    public async Task DeleteFileByUrlAsync(string fileUrl)
    {
        var blobName = GetBlobNameFromUrl(fileUrl);
        if (string.IsNullOrWhiteSpace(blobName)) return;

        try
        {
            await DeleteFileAsync(blobName);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            // Blob already removed: no-op to keep deletion idempotent.
        }
    }
}