using BOTGC.API.Services;
using Services.Common;
using Services.Dto;
using Services.Interfaces;
using Services.Services;
using Services.Services.BackgroundServices;
using Services.Services.CompetitionProcessors;
using System.Net;

namespace Services.Extensions
{
    public static class IGSupportExtensions
    {
        public static IServiceCollection AddIGSupport(this IServiceCollection services)
        {
            var cookieContainer = new CookieContainer();

            var httpHandler = new HttpClientHandler
            {
                CookieContainer = cookieContainer,
                UseCookies = true,
                AllowAutoRedirect = true
            };

            var httpClient = new HttpClient(httpHandler);

            services.AddSingleton(cookieContainer);
            services.AddSingleton(httpClient);
            services.AddSingleton<IGSessionService>(); 
            services.AddHostedService(provider => provider.GetRequiredService<IGSessionService>()); 

            //services.AddHttpClient<IGLoginService>()
            //    .ConfigurePrimaryHttpMessageHandler(sp =>
            //    {
            //        var cookieContainer = sp.GetRequiredService<CookieContainer>();
            //        return new HttpClientHandler
            //        {
            //            CookieContainer = cookieContainer,
            //            UseCookies = true,
            //            AllowAutoRedirect = true
            //        };
            //    });

            //services.AddHttpClient<IReportService>()
            //    .ConfigurePrimaryHttpMessageHandler(sp =>
            //    {
            //        var cookieContainer = sp.GetRequiredService<CookieContainer>();
            //        return new HttpClientHandler
            //        {
            //            CookieContainer = cookieContainer,
            //            UseCookies = true,
            //            AllowAutoRedirect = true
            //        };
            //    });

            services.AddSingleton<IGLoginService>();
            services.AddSingleton<IReportParser<MemberDto>, IGMemberReportParser>();
            services.AddSingleton<IReportParser<RoundDto>, IGRoundReportParser>();
            services.AddSingleton<IReportParser<PlayerIdLookupDto>, IGPlayerIdLookupReportParser>();
            services.AddSingleton<IReportParser<ScorecardDto>, IGScorecardReportParser>();

            services.AddTransient<JuniorEclecticCompetitionProcessor>();

            services.AddSingleton<ICompetitionTaskQueue, CompetitionTaskQueue>();
            services.AddSingleton<ICompetitionProcessorResolver, CompetitionProcessorResolver>();
            
            services.AddHostedService<CompetitionBackgroundService>();


            services.AddSingleton<IDataService, IGDataService>();

            return services;
        }
    }
}
