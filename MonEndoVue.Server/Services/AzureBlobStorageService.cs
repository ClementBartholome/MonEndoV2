using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
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
        var blobServiceClient = new BlobServiceClient(options.Value.ConnectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient(options.Value.ContainerName);
    }

    public async Task<string> UploadFileAsync(IFormFile file, string fileName)
    {
        var blobClient = _containerClient.GetBlobClient(fileName);
        await using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(stream, overwrite: true);
        return blobClient.Uri.ToString();
    }

    public string GetBlobNameFromUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return string.Empty;
        var uri = new Uri(url);
        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length <= 1 ? string.Empty : string.Join('/', segments.Skip(1));
    }

    public async Task DeleteFileAsync(string blobName)
    {
        if (string.IsNullOrWhiteSpace(blobName)) return;
        var blobClient = _containerClient.GetBlobClient(blobName);
        await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
    }
}