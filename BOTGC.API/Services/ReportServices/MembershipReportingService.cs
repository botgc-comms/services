using BOTGC.API.Common;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;

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
            var members = (await _dataService.GetMembershipReportAsync()).Where(m => m.MemberNumber != 0).ToList();

            var counts = members.GroupBy(m => m.MembershipCategory).Select(g => g.Count()).ToList();

            var report = new MembershipReportDto();
            var today = DateTime.UtcNow;

            var dataPoints = new List<MembershipReportEntryDto>();

            report.Anomalies = IdentifyAnomalies(members);

            // Determine the current and previous financial years
            int currentSubsYearStart = today.Month >= 4 ? today.Year : today.Year - 1;
            DateTime startOfPreviousSubsYear = new DateTime(currentSubsYearStart - 1, 4, 1);
            DateTime endOfPreviousSubsYear = new DateTime(currentSubsYearStart, 3, 31);
            DateTime startOfCurrentSubsYear = new DateTime(currentSubsYearStart, 4, 1);
            DateTime endOfCurrentSubsYear = new DateTime(currentSubsYearStart + 1, 3, 31);

            int currentFinancialYearStart = today.Month >= 10 ? today.Year : today.Year - 1;
            DateTime startOfPreviousFinancialYear = new DateTime(currentFinancialYearStart - 1, 10, 1);
            DateTime endOfPreviousFinancialYear = new DateTime(currentFinancialYearStart, 9, 30);
            DateTime startOfCurrentFinancialYear = new DateTime(currentFinancialYearStart, 10, 1);
            DateTime endOfCurrentFinancialYear = new DateTime(currentFinancialYearStart + 1, 9, 30);

            // Get all of the events that have taken place up to the start of the last financial year
            var memberEvents = await _dataService.GetMembershipEvents(startOfPreviousSubsYear.AddDays(-1), today);

            // Start with todays results
            var todaysResults = GetReportEntry(today, members);
            dataPoints.Add(todaysResults);

            var monthlySnapshots = new Dictionary<DateTime, MembershipSnapshotDto>();

            var previousDayGroupings = members
                .Where(m => m.IsActive == true)
                .ToDictionary(m => m.MemberNumber, m => m.MembershipCategoryGroup);

            // Process each day backwards from yesterday to the start of the previous financial year
            for (var currentDate = today.AddDays(-1); currentDate >= startOfPreviousSubsYear.AddDays(-1); currentDate = currentDate.AddDays(-1))
            {
                // Get events that occurred on this date
                var eventsOnThisDay = memberEvents.Where(e => e.DateOfChange.HasValue && e.DateOfChange.Value.Date == currentDate.Date).ToList();

                // Reverse each event to rewind the member data
                foreach (var memberEvent in eventsOnThisDay.OrderByDescending(me => me.ChangeIndex))
                {
                    var member = members.Find(m => m.MemberNumber == memberEvent.MemberId);

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
                            _logger.LogWarning($"Expected current category to be {memberEvent.ToCategory} but found {member.MembershipCategory} for member id {member.MemberNumber} on {currentDate.ToString("yyyy-MM-dd")}");
                        }

                        // If there was a status change, reverse that too
                        if (member.MembershipStatus == memberEvent.ToStatus)
                        {
                            member.MembershipStatus = memberEvent.FromStatus;
                        }
                        else
                        {
                            _logger.LogWarning($"Expected current stauts to be {memberEvent.ToStatus} but found {member.MembershipStatus} for member id {member.MemberNumber} on {currentDate.ToString("yyyy-MM-dd")}");
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Expected to find a member with id {memberEvent.MemberId} but no member was found");
                    }
                }

                // Update the playing status of all members
                foreach (var member in members)
                    member.SetPrimaryCategory(currentDate).SetCategoryGroup();

                // Build today's active members with their current group
                var todayActiveGroups = members
                     .Where(m => m.IsActive == true)
                     .ToDictionary(m => m.MemberNumber, m => m.MembershipCategoryGroup);

                var (joinersByGroup, leaversByGroup) = ComputeGroupJoinersAndLeavers(previousDayGroupings, todayActiveGroups);


                // Compute and store the statistics for this day after applying reversals
                var dailyReportEntry = GetReportEntry(currentDate, members);

                dailyReportEntry.DailyJoinersByCategoryGroup = joinersByGroup;
                dailyReportEntry.DailyLeaversByCategoryGroup = leaversByGroup;

                dataPoints.Insert(0, dailyReportEntry);

                if (IsMonthEnd(currentDate) || currentDate.Date == today.Date.AddDays(-1))
                {
                    monthlySnapshots[currentDate.Date] = CreateSnapshot(members, currentDate);
                }

                previousDayGroupings = todayActiveGroups;
            }

            EnsureMonthlyAndQuarterlyStats(report, monthlySnapshots);
            EnsureFullYearData(dataPoints, endOfCurrentSubsYear);
            ApplyGrowthTargets(dataPoints, startOfCurrentFinancialYear, endOfCurrentFinancialYear);

            report.DataPoints = dataPoints.Where(dp => dp.Date >= startOfPreviousSubsYear && dp.Date <= endOfCurrentSubsYear).ToList();
            report.DataPointsCsv = ConvertToCsv(report.DataPoints);

            return report;
        }

        private (Dictionary<string, int> Joiners, Dictionary<string, int> Leavers) ComputeGroupJoinersAndLeavers(Dictionary<int?, string> previousDayGroups,
                                                                                                                 Dictionary<int?, string> todayGroups)
        {
            var joiners = new Dictionary<string, int>();
            var leavers = new Dictionary<string, int>();

            var yesterdayIds = previousDayGroups.Keys;
            var todayIds = todayGroups.Keys;

            var newIds = todayIds.Except(yesterdayIds);
            var exitedIds = yesterdayIds.Except(todayIds);
            var commonIds = todayIds.Intersect(yesterdayIds);

            foreach (var id in newIds)
                AddToCount(joiners, todayGroups[id]);

            foreach (var id in exitedIds)
                AddToCount(leavers, previousDayGroups[id]);

            foreach (var id in commonIds)
            {
                var prevGroup = previousDayGroups[id];
                var nowGroup = todayGroups[id];

                if (!string.Equals(prevGroup, nowGroup, StringComparison.Ordinal))
                {
                    AddToCount(leavers, prevGroup);
                    AddToCount(joiners, nowGroup);
                }
            }

            return (joiners, leavers);
        }

        private MembershipSnapshotDto CreateSnapshot(List<MemberDto> members, DateTime date)
        {
            return new MembershipSnapshotDto
            {
                SnapshotDate = date,
                Members = members.Select(m => new MemberDto(m)).ToList()
            };
        }

        private string ConvertToCsv(IEnumerable<MembershipReportEntryDto> entries, string delimiter = ",")
        {
            var csvLines = new List<string>
            {
                "Date,PlayingMembers,NonPlayingMembers,TargetPlayingMembers,AveragePlayingMemberAge"
            };

            csvLines.AddRange(entries.Select(e =>
                $"{e.Date:yyyy-MM-dd}{delimiter}{e.PlayingMembers}{delimiter}{e.NonPlayingMembers}{delimiter}{e.TargetPlayingMembers}{delimiter}{e.AveragePlayingMembersAge:F2}"
            ));

            return string.Join("\n", csvLines);
        }

        private bool IsMonthEnd(DateTime date)
        {
            return date.Day == DateTime.DaysInMonth(date.Year, date.Month);
        }

        private bool IsQuarterEnd(DateTime date)
        {
            return (date.Month == 3 && date.Day == 31) ||  // Q1 End (Mar 31)
                   (date.Month == 6 && date.Day == 30) ||  // Q2 End (Jun 30)
                   (date.Month == 9 && date.Day == 30) ||  // Q3 End (Sep 30)
                   (date.Month == 12 && date.Day == 31);   // Q4 End (Dec 31)
        }

        private int GetQuarterNumber(DateTime d)
        {
            switch (d.Month)
            {
                case 12: return 1; // Dec → FY Q1
                case 3: return 2;  // Mar → FY Q2
                case 6: return 3;  // Jun → FY Q3
                case 9: return 4;  // Sep → FY Q4
                default: throw new InvalidOperationException("Not a fiscal quarter end");
            }
        }

        private List<MembershipAnomalyDto> IdentifyAnomalies(List<MemberDto> members)
        {
            var anomalies = new List<MembershipAnomalyDto>();
            var detectedDate = DateTime.UtcNow;

            var statusRWithPastLeaveDate = new MembershipAnomalyDto
            {
                DetectedDate = detectedDate,
                Type = MembershipAnomalyType.StatusRWithPastLeaveDate,
                Description = "Members have a status of 'R' but also have a leave date that is in the past",
                Members = new List<MemberSummmaryDto>()
            };

            var statusLWithoutLeaveDate = new MembershipAnomalyDto
            {
                DetectedDate = detectedDate,
                Type = MembershipAnomalyType.StatusLWithoutLeaveDate,
                Description = "Members have a status of 'L' but do not have a leave date",
                Members = new List<MemberSummmaryDto>()
            };

            var statusSWithPastLeaveDate = new MembershipAnomalyDto
            {
                DetectedDate = detectedDate,
                Type = MembershipAnomalyType.StatusSWithPastLeaveDate,
                Description = "Members have a status of 'S' but also have a leave date that is in the past",
                Members = new List<MemberSummmaryDto>()
            };

            var statusWithoutJoinDate = new MembershipAnomalyDto
            {
                DetectedDate = detectedDate,
                Type = MembershipAnomalyType.StatusWithoutJoinDate,
                Description = "Members have status other than 'W' but do not have a join date.",
                Members = new List<MemberSummmaryDto>()
            };

            anomalies.Add(statusRWithPastLeaveDate);
            anomalies.Add(statusLWithoutLeaveDate);
            anomalies.Add(statusSWithPastLeaveDate);
            anomalies.Add(statusWithoutJoinDate);

            foreach (var member in members)
            {
                if ((member.MembershipStatus == "R") && member.LeaveDate.HasValue && member.LeaveDate.Value < detectedDate)
                {
                    statusRWithPastLeaveDate.Members.Add(new MemberSummmaryDto(member));
                }
                if ((member.MembershipStatus == "S") && member.LeaveDate.HasValue && member.LeaveDate.Value < detectedDate)
                {
                    statusSWithPastLeaveDate.Members.Add(new MemberSummmaryDto(member));
                }
                if (member.MembershipStatus == "L" && !member.LeaveDate.HasValue)
                {
                    statusLWithoutLeaveDate.Members.Add(new MemberSummmaryDto(member));
                }
                if (member.MembershipStatus != "W" && !member.JoinDate.HasValue)
                {
                    statusWithoutJoinDate.Members.Add(new MemberSummmaryDto(member));
                }
            }
            return anomalies;
        }

        private MembershipReportEntryDto GetReportEntry(DateTime date, List<MemberDto> members)
        {
            var playingMembers = members.Where(m => m.PrimaryCategory == MembershipPrimaryCategories.PlayingMember && m.IsActive!.Value).ToList();
            var nonPlayingMembers = members.Where(m => m.PrimaryCategory == MembershipPrimaryCategories.NonPlayingMember && m.IsActive!.Value).ToList();

            double averageAge = playingMembers
               .Where(m => m.DateOfBirth.HasValue)
               .Select(m => (date - m.DateOfBirth.Value).TotalDays / 365.25) // Convert days to years
               .DefaultIfEmpty(0)
               .Average();

            // Populate category breakdown
            var playingCategoryBreakdown = members
                .Where(m => m.PrimaryCategory == MembershipPrimaryCategories.PlayingMember && m.IsActive!.Value)
                .GroupBy(m => m.MembershipCategory!)
                .ToDictionary(g => g.Key, g => g.Count());

            var nonPlayingCategoryBreakdown = members
                .Where(m => m.PrimaryCategory == MembershipPrimaryCategories.NonPlayingMember && m.IsActive!.Value)
                .GroupBy(m => m.MembershipCategory!)
                .ToDictionary(g => g.Key, g => g.Count());

            var categoryGroupBreakdown = members
                .Where(m => m.IsActive!.Value)
                .GroupBy(m => m.MembershipCategoryGroup ?? "Other")
                .ToDictionary(g => g.Key, g => g.Count());

            return new MembershipReportEntryDto
            {
                Date = date,
                PlayingMembers = playingMembers.Count,
                NonPlayingMembers = nonPlayingMembers.Count,
                AveragePlayingMembersAge = averageAge,
                PlayingCategoryBreakdown = playingCategoryBreakdown,
                NonPlayingCategoryBreakdown = nonPlayingCategoryBreakdown, 
                CategoryGroupBreakdown = categoryGroupBreakdown
            };
        }

        private void EnsureFullYearData(List<MembershipReportEntryDto> dataPoints, DateTime endOfCurrentFinancialYear)
        {
            DateTime lastRecordedDate = dataPoints.Last().Date;
            var lastEntry = dataPoints.Last();

            var lastCategoryGroupBreakdown = lastEntry.CategoryGroupBreakdown != null
                ? new Dictionary<string, int>(lastEntry.CategoryGroupBreakdown)
                : new Dictionary<string, int>();

            var lastPlayingCategoryBreakdown = lastEntry.PlayingCategoryBreakdown != null
                ? new Dictionary<string, int>(lastEntry.PlayingCategoryBreakdown)
                : new Dictionary<string, int>();

            var lastNonPlayingCategoryBreakdown = lastEntry.NonPlayingCategoryBreakdown != null
                ? new Dictionary<string, int>(lastEntry.NonPlayingCategoryBreakdown)
                : new Dictionary<string, int>();

            for (var currentDate = lastRecordedDate.AddDays(1); currentDate <= endOfCurrentFinancialYear; currentDate = currentDate.AddDays(1))
            {
                dataPoints.Add(new MembershipReportEntryDto
                {
                    Date = currentDate,
                    PlayingMembers = lastEntry.PlayingMembers,
                    NonPlayingMembers = lastEntry.NonPlayingMembers,
                    TargetPlayingMembers = 0,
                    CategoryGroupBreakdown = new Dictionary<string, int>(lastCategoryGroupBreakdown),
                    PlayingCategoryBreakdown = new Dictionary<string, int>(lastPlayingCategoryBreakdown),
                    NonPlayingCategoryBreakdown = new Dictionary<string, int>(lastNonPlayingCategoryBreakdown)
                });
            }
        }

        private void EnsureMonthlyAndQuarterlyStats(MembershipReportDto report, Dictionary<DateTime, MembershipSnapshotDto> monthlySnapshots)
        {
            report.MonthlyStats = new List<MembershipDeltaDto>();

            // **Ensure chronological order**
            var monthEndDates = monthlySnapshots.Keys.OrderBy(d => d).ToList();

            for (int i = 1; i < monthEndDates.Count; i++)
            {
                var fromSnapshot = monthlySnapshots[monthEndDates[i - 1]];
                var toSnapshot = monthlySnapshots[monthEndDates[i]];

                if (fromSnapshot.SnapshotDate >= toSnapshot.SnapshotDate)
                    throw new InvalidOperationException($"Snapshots are out of order! {fromSnapshot.SnapshotDate} is not before {toSnapshot.SnapshotDate}");

                report.MonthlyStats.Add(ComputeMembershipDelta(fromSnapshot, toSnapshot, $"{toSnapshot.SnapshotDate:yyyy-MM}"));
            }

            report.QuarterlyStats = new List<MembershipDeltaDto>();

            // **Ensure quarter-end dates are extracted in correct order**
            var quarterEndDates = monthEndDates.Where(IsQuarterEnd).OrderBy(d => d).ToList();

            for (int i = 1; i < quarterEndDates.Count; i++)
            {
                var fromSnapshot = monthlySnapshots[quarterEndDates[i - 1]];
                var toSnapshot = monthlySnapshots[quarterEndDates[i]];

                if (fromSnapshot.SnapshotDate >= toSnapshot.SnapshotDate)
                    throw new InvalidOperationException($"Quarterly snapshots are out of order! {fromSnapshot.SnapshotDate} is not before {toSnapshot.SnapshotDate}");

                report.QuarterlyStats.Add(ComputeMembershipDelta(fromSnapshot, toSnapshot, GetPeriodDescription(toSnapshot.SnapshotDate)));
            }

            var today = DateTime.UtcNow.Date.AddDays(-1);

            // only if we have a today-snapshot and it's past the last full quarter
            if (monthlySnapshots.ContainsKey(today) && today > quarterEndDates.Last())
            {
                var fromSnapshot = monthlySnapshots[quarterEndDates.Last()];
                var toSnapshot = monthlySnapshots[today];
                var periodDesc = GetPeriodDescription(today); 
                
                report.QuarterlyStats.Add(
                    ComputeMembershipDelta(fromSnapshot, toSnapshot, periodDesc)
                );
            }
        }

        private string GetPeriodDescription(DateTime snapshotDate)
        {
            var sd = snapshotDate;
            int fq = GetQuarterNumber(sd);

            // roll Dec into the next calendar year’s Q1 label
            int labelYear = sd.Month == 12 ? sd.Year + 1 : sd.Year;
            string suffix = (sd.Date == DateTime.UtcNow.Date.AddDays(-1)) ? " (To Date)" : "";

            return $"Q{fq} {labelYear % 100:00}{suffix}";
        }

        private void ApplyGrowthTargets(List<MembershipReportEntryDto> dataPoints, DateTime start, DateTime end)
        {
            // Find the last actual recorded playing members count before the start date
            var lastActualEntry = dataPoints.LastOrDefault(dp => dp.Date.Date < start.Date);
            double startValue = lastActualEntry?.PlayingMembers ?? 0; // ✅ Start from the last actual count
            double targetValue = startValue * 1.05; // 5% growth target
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

        private MembershipDeltaDto ComputeMembershipDelta(MembershipSnapshotDto fromSnapshot, MembershipSnapshotDto toSnapshot, string periodDescription)
        {
            // Filter out future joiners from both snapshots**
            var filteredFromMembers = fromSnapshot.Members
                .Where(m => !m.JoinDate.HasValue || m.JoinDate!.Value <= fromSnapshot.SnapshotDate)
                .ToDictionary(m => m.MemberNumber!.Value);

            var filteredToMembers = toSnapshot.Members
                .Where(m => !m.JoinDate.HasValue || m.JoinDate!.Value <= toSnapshot.SnapshotDate)
                .ToDictionary(m => m.MemberNumber!.Value);

            // Identify new members: They exist in `toSnapshot` but NOT in `fromSnapshot`**
            var newMembers = filteredToMembers.Keys.Except(filteredFromMembers.Keys).ToList();

            // Identify leavers: They now have status "L" but did NOT in `fromSnapshot`**
            var leavers = filteredToMembers.Values
                .Where(m => m.MembershipStatus == "L"
                            && filteredFromMembers.TryGetValue(m.MemberNumber!.Value, out var prevMember)
                            && prevMember.MembershipStatus != "L")
                .ToList();

            // Identify deaths: They now have status "D" but did NOT in `fromSnapshot`**
            var deaths = filteredToMembers.Values
                .Where(m => m.MembershipStatus == "D"
                            && filteredFromMembers.TryGetValue(m.MemberNumber!.Value, out var prevMember)
                            && prevMember.MembershipStatus != "D")
                .ToList();

            // Category Changes: Members who exist in both but changed category**
            var categoryChanges = new Dictionary<string, int>();

            foreach (var member in filteredToMembers.Values)
            {
                if (filteredFromMembers.TryGetValue(member.MemberNumber!.Value, out var prevMember) &&
                    member.MembershipCategory != prevMember.MembershipCategory)
                {
                    string transitionKey = $"{prevMember.MembershipCategory} → {member.MembershipCategory}";
                    if (categoryChanges.ContainsKey(transitionKey))
                        categoryChanges[transitionKey]++;
                    else
                        categoryChanges[transitionKey] = 1;
                }
            }

            return new MembershipDeltaDto
            {
                FromDate = fromSnapshot.SnapshotDate.AddDays(1),
                ToDate = toSnapshot.SnapshotDate,
                PeriodDescription = periodDescription,
                NewMembers = newMembers.Count,
                Leavers = leavers.Count,
                Deaths = deaths.Count,
                CategoryChanges = categoryChanges
            };
        }

        private void AddToCount(Dictionary<string, int> dict, string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            if (!dict.TryAdd(key, 1))
                dict[key]++;
        }

    }
}
