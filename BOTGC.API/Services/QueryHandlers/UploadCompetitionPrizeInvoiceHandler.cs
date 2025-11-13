using Azure.Storage.Sas;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers;

public sealed class UploadCompetitionPrizeInvoiceHandler(
        IBlobStorageService blobStorage,
        IOptions<AppSettings> settings,
        ILogger<UploadCompetitionPrizeInvoiceHandler> logger
    ) : QueryHandlerBase<UploadCompetitionPrizeInvoiceCommand, string>
{
    private readonly IBlobStorageService _blobStorage = blobStorage ?? throw new ArgumentNullException(nameof(blobStorage));
    private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    private readonly ILogger<UploadCompetitionPrizeInvoiceHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public override async Task<string> Handle(UploadCompetitionPrizeInvoiceCommand request, CancellationToken cancellationToken)
    {
        if (request.Summary is null)
            throw new ArgumentNullException(nameof(request.Summary));

        if (string.IsNullOrWhiteSpace(request.InvoiceId))
            throw new ArgumentException("InvoiceId is required.", nameof(request.InvoiceId));

        if (request.PdfBytes is null || request.PdfBytes.Length == 0)
            throw new ArgumentException("PdfBytes is required.", nameof(request.PdfBytes));

        var summary = request.Summary;

        // Container name from config or sensible default.
        // Add something like:
        //  "Invoices": { "ContainerName": "competition-prize-invoices", "SasExpiryDays": 30 }
        // into AppSettings if you want to tune this.
        var containerName =
            _settings.Invoices?.ContainerName
            ?? "competition-prize-invoices";

        var sasExpiryDays =
            _settings.Invoices?.SasExpiryDays > 0
                ? _settings.Invoices.SasExpiryDays
                : 30;

        // Example filename: INV-2025-0001-20250314-Medal-May-Stableford.pdf
        var fileNameCore =
            $"{request.InvoiceId}-{summary.CompetitionDate:yyyyMMdd}-{summary.CompetitionName}";
        var safeFileName = SanitizeFileName(fileNameCore) + ".pdf";

        // Optional foldering by year/month to keep things tidy
        var blobName = $"{summary.CompetitionDate:yyyy}/{summary.CompetitionDate:MM}/{safeFileName}";

        _logger.LogInformation(
            "Uploading competition prize invoice {InvoiceId} for CompetitionId {CompetitionId} to container '{Container}' as '{BlobName}'.",
            request.InvoiceId,
            summary.CompetitionId,
            containerName,
            blobName);

        // Upload the file
        await _blobStorage.UploadAsync(
            containerName,
            blobName,
            request.PdfBytes,
            contentType: "application/pdf",
            cancellationToken: cancellationToken);

        // Try to generate a SAS URL; if that fails, fall back to direct blob URI.
        string publicUrl;

        try
        {
            var sasUri = _blobStorage.GetSasUri(
                containerName,
                blobName,
                BlobSasPermissions.Read,
                DateTimeOffset.UtcNow.AddDays(sasExpiryDays));

            publicUrl = sasUri.ToString();
            _logger.LogInformation(
                "Generated SAS URL for invoice {InvoiceId}: {Url}",
                request.InvoiceId,
                publicUrl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to generate SAS URL for invoice {InvoiceId}. Falling back to direct blob URL.",
                request.InvoiceId);

            var uri = _blobStorage.GetBlobUri(containerName, blobName);
            publicUrl = uri.ToString();
        }

        return publicUrl;
    }

    private static string SanitizeFileName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "invoice";

        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(
            value
                .Trim()
                .Select(ch => invalid.Contains(ch) ? '-' : ch)
                .ToArray());

        // collapse spaces
        cleaned = string.Join("-", cleaned.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));

        return string.IsNullOrWhiteSpace(cleaned) ? "invoice" : cleaned;
    }
}