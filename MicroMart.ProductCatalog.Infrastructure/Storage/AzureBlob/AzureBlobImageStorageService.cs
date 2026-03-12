using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using MicroMart.ProductCatalog.Application.Abstractions;
using MicroMart.ProductCatalog.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
namespace MicroMart.ProductCatalog.Infrastructure.Storage.AzureBlob;

public sealed class AzureBlobImageStorageService(
    BlobServiceClient blobClient,
    IOptions<BlobStorageSettings> options,
    ILogger<AzureBlobImageStorageService> logger)
    : IImageStorageService
{
    private readonly BlobStorageSettings _settings = options.Value;

    public async Task<string> UploadAsync(
        Stream imageStream,
        string fileName,
        string contentType,
        CancellationToken ct)
    {
        // Validate file size before upload
        if (imageStream.Length > _settings.MaxFileSizeBytes)
            throw new InvalidOperationException(
                $"Image exceeds maximum size of {_settings.MaxFileSizeBytes / 1024 / 1024}MB");

        // Validate content type
        if (!_settings.AllowedMimeTypes.Contains(contentType))
            throw new InvalidOperationException(
                $"Content type '{contentType}' is not allowed");

        // Generate unique blob name with folder structure: products/{year}/{month}/{guid}.{ext}
        var extension = Path.GetExtension(fileName).TrimStart('.');
        var blobName = $"products/{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid()}.{extension}";

        var container = blobClient.GetBlobContainerClient(_settings.ContainerName);
        var blobRef = container.GetBlobClient(blobName);

        var uploadOptions = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders
            {
                ContentType = contentType,
                CacheControl = "public, max-age=31536000"  // 1 year CDN cache
            }
        };

        await blobRef.UploadAsync(imageStream, uploadOptions, ct);

        // Return CDN URL if configured, otherwise direct blob URL
        var cdnBase = _settings.CdnBaseUrl.TrimEnd('/');
        var url = string.IsNullOrEmpty(cdnBase)
            ? blobRef.Uri.ToString()
            : $"{cdnBase}/{blobName}";

        logger.LogInformation(
            "Uploaded image to blob storage: {BlobName}", blobName);

        return url;
    }

    public async Task DeleteAsync(string imageUrl, CancellationToken ct)
    {
        try
        {
            // Extract blob name from either CDN URL or direct blob URL
            var blobName = ExtractBlobName(imageUrl);
            var container = blobClient.GetBlobContainerClient(_settings.ContainerName);
            var blobRef = container.GetBlobClient(blobName);
            await blobRef.DeleteIfExistsAsync(cancellationToken: ct);
            logger.LogInformation("Deleted blob: {BlobName}", blobName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to delete blob for URL '{Url}'", imageUrl);
        }
    }

    private string ExtractBlobName(string imageUrl)
    {
        // Extract relative path from CDN URL or direct Blob URL
        var cdnBase = _settings.CdnBaseUrl.TrimEnd('/');
        if (!string.IsNullOrEmpty(cdnBase) && imageUrl.StartsWith(cdnBase))
            return imageUrl[(cdnBase.Length + 1)..];

        var uri = new Uri(imageUrl);
        // Remove leading /containerName/ from path
        var path = uri.AbsolutePath.TrimStart('/');
        var containerPrefix = _settings.ContainerName + "/";
        return path.StartsWith(containerPrefix)
            ? path[containerPrefix.Length..]
            : path;
    }
}