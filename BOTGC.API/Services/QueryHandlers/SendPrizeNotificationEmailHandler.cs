using BOTGC.API.Services.Queries;
using MediatR;
using System.Globalization;
using System.Net;

namespace BOTGC.API.Services.QueryHandlers;

public sealed class SendPrizeNotificationEmailHandler(
    ILogger<SendPrizeNotificationEmailHandler> logger,
    IMediator mediator
) : QueryHandlerBase<SendPrizeNotificationEmailCommand, bool>
{
    private readonly ILogger<SendPrizeNotificationEmailHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

    public async override Task<bool> Handle(SendPrizeNotificationEmailCommand request, CancellationToken cancellationToken)
    {
        int playerId;
        if (!int.TryParse(request.PlayerId, out playerId))
        {
            _logger.LogWarning(
                "Invalid PlayerId {PlayerId} when sending prize notification.",
                request.PlayerId);
            return false;
        }   

        var member = await _mediator.Send(
            new GetMemberQuery { PlayerId = playerId },
            cancellationToken);

        if (member == null || string.IsNullOrWhiteSpace(member.Email))
        {
            _logger.LogWarning(
                "No member details or email found for PlayerId {PlayerId} when sending prize notification.",
                request.PlayerId);
            return false;
        }

        var firstName = !string.IsNullOrWhiteSpace(member.Forename)
            ? member.Forename
            : (request.CompetitorName?.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Member");

        var subject = BuildSubject(request);
        var bodyHtml = BuildBodyHtml(firstName, request);

        var sendCommand = new SendMemberEmailCommand(playerId)
        {
            RecipientEmail = member.Email,
            Subject = subject,
            FromName = "Burton-on-Trent Golf Club",
            FromAddress = "comms@botgc.co.uk",
            BodyHtml = bodyHtml
        };

        sendCommand.RecipientEmail = "simon@maraboustork.co.uk";

        var sent = await _mediator.Send(sendCommand, cancellationToken);

        if (!sent)
        {
            _logger.LogWarning(
                "Failed to send prize notification for {Competition} to {Recipient}.",
                request.CompetitionName,
                member.Email);
        }

        return sent;
    }

    private static string BuildSubject(SendPrizeNotificationEmailCommand payload)
    {
        var culture = CultureInfo.GetCultureInfo("en-GB");
        var dateText = payload.CompetitionDate.ToString("d MMMM yyyy", culture);
        return $"Your prize for {payload.CompetitionName} on {dateText}";
    }

    private static string BuildBodyHtml(string firstName, SendPrizeNotificationEmailCommand payload)
    {
        var culture = CultureInfo.GetCultureInfo("en-GB");
        var ordinal = GetOrdinal(payload.Position);
        var dateText = payload.CompetitionDate.ToString("d MMMM yyyy", culture);
        var amountText = $"£{payload.Amount:0.00}";

        var message =
            $"Dear {WebUtility.HtmlEncode(firstName)}, we would like to congratulate you on securing {ordinal} place in the {WebUtility.HtmlEncode(payload.CompetitionName)} on {WebUtility.HtmlEncode(dateText)}. " +
            $"You have won {WebUtility.HtmlEncode(amountText)} which will be credited to your pro shop account shortly.";

        return
            @"<div id=""editWrapper"" class=""editWrapper"">" +
            @"<div id=""header"" class=""header"">" +
            @"<p style=""text-align: center;""><img class=""originalHeight:149 originalWidth:148 size:custom"" src=""http://www.botgc.co.uk/images/thumbs/sites/burtonontrent/100x100/0/Golf%20Club%20Badge.PNG"" alt="""" width=""68"" height=""68"" /></p>" +
            @"</div>" +
            @"<div id=""content"" class=""content contentContainer"" style=""clear: both;"">&nbsp;</div>" +
            @"<div id=""footer"" class=""footer"">" +
            message +
            @"</div>" +
            @"</div>";
    }

    private static string GetOrdinal(int position)
    {
        if (position <= 0) return position.ToString(CultureInfo.InvariantCulture);

        var rem100 = position % 100;
        if (rem100 is 11 or 12 or 13) return position + "th";

        return (position % 10) switch
        {
            1 => position + "st",
            2 => position + "nd",
            3 => position + "rd",
            _ => position + "th"
        };
    }
}
