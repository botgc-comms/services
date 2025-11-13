using Azure.Storage.Sas;

namespace BOTGC.API.Interfaces;

public interface IBlobStorageService
{
    Task<string> UploadAsync(
        string containerName,
        string blobName,
        byte[] content,
        string contentType = "application/octet-stream",
        CancellationToken cancellationToken = default);

    Task<string> UploadAsync(
        string containerName,
        string blobName,
        Stream content,
        string contentType = "application/octet-stream",
        CancellationToken cancellationToken = default);

    Task<Stream?> DownloadAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default);

    Uri GetBlobUri(string containerName, string blobName);

    /// <summary>
    /// Generate a SAS URL for an existing blob.
    /// Use when the container is private but you want a shareable link.
    /// </summary>
    Uri GetSasUri(
        string containerName,
        string blobName,
        BlobSasPermissions permissions,
        DateTimeOffset expiresOn);
}
