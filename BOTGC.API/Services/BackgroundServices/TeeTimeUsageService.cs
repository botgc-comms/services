using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using MediatR;
using BOTGC.API.Services.Queries;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BOTGC.API.Services.BackgroundServices
{
    public class TeeTimeUsageBackgroundService : BackgroundService
    {
        private const string __CACHE_MEMBERTEESTATS = "Member_Tee_Stats_{from}_{to}";
        private const string __CACHE_MEMBERTEESTATSRESULTS = "Member_Tee_Stats_Results_{from}_{to}";

        private readonly AppSettings _settings;
        private readonly ITeeTimeUsageTaskQueue _taskQueue;
        private readonly ILogger<TeeTimeUsageBackgroundService> _logger;
        private readonly IMediator _mediator;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        private const int BatchSize = 5; 

        public TeeTimeUsageBackgroundService(IOptions<AppSettings> settings,
                                             ILogger<TeeTimeUsageBackgroundService> logger,
                                             ITeeTimeUsageTaskQueue taskQueue,
                                             IMediator mediator,
                                             IServiceScopeFactory serviceScopeFactory)
        {
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _taskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var taskItem = await _taskQueue.DequeueAsync(stoppingToken);
                try
                {
                    await ProcessTaskAsync(taskItem, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing task.");
                }
            }
        }

        private async Task ProcessTaskAsync(TeeTimeUsageTaskItem taskItem, CancellationToken stoppingToken)
        {
            ICacheService? cacheService = null;
            List<MemberTeeStatsDto>? memberStats = null;

            var cacheKey = __CACHE_MEMBERTEESTATS.Replace("{from}", taskItem.FromDate.ToString("yyyyMMdd")).Replace("{to}", taskItem.ToDate.ToString("yyyyMMdd"));
            var resultsKey = __CACHE_MEMBERTEESTATSRESULTS.Replace("{from}", taskItem.FromDate.ToString("yyyyMMdd")).Replace("{to}", taskItem.ToDate.ToString("yyyyMMdd"));

            var completedAlready = false;
            if (!string.IsNullOrEmpty(cacheKey))
            {
                using var scope = _serviceScopeFactory.CreateScope();
                cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

                memberStats = await cacheService!.GetAsync<List<MemberTeeStatsDto>>(cacheKey).ConfigureAwait(false);
                if (memberStats != null && memberStats.Any())
                {
                    completedAlready = true;
                    _logger.LogInformation("Member teetime statistics have already been created for the period {from} to {to}", taskItem.FromDate.ToString("dd-MM-yyyy"), taskItem.ToDate.ToString("dd-MM-yyyy"));
                }
            }

            if (!completedAlready)
            {
                var teeSheets = new List<TeeSheetDto>();
                var allDates = Enumerable.Range(0, (taskItem.ToDate - taskItem.FromDate).Days + 1)
                                         .Select(offset => taskItem.FromDate.AddDays(offset))
                                         .ToList();

                foreach (var dateBatch in allDates.Chunk(BatchSize))
                {
                    var tasks = dateBatch.Select(async date =>
                    {
                        var query = new GetTeeSheetByDateQuery { Date = date };
                        var result = await _mediator.Send(query, stoppingToken);
                        return result;
                    }).ToList();

                    var results = await Task.WhenAll(tasks); 

                    teeSheets.AddRange(results.Where(sheet => sheet != null).Cast<TeeSheetDto>());
                }

                var query = new GetCurrentMembersQuery();
                var currentMembers = await _mediator.Send(query, stoppingToken);

                var currentMemberNames = currentMembers?
                    .GroupBy(m => m.FullName.ToLower())
                    .ToDictionary(g => g.Key, g => g.First());

                memberStats = ProcessTeeSheetData(teeSheets, currentMemberNames!);

                await cacheService.SetAsync(cacheKey!, memberStats, TimeSpan.FromMinutes(_settings.Cache.Default_TTL_Mins)).ConfigureAwait(false);
            }

            var social = memberStats
                    .Where(ms => ms.Membership.MembershipCategory == "SOCL - Social Members")
                    .GroupBy(ms => ms.Year)
                    .Select(g => new { Year = g.Key, Members = g.OrderByDescending(m => m.TotalRounds).Select(m => new { Name = m.Membership.FullName, Rounds = m.TotalRounds }).ToList() }).ToList();


            var report = memberStats
                .GroupBy(ms => new { Year = ms.Year!, Group = ms.Group! })
                .Select(g => GetCounts(memberStats, g.Key.Group, g.Key.Year))
                .ToList();

            await cacheService.SetAsync(resultsKey!, report, TimeSpan.FromMinutes(_settings.Cache.Default_TTL_Mins)).ConfigureAwait(false);
        }

        private record MemberCountResult(int Count, string Members);

        private Dictionary<string, MemberCountResult> GetCounts(List<MemberTeeStatsDto> memberStats, string group, int year)
        {
            return memberStats
                .Where(ms => ms.Group == group && ms.Year == year)
                .GroupBy(ms => ms.Membership.MembershipCategory)
                .ToDictionary(
                    g => $"{group}_{year}_{g.Key}", 
                    g => new MemberCountResult(g.Count(), string.Join(",", g.OrderBy(m => m.Membership.FullName).Select(m => $"{m.Membership.FullName}({m.Membership.MemberNumber})").ToArray())));
        }

        public List<MemberTeeStatsDto> ProcessTeeSheetData(List<TeeSheetDto> teeSheets, Dictionary<string, MemberDto> currentMembers)
        {
            var memberStats = new Dictionary<(string FullName, int Year), MemberTeeStatsDto>();

            foreach (var teeSheet in teeSheets)
            {
                int year = teeSheet.Date.Year;
                var players = teeSheet.Players.Where(p => currentMembers.Keys.Contains(p.FullName.ToLower()));

                foreach (var playerTeeTimes in players)
                {
                    string fullName = playerTeeTimes.FullName;
                    var membership = currentMembers[fullName.ToLower()];

                    if (!memberStats.ContainsKey((fullName, year)))
                    {
                        memberStats[(fullName, year)] = new MemberTeeStatsDto
                        {
                            Membership = membership,
                            Year = year,
                            TotalRounds = 0,
                            CompetitionRounds = 0,
                            QuietPeriodRounds = 0
                        };
                    }

                    foreach (var booking in playerTeeTimes.TeeTimes)
                    {
                        memberStats[(fullName, year)].TotalRounds++;

                        if (booking.IsCompetitionBooking)
                        {
                            memberStats[(fullName, year)].CompetitionRounds++;
                        }

                        if (IsQuietPeriod(booking.Time))
                        {
                            memberStats[(fullName, year)].QuietPeriodRounds++;
                        }
                    }
                }
            }

            return memberStats.Values.Select(m =>
            {
                m.Group = ClassifyMember(m);
                return m;
            }).ToList();
        }


        private bool IsQuietPeriod(DateTime teeTime)
        {
            // Define summer and winter start dates
            DateTime summerStart = new DateTime(teeTime.Year, 4, 1);
            DateTime summerEnd = new DateTime(teeTime.Year, 9, 30);

            bool isSummer = teeTime.Date >= summerStart && teeTime.Date <= summerEnd;

            switch (teeTime.DayOfWeek)
            {
                case DayOfWeek.Monday:
                case DayOfWeek.Tuesday:
                    return teeTime.TimeOfDay >= TimeSpan.FromHours(12); // 12:00 noon onwards

                case DayOfWeek.Wednesday:
                    return false; // No play allowed

                case DayOfWeek.Thursday:
                case DayOfWeek.Friday:
                    return teeTime.TimeOfDay >= TimeSpan.FromHours(9); // 9:00 AM onwards

                case DayOfWeek.Saturday:
                    return isSummer ? teeTime.TimeOfDay >= GetLastCompetitionTeeTime(teeTime) : false;

                case DayOfWeek.Sunday:
                    return teeTime.TimeOfDay >= TimeSpan.FromHours(12); // 12:00 noon onwards

                default:
                    return false;
            }
        }

        // Placeholder method for determining last competition tee time on a Saturday
        private TimeSpan GetLastCompetitionTeeTime(DateTime teeTime)
        {
            // This would ideally fetch the actual last competition tee time for that date
            return TimeSpan.FromHours(14); // Example: Assume competitions end at 2 PM
        }

        private string ClassifyMember(MemberTeeStatsDto member)
        {
            if (member.TotalRounds <= 20 && member.CompetitionRounds == 0 && member.QuietPeriodPercentage >= 90)
                return "Goldilocks";

            if (member.TotalRounds <= 25 && member.CompetitionPercentage <= 10 && member.QuietPeriodPercentage >= 75)
                return "Amber";

            return "Other";
        }
    }
}
