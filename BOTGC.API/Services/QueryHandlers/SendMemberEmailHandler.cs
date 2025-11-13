using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers;

public sealed class SendMemberEmailHandler(
       IOptions<AppSettings> settings,
       ILogger<SendMemberEmailHandler> logger,
       IDataProvider dataProvider
   ) : QueryHandlerBase<SendMemberEmailCommand, bool>
{
    private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    private readonly ILogger<SendMemberEmailHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IDataProvider _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));

    public async override Task<bool> Handle(SendMemberEmailCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RecipientEmail))
        {
            throw new ArgumentException("Recipient is required.", nameof(request.RecipientEmail));
        }

        if (string.IsNullOrWhiteSpace(request.Subject))
        {
            throw new ArgumentException("Subject is required.", nameof(request.Subject));
        }

        if (string.IsNullOrWhiteSpace(request.FromName))
        {
            throw new ArgumentException("FromName is required.", nameof(request.FromName));
        }

        if (string.IsNullOrWhiteSpace(request.FromAddress))
        {
            throw new ArgumentException("FromAddress is required.", nameof(request.FromAddress));
        }

        if (string.IsNullOrWhiteSpace(request.BodyHtml))
        {
            throw new ArgumentException("BodyHtml is required.", nameof(request.BodyHtml));
        }

        var memberId = request.SenderId;
        if (memberId <= 0)
        {
            throw new InvalidOperationException("IG.MemberEmailMemberId is not configured.");
        }

        var baseUrl = _settings.IG.BaseUrl?.TrimEnd('/')
                      ?? throw new InvalidOperationException("IG.BaseUrl is not configured.");

        var url = $"{baseUrl}/member.php?memberid={memberId}&tab=email&requestType=ajax&ajaxaction=send";

        var form = new Dictionary<string, string>
        {
            ["email_subject"] = request.Subject,
            ["email_fromname"] = request.FromName,
            ["email_fromaddress"] = request.FromAddress,
            ["recipient"] = request.RecipientEmail,
            ["email_content"] = request.BodyHtml,
            ["email_preview_to"] = request.RecipientEmail
        };

        try
        {
            _logger.LogInformation(
                "Sending member email to {Recipient} with subject '{Subject}' via IG email endpoint.",
                request.RecipientEmail,
                request.Subject
            );

            await _dataProvider.PostData(url, form);

            _logger.LogInformation(
                "Successfully sent member email to {Recipient} with subject '{Subject}'.",
                request.RecipientEmail,
                request.Subject
            );

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send member email to {Recipient} with subject '{Subject}'.",
                request.RecipientEmail,
                request.Subject
            );
            return false;
        }
    }
}
