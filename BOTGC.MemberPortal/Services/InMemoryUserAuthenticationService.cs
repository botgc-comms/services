using BOTGC.MemberPortal.Interfaces;
using BOTGC.MemberPortal.Models;

namespace BOTGC.MemberPortal.Services;

public sealed class InMemoryUserAuthenticationService : IUserAuthenticationService
{
    private readonly List<AppUser> _users;
    private readonly Dictionary<string, AppUser> _usersByUsername;

    public InMemoryUserAuthenticationService()
    {
        _users = new List<AppUser>
        {
            new AppUser
            {
                Id = 3746,
                Username = "3746",
                DisplayName = "Jo Test",
                FirstName = "Jo",
                LastName = "Test",
                Category = "Junior Cadet",
                Children = Array.Empty<AppAuthChildLink>()
            },
            new AppUser
            {
                Id = 3747,
                Username = "3747",
                DisplayName = "Josephine Test",
                FirstName = "Josephine",
                LastName = "Test",
                Category = "Junior Course Cadet",
                Children = Array.Empty<AppAuthChildLink>()
            },
            new AppUser
            {
                Id = 3194,
                Username = "3194",
                DisplayName = "Benji Toone",
                FirstName = "Benji",
                LastName = "Toone",
                Category = "Junior Cadet",
                Children = Array.Empty<AppAuthChildLink>()
            },
            new AppUser
            {
                Id = 1,
                Username = "admin",
                DisplayName = "Admin User",
                FirstName = "Admin",
                LastName = "User",
                Category = "Admin",
                Children = Array.Empty<AppAuthChildLink>()
            },
            new AppUser
            {
                Id = 1590,
                Username = "1590",
                DisplayName = "Adam Toone",
                FirstName = "Adam",
                LastName = "Toone",
                Category = "Family",
                Children = new[]
                {
                    new AppAuthChildLink
                    {
                        MembershipId = 3194,
                        Name = "Benji Toone",
                        Category = "Junior Cadet"
                    }
                }
            },
            new AppUser
            {
                Id = 3104,
                Username = "3104",
                DisplayName = "Simon Parsons",
                FirstName = "Simon",
                LastName = "Parsons",
                Category = "Family",
                Children = new[]
                {
                    new AppAuthChildLink
                    {
                        MembershipId = 3746,
                        Name = "Jo Test",
                        Category = "Junior Cadet"
                    },
                    new AppAuthChildLink
                    {
                        MembershipId = 3747,
                        Name = "Josephine Test",
                        Category = "Junior Course Cadet"
                    }
                }
            }
        };

        _usersByUsername = _users.ToDictionary(x => x.Username, StringComparer.OrdinalIgnoreCase);
    }

    public Task<AppUser?> ValidateCredentialsAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return Task.FromResult<AppUser?>(null);
        }

        if (!string.Equals(username.Trim(), password.Trim(), StringComparison.Ordinal))
        {
            return Task.FromResult<AppUser?>(null);
        }

        _usersByUsername.TryGetValue(username.Trim(), out var user);

        return Task.FromResult<AppUser?>(user);
    }
}
