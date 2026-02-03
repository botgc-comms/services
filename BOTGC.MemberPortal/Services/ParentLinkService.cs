// LearningPackServices.cs
using BOTGC.MemberPortal.Interfaces;

namespace BOTGC.MemberPortal.Services;

public sealed class ParentLinkService : IParentLinkService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ParentLinkService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<(string Code, DateTimeOffset ExpiresUtc)> CreateParentLinkAsync(
        int childMembershipId,
        int childMembershipNumber,
        string childFirstName,
        string childSurname,
        string childCategory,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("Api");

        var res = await client.PostAsJsonAsync(
            "api/auth/app/parent-link",
            new AppAuthIssueParentLinkRequest
            {
                ChildMembershipId = childMembershipId,
                ChildMembershipNumber = childMembershipNumber,
                ChildFirstName = childFirstName ?? string.Empty,
                ChildSurname = childSurname ?? string.Empty,
                ChildCategory = childCategory ?? string.Empty
            },
            cancellationToken);

        res.EnsureSuccessStatusCode();

        var body = await res.Content.ReadFromJsonAsync<AppAuthWebSsoResponse>(cancellationToken: cancellationToken);
        if (body == null || string.IsNullOrWhiteSpace(body.Code))
        {
            throw new InvalidOperationException("Parent link response invalid.");
        }

        return (body.Code.Trim(), body.ExpiresUtc);
    }

    private sealed class AppAuthIssueParentLinkRequest
    {
        public int ChildMembershipId { get; set; }
        public int ChildMembershipNumber { get; set; }
        public string ChildFirstName { get; set; } = string.Empty;
        public string ChildSurname { get; set; } = string.Empty;
        public string ChildCategory { get; set; } = string.Empty;
    }

    private sealed class AppAuthWebSsoResponse
    {
        public string Code { get; set; } = string.Empty;
        public DateTimeOffset ExpiresUtc { get; set; }
    }
}