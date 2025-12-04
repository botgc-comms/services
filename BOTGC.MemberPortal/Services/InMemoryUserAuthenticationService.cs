using BOTGC.MemberPortal.Interfaces;
using BOTGC.MemberPortal.Models;

namespace BOTGC.MemberPortal.Services;

public sealed class InMemoryUserAuthenticationService : IUserAuthenticationService
{
    private readonly List<AppUser> _users;

    public InMemoryUserAuthenticationService()
    {
        _users = new List<AppUser>
        {
            new AppUser
            {
                Id = 3677,
                Username = "3677",
                DisplayName = "Seth Parsons"
            }
        };
    }

    public Task<AppUser?> ValidateCredentialsAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        // Hard-coded accepted credentials for now
        const string acceptedUsername = "3677";
        const string acceptedPassword = "3677";

        if (username == acceptedUsername && password == acceptedPassword)
        {
            var user = _users.Single(x => x.Username == acceptedUsername);
            return Task.FromResult<AppUser?>(user);
        }

        return Task.FromResult<AppUser?>(null);
    }
}
