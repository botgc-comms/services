using BOTGC.API.Common;
using BOTGC.API.Interfaces;
using Services.Common;
using Services.Dto;
using Services.Interfaces;
using Services.Services;
using System.Reflection.Metadata.Ecma335;

namespace BOTGC.API.Services.ReportServices
{
    public class MembershipReportingService : IMembershipReportingService
    {
        private readonly IDataService _dataService;
        private readonly ILogger<MembershipReportingService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TrophyDataStore"/> class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        /// <param name="trophyFiles">File-based storage for trophy metadata.</param>
        public MembershipReportingService(ILogger<MembershipReportingService> logger, IDataService dataService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
        }

        public async Task<MembershipReportDto> GetManagementReport()
        {
            var members = (await _dataService.GetMembershipReportAsync()).Where(m => m.MemberId != 0).ToList();
            
            var counts = members.GroupBy(m => m.MembershipCategory).Select(g => g.Count()).ToList();

            var report = new MembershipReportDto();
            var today = DateTime.UtcNow;

            // Determine the current and previous financial years
            int currentFinancialYearStart = today.Month >= 4 ? today.Year : today.Year - 1;
            DateTime startOfPreviousFinancialYear = new DateTime(currentFinancialYearStart - 1, 4, 1);
            DateTime endOfPreviousFinancialYear = new DateTime(currentFinancialYearStart, 3, 31);
            DateTime startOfCurrentFinancialYear = new DateTime(currentFinancialYearStart, 4, 1);
            DateTime endOfCurrentFinancialYear = new DateTime(currentFinancialYearStart + 1, 3, 31);

            // Get all of the events that have taken place up to the start of the last financial year
            var memberEvents = await _dataService.GetMembershipEvents(startOfPreviousFinancialYear, today);

            // Start with todays results
            var todaysResults = GetReportEntry(today, members);
            report.DataPoints.Add(todaysResults);

            // Process each day backwards from yesterday to the start of the previous financial year
            for (var currentDate = today.AddDays(-1); currentDate >= startOfPreviousFinancialYear; currentDate = currentDate.AddDays(-1))
            {
                // Get events that occurred on this date
                var eventsOnThisDay = memberEvents.Where(e => e.DateOfChange.HasValue && e.DateOfChange.Value.Date == currentDate.Date).ToList();

                // Reverse each event to rewind the member data
                foreach (var memberEvent in eventsOnThisDay.OrderByDescending(me => me.ChangeIndex))
                {
                    var member = members.Find(m => m.MemberId == memberEvent.MemberId);

                    if (member != null)
                    {
                        // Verify the current state before rewinding
                        if (member.MembershipCategory == memberEvent.ToCategory)
                        {
                            // Reverse the category change
                            member.MembershipCategory = memberEvent.FromCategory;
                        }
                        else
                        {
                            _logger.LogWarning($"Expected current category to be {memberEvent.ToCategory} but found {member.MembershipCategory} for member id {member.MemberId} on {currentDate.ToString("yyyy-MM-dd")}");
                        }

                        // If there was a status change, reverse that too
                        if (member.MembershipStatus == memberEvent.ToStatus)
                        {
                            member.MembershipStatus = memberEvent.FromStatus;
                        }
                        else
                        {
                            _logger.LogWarning($"Expected current stauts to be {memberEvent.ToStatus} but found {member.MembershipStatus} for member id {member.MemberId} on {currentDate.ToString("yyyy-MM-dd")}");
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Exoected to find a member with id {memberEvent.MemberId} but no member was found");
                    }
                }

                // Update the playing status of all members
                foreach(var member in members)
                    MembershipHelper.SetPrimaryCategory(member, currentDate);

                // Compute and store the statistics for this day after applying reversals
                var dailyReportEntry = GetReportEntry(currentDate, members);
                report.DataPoints.Insert(0, dailyReportEntry); // Insert at the beginning to keep chronological order
            }

            EnsureFullYearData(report.DataPoints, endOfCurrentFinancialYear);
            ApplyGrowthTargets(report.DataPoints, startOfPreviousFinancialYear, endOfPreviousFinancialYear);
            ApplyGrowthTargets(report.DataPoints, startOfCurrentFinancialYear, endOfCurrentFinancialYear);

            report.DataPointsCsv = ConvertToCsv(report.DataPoints);

            return report;
        }

        public string ConvertToCsv(IEnumerable<MembershipReportEntryDto> entries, string delimiter = ",")
        {
            var csvLines = new List<string>
            {
                "Date,PlayingMembers,NonPlayingMembers,TargetPlayingMembers" 
            };

            csvLines.AddRange(entries.Select(e => $"{e.Date:yyyy-MM-dd}{delimiter}{e.PlayingMembers}{delimiter}{e.NonPlayingMembers},{delimiter}{e.TargetPlayingMembers}"));

            return string.Join("\n", csvLines);
        }

        private MembershipReportEntryDto GetReportEntry(DateTime date, List<MemberDto> members)
        {
            var playingMembers = members.Where(m => m.PrimaryCategory == MembershipPrimaryCategories.PlayingMember).ToList();
            var nonPlayingMembers = members.Where(m => m.PrimaryCategory == MembershipPrimaryCategories.NonPlayingMember).ToList();

            return new MembershipReportEntryDto
            {
                Date = date,
                PlayingMembers = playingMembers.Count,
                NonPlayingMembers = nonPlayingMembers.Count,
            };
        }

        private void EnsureFullYearData(List<MembershipReportEntryDto> dataPoints, DateTime endOfCurrentFinancialYear)
        {
            DateTime lastRecordedDate = dataPoints.Last().Date;
            for (var currentDate = lastRecordedDate.AddDays(1); currentDate <= endOfCurrentFinancialYear; currentDate = currentDate.AddDays(1))
            {
                dataPoints.Add(new MembershipReportEntryDto
                {
                    Date = currentDate,
                    PlayingMembers = dataPoints.Last().PlayingMembers,
                    NonPlayingMembers = dataPoints.Last().NonPlayingMembers,
                    TargetPlayingMembers = 0
                });
            }
        }

        private void ApplyGrowthTargets(List<MembershipReportEntryDto> dataPoints, DateTime start, DateTime end)
        {
            var startEntry = dataPoints.FirstOrDefault(dp => dp.Date.Date == start.Date);
            double startValue = startEntry?.PlayingMembers ?? 0;
            double targetValue = startValue * 1.05;
            int totalDays = (end - start).Days;

            for (var i = 0; i <= totalDays; i++)
            {
                var currentDate = start.AddDays(i);
                var entry = dataPoints.FirstOrDefault(dp => dp.Date.Date == currentDate.Date);
                if (entry != null)
                {
                    entry.TargetPlayingMembers = startValue + (targetValue - startValue) * (i / (double)totalDays);
                }
            }
        }
    }
}
