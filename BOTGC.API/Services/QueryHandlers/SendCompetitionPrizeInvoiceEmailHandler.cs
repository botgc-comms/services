using BOTGC.API.Dto;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net;
using System.Text;

namespace BOTGC.API.Services.QueryHandlers;

public sealed class SendCompetitionPrizeInvoiceEmailHandler(
        IOptions<AppSettings> settings,
        ILogger<SendCompetitionPrizeInvoiceEmailHandler> logger,
        IMediator mediator
    ) : QueryHandlerBase<SendCompetitionPrizeInvoiceEmailCommand, bool>
{
    private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    private readonly ILogger<SendCompetitionPrizeInvoiceEmailHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

    public override async Task<bool> Handle(SendCompetitionPrizeInvoiceEmailCommand request, CancellationToken cancellationToken)
    {
        if (request.Summary is null)
            throw new ArgumentNullException(nameof(request.Summary));

        var summary = request.Summary;

        // Collect all paid placings
        var allPlacings = summary.Divisions?
            .SelectMany(d => d.Placings ?? Enumerable.Empty<PlacingDto>())
            .Where(p => p.Amount > 0)
            .ToList() ?? new List<PlacingDto>();

        if (allPlacings.Count == 0)
        {
            _logger.LogWarning(
                "No prize placings found for CompetitionId {CompetitionId} when sending prize invoice email.",
                summary.CompetitionId);
            return true;
        }

        var totalPayout = allPlacings.Sum(p => p.Amount);

        // Pro shop / accounts recipient email address from config
        // e.g. AppSettings.ProShopEmail or AppSettings.PrizeInvoice.ProShopEmail
        var recipientEmail = _settings.ProShopEmail;
        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            _logger.LogError("Pro shop / accounts email address is not configured (ProShopEmail).");
            return false;
        }

        // IG member that represents the Pro Shop (provided: 2812)
        var accountantId = _settings.AccountantMemberId;

        var subject = BuildSubject(summary, request.InvoiceId);
        var bodyHtml = BuildBodyHtml(
            summary,
            allPlacings,
            totalPayout,
            request.InvoiceId,
            request.TicketId,
            request.InvoiceUrl
        );

        var sendCommand = new SendMemberEmailCommand(accountantId)
        {
            RecipientEmail = recipientEmail,
            Subject = subject,
            FromName = "Burton-on-Trent Golf Club",
            FromAddress = _settings.Invoices.EmailsSentFrom,
            BodyHtml = bodyHtml
        };

        // TODO: remove this
        sendCommand.RecipientEmail = "simon@maraboustork.co.uk";

        var sent = await _mediator.Send(sendCommand, cancellationToken);

        if (!sent)
        {
            _logger.LogWarning(
                "Failed to send competition prize invoice email {InvoiceId} for CompetitionId {CompetitionId} to {Recipient}.",
                request.InvoiceId,
                summary.CompetitionId,
                recipientEmail);
        }
        else
        {
            _logger.LogInformation(
                "Sent competition prize invoice email {InvoiceId} for CompetitionId {CompetitionId} to {Recipient}.",
                request.InvoiceId,
                summary.CompetitionId,
                recipientEmail);
        }

        return sent;
    }

    private static string BuildSubject(CompetitionWinningsSummaryDto summary, string invoiceId)
    {
        var culture = CultureInfo.GetCultureInfo("en-GB");
        var dateText = summary.CompetitionDate.ToString("d MMM yyyy", culture);

        return !string.IsNullOrWhiteSpace(invoiceId)
            ? $"Prize invoice {invoiceId} – {summary.CompetitionName} ({dateText})"
            : $"Prize allocation – {summary.CompetitionName} ({dateText})";
    }

    private static string BuildBodyHtml(
        CompetitionWinningsSummaryDto summary,
        IReadOnlyCollection<PlacingDto> placings,
        decimal totalPayout,
        string invoiceId,
        string? ticketId,
        string? invoiceUrl)
    {
        var culture = CultureInfo.GetCultureInfo("en-GB");
        var dateText = summary.CompetitionDate.ToString("d MMM yyyy", culture);

        var encodedCompName = WebUtility.HtmlEncode(summary.CompetitionName);
        var encodedDate = WebUtility.HtmlEncode(dateText);
        var encodedInvoiceId = WebUtility.HtmlEncode(invoiceId ?? string.Empty);
        var encodedInvoiceUrl = string.IsNullOrWhiteSpace(invoiceUrl)
            ? null
            : WebUtility.HtmlEncode(invoiceUrl);

        var sb = new StringBuilder();

        sb.Append(@"<div id=""editWrapper"" class=""editWrapper"">");

        // Header with club logo
        sb.Append(@"
            <div id=""header"" class=""header"">
                <p style=""text-align: center;"">
                    <img class=""originalHeight:149 originalWidth:148 size:custom""
                         src=""http://www.botgc.co.uk/images/thumbs/sites/burtonontrent/100x100/0/Golf%20Club%20Badge.PNG""
                         alt=""""
                         width=""68"" height=""68"" />
                </p>
            </div>");

        sb.Append(@"<div id=""content"" class=""content contentContainer"" style=""clear: both;"">");

        sb.Append("<p>Dear Pro Shop,</p>");

        sb.Append("<p>");
        sb.Append("Please find below the prize allocation for ");
        sb.Append($"<strong>{encodedCompName}</strong> played on <strong>{encodedDate}</strong>.");
        if (!string.IsNullOrWhiteSpace(invoiceId))
        {
            sb.Append(" This relates to ");
            sb.Append($"<strong>invoice reference {encodedInvoiceId}</strong>.");
        }
        sb.Append("</p>");

        if (!string.IsNullOrWhiteSpace(encodedInvoiceUrl))
        {
            sb.Append("<p>");
            sb.Append("You can also download the full PDF invoice here: ");
            sb.Append($@"<a href=""{encodedInvoiceUrl}"" target=""_blank"">View invoice</a>.");
            sb.Append("</p>");
        }

        if (!string.IsNullOrWhiteSpace(ticketId))
        {
            sb.Append("<p>");
            sb.Append("For internal reference, this payment has been logged on our Monday board ");
            sb.Append($"(item ID <strong>{WebUtility.HtmlEncode(ticketId)}</strong>).");
            sb.Append("</p>");
        }

        sb.Append(@"
            <p>The following amounts should be credited to players' prize accounts:</p>
            <table style=""border-collapse:collapse;width:100%;max-width:800px;font-size:13px;"">
              <thead>
                <tr>
                  <th style=""border-bottom:1px solid #ccc;text-align:left;padding:4px 6px;"">Division</th>
                  <th style=""border-bottom:1px solid #ccc;text-align:left;padding:4px 6px;"">Position</th>
                  <th style=""border-bottom:1px solid #ccc;text-align:left;padding:4px 6px;"">Player</th>
                  <th style=""border-bottom:1px solid #ccc;text-align:right;padding:4px 6px;"">Amount</th>
                </tr>
              </thead>
              <tbody>");

        foreach (var division in summary.Divisions.OrderBy(d => d.DivisionNumber))
        {
            var divName = string.IsNullOrWhiteSpace(division.DivisionName)
                ? $"Division {division.DivisionNumber}"
                : division.DivisionName;

            var encodedDivName = WebUtility.HtmlEncode(divName);

            foreach (var placing in division.Placings
                         .Where(p => p.Amount > 0)
                         .OrderBy(p => p.Position))
            {
                var ordinal = ToOrdinal(placing.Position);
                var playerName = string.IsNullOrWhiteSpace(placing.CompetitorName)
                    ? placing.CompetitorId
                    : placing.CompetitorName;

                var encodedPlayer = WebUtility.HtmlEncode(playerName ?? string.Empty);
                var amountText = placing.Amount.ToString("£0.00", culture);

                sb.Append(@"
                <tr>
                  <td style=""border-bottom:1px solid #eee;padding:4px 6px;"">")
                  .Append(encodedDivName)
                  .Append(@"</td>
                  <td style=""border-bottom:1px solid #eee;padding:4px 6px;"">")
                  .Append(WebUtility.HtmlEncode(ordinal))
                  .Append(@"</td>
                  <td style=""border-bottom:1px solid #eee;padding:4px 6px;"">")
                  .Append(encodedPlayer)
                  .Append(@"</td>
                  <td style=""border-bottom:1px solid #eee;padding:4px 6px;text-align:right;"">")
                  .Append(WebUtility.HtmlEncode(amountText))
                  .Append(@"</td>
                </tr>");
            }
        }

        sb.Append(@"
              </tbody>
            </table>");

        var totalText = totalPayout.ToString("£0.00", culture);

        sb.Append("<p>");
        sb.Append("The total amount to be transferred from the Club competition account to the Pro Shop prize account is ");
        sb.Append($"<strong>{WebUtility.HtmlEncode(totalText)}</strong>.");
        sb.Append("</p>");

        sb.Append(@"
            <p>
                If you have any queries regarding this competition or the allocations above,
                please contact the Club office.
            </p>
            <p>
                Best regards,<br/>
                Burton-on-Trent Golf Club
            </p>");

        sb.Append("</div>"); // content
        sb.Append("</div>"); // wrapper

        return sb.ToString();
    }

    private static string ToOrdinal(int n)
    {
        if (n <= 0) return n.ToString(CultureInfo.InvariantCulture);

        var rem100 = n % 100;
        if (rem100 is 11 or 12 or 13) return $"{n}th";

        return (n % 10) switch
        {
            1 => $"{n}st",
            2 => $"{n}nd",
            3 => $"{n}rd",
            _ => $"{n}th"
        };
    }
}
