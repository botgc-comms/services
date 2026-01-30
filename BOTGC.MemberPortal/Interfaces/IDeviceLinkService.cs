using System.Security.Claims;

namespace BOTGC.MemberPortal.Interfaces;

public interface IDeviceLinkService
{
    string Create(ClaimsPrincipal user, string returnUrl);
    (ClaimsPrincipal Principal, string ReturnUrl)? Redeem(string token);
}