using Services.Common;
using Services.Interfaces;
using Services.Services;
using System.Net;

namespace Services.Extensions
{
    public static class IGSupportExtensions
    {
        public static IServiceCollection AddIGSupport(this IServiceCollection services)
        {
            services.AddSingleton<CookieContainer>(); // Shared Cookie Container
            services.AddHttpClient<IGLoginService>()
                .ConfigurePrimaryHttpMessageHandler(sp =>
                {
                    var cookieContainer = sp.GetRequiredService<CookieContainer>();
                    return new HttpClientHandler
                    {
                        CookieContainer = cookieContainer,
                        UseCookies = true,
                        AllowAutoRedirect = true
                    };
                });

            services.AddHttpClient<IReportService>()
                .ConfigurePrimaryHttpMessageHandler(sp =>
                {
                    var cookieContainer = sp.GetRequiredService<CookieContainer>();
                    return new HttpClientHandler
                    {
                        CookieContainer = cookieContainer,
                        UseCookies = true,
                        AllowAutoRedirect = true
                    };
                });

            services.AddSingleton<IGLoginService>();
            services.AddSingleton<IGMemberReportParser>();

            services.AddSingleton<IReportService, IGReportsService>();

            return services;
        }
    }
}
