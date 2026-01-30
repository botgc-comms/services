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
                Id = 3194,
                Username = "3194",
                DisplayName = "Benji Toone",
                FirstName = "Benji",
                LastName = "Toone",
                Category = "JuniorCadet"
            },
            new AppUser
            {
                Id = 1,
                Username = "admin",
                DisplayName = "Admin User",
                FirstName = "Admin",
                LastName = "User", 
                Category = "Admin"
            }
        };
    }

    public Task<AppUser?> ValidateCredentialsAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (username == "3194" && password == "3194")
        {
            var user = _users.Single(x => x.Username == "3194");
            return Task.FromResult<AppUser?>(user);
        }

        if (username == "admin" && password == "admin")
        {
            var user = _users.Single(x => x.Username == "admin");
            return Task.FromResult<AppUser?>(user);
        }

        return Task.FromResult<AppUser?>(null);
    }
}
