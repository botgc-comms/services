using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using BOTGC.API.Interfaces;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace BOTGC.API.Services;

public sealed class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _serviceClient;
    private readonly ILogger<AzureBlobStorageService> _logger;
    private readonly ConcurrentDictionary<string, BlobContainerClient> _containers = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _publicContainers;

    public AzureBlobStorageService(
        IOptions<AppSettings> settings,
        ILogger<AzureBlobStorageService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var appSettings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        var connectionString = appSettings.Storage?.ConnectionString;

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Storage.ConnectionString is not configured.");

        _serviceClient = new BlobServiceClient(connectionString);

        _publicContainers = new HashSet<string>(
            appSettings.Storage?.PublicContainers ?? Array.Empty<string>(),
            StringComparer.OrdinalIgnoreCase);
    }

    public async Task<string> UploadAsync(
        string containerName,
        string blobName,
        byte[] content,
        string contentType = "application/octet-stream",
        CancellationToken cancellationToken = default)
    {
        if (content is null) throw new ArgumentNullException(nameof(content));

        await using var ms = new MemoryStream(content, writable: false);
        return await UploadAsync(containerName, blobName, ms, contentType, cancellationToken);
    }

    public async Task<string> UploadAsync(
        string containerName,
        string blobName,
        Stream content,
        string contentType = "application/octet-stream",
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(containerName))
            throw new ArgumentException("Container name is required.", nameof(containerName));

        if (string.IsNullOrWhiteSpace(blobName))
            throw new ArgumentException("Blob name is required.", nameof(blobName));

        if (content is null)
            throw new ArgumentNullException(nameof(content));

        var container = await GetOrCreateContainerAsync(containerName, cancellationToken);
        var blob = container.GetBlobClient(blobName);

        _logger.LogInformation(
            "Uploading blob '{BlobName}' to container '{ContainerName}'.",
            blobName, containerName);

        var headers = new BlobHttpHeaders
        {
            ContentType = string.IsNullOrWhiteSpace(contentType)
                ? "application/octet-stream"
                : contentType
        };

        content.Position = 0;
        await blob.UploadAsync(content, headers, cancellationToken: cancellationToken);

        return blob.Uri.ToString();
    }

    public async Task<Stream?> DownloadAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        var container = _serviceClient.GetBlobContainerClient(containerName);
        var blob = container.GetBlobClient(blobName);

        if (!await blob.ExistsAsync(cancellationToken))
        {
            _logger.LogWarning(
                "Blob '{BlobName}' not found in container '{ContainerName}'.",
                blobName, containerName);
            return null;
        }

        var response = await blob.DownloadAsync(cancellationToken);
        var ms = new MemoryStream();
        await response.Value.Content.CopyToAsync(ms, cancellationToken);
        ms.Position = 0;
        return ms;
    }

    public async Task<bool> ExistsAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        var container = _serviceClient.GetBlobContainerClient(containerName);
        var blob = container.GetBlobClient(blobName);
        var exists = await blob.ExistsAsync(cancellationToken);
        return exists.Value;
    }

    public async Task<bool> DeleteAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        var container = _serviceClient.GetBlobContainerClient(containerName);
        var blob = container.GetBlobClient(blobName);

        try
        {
            var response = await blob.DeleteIfExistsAsync(
                DeleteSnapshotsOption.IncludeSnapshots,
                cancellationToken: cancellationToken);

            return response.Value;
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(
                ex,
                "Error deleting blob '{BlobName}' from container '{ContainerName}'.",
                blobName, containerName);
            throw;
        }
    }

    public Uri GetBlobUri(string containerName, string blobName)
    {
        var container = _serviceClient.GetBlobContainerClient(containerName);
        var blob = container.GetBlobClient(blobName);
        return blob.Uri;
    }

    public Uri GetSasUri(
        string containerName,
        string blobName,
        BlobSasPermissions permissions,
        DateTimeOffset expiresOn)
    {
        var container = _serviceClient.GetBlobContainerClient(containerName);
        var blob = container.GetBlobClient(blobName);

        if (!blob.CanGenerateSasUri)
        {
            throw new InvalidOperationException(
                "Cannot generate SAS URI for this blob. Ensure the client is constructed with a key-based credential.");
        }

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = container.Name,
            BlobName = blob.Name,
            Resource = "b",
            ExpiresOn = expiresOn
        };

        sasBuilder.SetPermissions(permissions);

        return blob.GenerateSasUri(sasBuilder);
    }

    private async Task<BlobContainerClient> GetOrCreateContainerAsync(
        string containerName,
        CancellationToken cancellationToken)
    {
        if (_containers.TryGetValue(containerName, out var existing))
        {
            return existing;
        }

        var container = _serviceClient.GetBlobContainerClient(containerName);

        var desiredAccess = _publicContainers.Contains(containerName)
            ? PublicAccessType.Blob        // public read for blobs only
            : PublicAccessType.None;       // fully private

        try
        {
            var created = await container.CreateIfNotExistsAsync(
                publicAccessType: desiredAccess,
                cancellationToken: cancellationToken);

            if (created != null)
            {
                _logger.LogInformation(
                    "Created blob container '{ContainerName}' with access level {Access}.",
                    containerName, desiredAccess);
            }
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == BlobErrorCode.ContainerAlreadyExists)
        {
            // Another instance created it; ignore.
        }

        _containers[containerName] = container;
        return container;
    }
}
