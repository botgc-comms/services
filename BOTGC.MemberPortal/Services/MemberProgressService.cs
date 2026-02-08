using BOTGC.MemberPortal.Interfaces;
using BOTGC.MemberPortal.Models;

namespace BOTGC.MemberPortal.Services;

public sealed class MemberProgressService : IMemberProgressService
{
    private readonly HttpClient _client;

    public MemberProgressService(IHttpClientFactory httpClientFactory)
    {
        _client = httpClientFactory.CreateClient("Api");
    }

    public async Task<MemberProgress?> GetMemberProgressAsync(
        int memberId,
        CancellationToken cancellationToken = default)
    {
        if (memberId <= 0)
        {
            return null;
        }

        var response = await _client.GetAsync(
            $"api/events/members/{memberId}/junior-progress/latest?take=250",
            cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var evt = await response.Content.ReadFromJsonAsync<JuniorProgressChangedEventDto>(cancellationToken: cancellationToken);

        if (evt is null)
        {
            return null;
        }

        var percentComplete = evt.PercentComplete;
        if (percentComplete < 0d) percentComplete = 0d;
        if (percentComplete > 100d) percentComplete = 100d;

        var percentRounded = (int)Math.Round(percentComplete, MidpointRounding.AwayFromZero);
        if (percentRounded < 0) percentRounded = 0;
        if (percentRounded > 100) percentRounded = 100;

        return new MemberProgress
        {
            EventId = evt.EventId,
            MemberId = evt.MemberId,
            OccurredAtUtc = evt.OccurredAtUtc,
            Category = string.IsNullOrWhiteSpace(evt.Category) ? "Unknown" : evt.Category,
            PercentComplete = percentComplete,
            Pillars = evt.Pillars is null || evt.Pillars.Count == 0
                ? Array.Empty<MemberProgressPillar>()
                : evt.Pillars.Select(p => new MemberProgressPillar
                {
                    PillarKey = p.PillarKey,
                    WeightPercent = p.WeightPercent,
                    MilestoneAchieved = p.MilestoneAchieved,
                    PillarCompletionFraction = p.PillarCompletionFraction,
                    ContributionPercent = p.ContributionPercent
                }).ToList(),
            PreviousSignature = evt.PreviousSignature,
            NewSignature = evt.NewSignature,
            ProgressPercentRounded = percentRounded
        };
    }
}
