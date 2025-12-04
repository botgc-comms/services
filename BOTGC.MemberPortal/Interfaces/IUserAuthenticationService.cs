using BOTGC.MemberPortal.Models;

namespace BOTGC.MemberPortal.Interfaces;

public interface IUserAuthenticationService
{
    Task<AppUser?> ValidateCredentialsAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default);
}
