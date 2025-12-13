using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using MediatR;

namespace BOTGC.API.Services.QueryHandlers;

//    #endregion


public sealed class EPOSUpdateBenefitsAccountCommandHandler(
        IEposStore store,
        IMediator mediator,
        Microsoft.Extensions.Options.IOptions<AppSettings> settings,
        Microsoft.Extensions.Logging.ILogger<EPOSUpdateBenefitsAccountCommandHandler> logger)
                : QueryHandlerBase<UpdateBenefitsAccountCommand, bool>
{
    private readonly IEposStore _store = store ?? throw new ArgumentNullException(nameof(store));
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    private readonly Microsoft.Extensions.Logging.ILogger<EPOSUpdateBenefitsAccountCommandHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));

    public override async Task<bool> Handle(UpdateBenefitsAccountCommand request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.Amount), "Subscription amount must be positive.");
        }

        var memberQuery = new GetMemberQuery { MemberNumber = request.MemberId };
        var member = await _mediator.Send(memberQuery, cancellationToken);

        if (member is null)
        {
            throw new InvalidOperationException($"Member with ID {request.MemberId} not found.");
        }

        var categoryCode = member.MembershipCategory;
        var rules = _settings.EposBenefits?.CategoryRules ?? new List<EposBenefitsCategoryRule>();
        var rule = rules.FirstOrDefault(r =>
            string.Equals(r.CategoryCode, categoryCode, StringComparison.OrdinalIgnoreCase));

        if (rule is null || rule.SubscriptionCreditPercentage <= 0m)
        {
            return false;
        }

        var creditAmount = Math.Round(
            request.Amount * (rule.SubscriptionCreditPercentage / 100m),
            2,
            MidpointRounding.AwayFromZero);

        if (creditAmount <= 0m)
        {
            return false;
        }

        var account = await _store.GetAccountAsync(request.MemberId, cancellationToken)
                     ?? await _store.CreateOrGetAccountAsync(
                         request.MemberId,
                         $"{member.Forename} {member.Surname}",
                         cancellationToken);

        var reason = $"Subscription credit {DateTimeOffset.UtcNow:yyyy-MM}";

        await _store.CreditAccountAsync(account, creditAmount, reason, cancellationToken);

        return true;
    }
}