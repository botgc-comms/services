using Microsoft.AspNetCore.Builder;

namespace BOTGC.MembershipApplication
{
    public static class ContentSecurityPolicyExtensions
    {
        public static ContentSecurityPolicyBuilder UseContentSecurityPolicy(this IApplicationBuilder app)
        {
            return new ContentSecurityPolicyBuilder(app);
        }
    }
}
