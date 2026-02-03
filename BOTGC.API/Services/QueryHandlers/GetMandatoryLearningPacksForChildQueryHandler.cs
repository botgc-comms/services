using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using BOTGC.API.Services.Queries;
using MediatR;

namespace BOTGC.API.Services.QueryHandlers;

public sealed class GetMandatoryLearningPacksForChildQueryHandler(
    IMediator mediator,
    ILearningPackRequirementResolver requirementResolver)
    : QueryHandlerBase<GetMandatoryLearningPacksForChildQuery, MandatoryLearningPacksForChildDto>
{
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    private readonly ILearningPackRequirementResolver _requirementResolver = requirementResolver ?? throw new ArgumentNullException(nameof(requirementResolver));

    public override async Task<MandatoryLearningPacksForChildDto> Handle(
        GetMandatoryLearningPacksForChildQuery request,
        CancellationToken cancellationToken)
    {
        var child = await _mediator.Send(new GetMemberQuery { MemberNumber = request.ChildMemberId }, cancellationToken);
        var childCategory = (child?.MembershipCategory ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(childCategory))
        {
            return new MandatoryLearningPacksForChildDto(request.ChildMemberId, string.Empty, Array.Empty<string>());
        }

        var mandatory = await _requirementResolver.GetMandatoryPackIdsForChildCategoryAsync(childCategory, cancellationToken);

        var normalised = mandatory
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new MandatoryLearningPacksForChildDto(request.ChildMemberId, childCategory, normalised);
    }
}