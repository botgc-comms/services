namespace BOTGC.MemberPortal.Models;

public sealed class AppUser
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public IReadOnlyCollection<AppAuthChildLink> Children { get; set; } = Array.Empty<AppAuthChildLink>();
}

