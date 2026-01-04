using BOTGC.API.Common;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Text.RegularExpressions;
using MediatR;
using BOTGC.API.Services.Queries;

namespace BOTGC.API.Services.ReportServices
{
    public class MembershipReportingService : IMembershipReportingService
    {
        private readonly IMediator _mediator;
        private readonly ILogger<MembershipReportingService> _logger;

        /// <summary>slice
        /// Initializes a new instance of the <see cref="TrophyDataStore"/> class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        /// <param name="mediator">Mediator to handle queries</param>
        public MembershipReportingService(ILogger<MembershipReportingService> logger, IMediator mediator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        public async Task<MembershipReportDto> GetManagementReport(CancellationToken cancellationToken)
        {
            return await PrepareManagementReport(null, DateTime.UtcNow.Date, cancellationToken);
        }

        public async Task<MembershipReportDto> GetManagementReport(DateTime asAtDate, CancellationToken cancellationToken)
        {
            return await PrepareManagementReport(null, asAtDate.Date, cancellationToken);
        }

        private async Task<MembershipReportDto> PrepareManagementReport(int? filterByMemberId, DateTime asAtDate, CancellationToken cancellationToken)
        {
            var membersQuery = new GetMembershipReportQuery();
            var membershipReport = await _mediator.Send(membersQuery, cancellationToken);

            var members = membershipReport.Where(m => (m.MembershipStatus != "W" && m.MemberNumber != 0) || m.MembershipStatus == "W").ToList();
            if (filterByMemberId != null) members = members.Where(m => m.MemberNumber == filterByMemberId).ToList();

            var report = new MembershipReportDto();
            var today = asAtDate.Date;

            var dataPoints = new List<MembershipReportEntryDto>();

            report.Anomalies = IdentifyAnomalies(members);

            int currentSubsYearStart = today.Month >= 4 ? today.Year : today.Year - 1;
            DateTime startOfPreviousSubsYear = new DateTime(currentSubsYearStart - 1, 4, 1);
            DateTime endOfPreviousSubsYear = new DateTime(currentSubsYearStart, 3, 31);
            DateTime startOfCurrentSubsYear = new DateTime(currentSubsYearStart, 4, 1);
            DateTime endOfCurrentSubsYear = new DateTime(currentSubsYearStart + 1, 3, 31);

            int fyStartYear = today.Month >= 10 ? today.Year : today.Year - 1;
            DateTime fyStart = new DateTime(fyStartYear, 10, 1);
            DateTime fyEnd = new DateTime(fyStartYear + 1, 9, 30);

            DateTime startOfPreviousFinancialYear = new DateTime(fyStartYear - 1, 10, 1);

            report.Today = today;
            report.SubscriptionYearStart = startOfCurrentSubsYear;
            report.SubscriptionYearEnd = endOfCurrentSubsYear;
            report.FinancialYearStart = fyStart;
            report.FinancialYearEnd = fyEnd;

            var feeCoverageStart = startOfPreviousSubsYear;
            var feeCoverageEnd = fyEnd;

            var (categoryFees, categoryFeeLookup, dailyTargetRevenue) =
                await LoadRevenueConfig(feeCoverageStart, feeCoverageEnd, fyStart, fyEnd);

            var memberEventsQuery = new GetMemberEventsQuery
            {
                FromDate = startOfPreviousSubsYear.AddDays(-1),
                ToDate = today
            };

            var memberEvents = await _mediator.Send(memberEventsQuery);

            var firstAppliedAt = new Dictionary<int, DateTime>();

            foreach (var ev in memberEvents.Where(e => e.DateOfChange.HasValue)
                                           .OrderBy(e => e.DateOfChange!.Value))
            {
                var fromW = string.Equals((ev.FromStatus ?? "").Trim(), "W", StringComparison.OrdinalIgnoreCase);
                var toW = string.Equals((ev.ToStatus ?? "").Trim(), "W", StringComparison.OrdinalIgnoreCase);
                if (!fromW && toW)
                {
                    if (!firstAppliedAt.ContainsKey(ev.MemberId))
                        firstAppliedAt[ev.MemberId] = ev.DateOfChange!.Value.Date;
                }
            }

            foreach (var m in members.Where(m => m.MemberNumber.HasValue && m.MemberNumber.Value != 0 && m.ApplicationDate.HasValue))
            {
                var id = m.MemberNumber!.Value;
                var d = m.ApplicationDate!.Value.Date;
                if (!firstAppliedAt.TryGetValue(id, out var existing) || d < existing)
                    firstAppliedAt[id] = d;
            }

            var (previousSubscriptionYearPayments, currentSubscriptionYearPayments)
                = await GetReportSubscriptionPaymentsAsync(
                      startOfPreviousSubsYear,
                      endOfPreviousSubsYear,
                      startOfCurrentSubsYear,
                      endOfCurrentSubsYear,
                      startOfPreviousFinancialYear,
                      cancellationToken,
                      filterByMemberId);

            var allPayments = previousSubscriptionYearPayments
                                  .Concat(currentSubscriptionYearPayments);

            if (filterByMemberId != null) allPayments = allPayments.Where(p => p.MemberId == filterByMemberId).ToList();

            var (dailyBilled, dailyReceived) = BuildDailyRevenueSpread(allPayments, members, fyStart, fyEnd);

            var todaysResults = GetReportEntry(
                today,
                members,
                categoryFees,
                categoryFeeLookup,
                dailyTargetRevenue,
                dailyReceived,
                dailyBilled
            );

            dataPoints.Add(todaysResults);

            var monthlySnapshots = new Dictionary<DateTime, MembershipSnapshotDto>();

            var previousDayGroupings = members
                .Where(m => m.IsActive == true && m.MemberNumber.HasValue && m.MemberNumber.Value != 0)
                .ToDictionary(m => m.MemberNumber!.Value, m => m.MembershipCategoryGroup);

            for (var currentDate = today.AddDays(-1); currentDate >= startOfPreviousSubsYear; currentDate = currentDate.AddDays(-1))
            {
                try
                {
                    var appliedByEventToday = new HashSet<int>();

                    var eventsOnThisDay = memberEvents
                        .Where(e => e.DateOfChange.HasValue && e.DateOfChange.Value.Date == currentDate.Date)
                        .ToList();

                    var wlApps = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    var wlConv = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    var wlDrop = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                    static string T(string? s) => (s ?? string.Empty).Trim();
                    static bool Eq(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

                    foreach (var byMember in eventsOnThisDay.GroupBy(e => e.MemberId))
                    {
                        var memberId = byMember.Key;

                        var ordered = byMember
                            .OrderBy(e => e.DateOfChange!.Value)
                            .ThenBy(e => e.ChangeIndex)
                            .ToList();

                        if (ordered.Count == 0) continue;

                        bool everApplied = firstAppliedAt.TryGetValue(memberId, out var firstAppDate)
                                           && firstAppDate <= currentDate.Date;

                        var path = new List<(string S, string C)>();
                        var seen = new Dictionary<(string S, string C), int>();

                        var start = (T(ordered[0].FromStatus), T(ordered[0].FromCategory));
                        path.Add(start);
                        seen[start] = 0;

                        foreach (var e in ordered)
                        {
                            var prev = path[^1];
                            var next = (
                                S: !Eq(T(e.ToStatus), T(e.FromStatus)) ? T(e.ToStatus) : prev.S,
                                C: !Eq(T(e.ToCategory), T(e.FromCategory)) ? T(e.ToCategory) : prev.C
                            );

                            if (Eq(prev.S, next.S) && Eq(prev.C, next.C)) continue;

                            if (seen.TryGetValue(next, out var idx))
                            {
                                for (int i = path.Count - 1; i > idx; i--) seen.Remove(path[i]);
                                path.RemoveRange(idx + 1, path.Count - (idx + 1));
                            }
                            else
                            {
                                seen[next] = path.Count;
                                path.Add(next);
                            }
                        }

                        if (!(path.Count < 2 || (Eq(path[0].S, path[^1].S) && Eq(path[0].C, path[^1].C))))
                        {
                            for (int i = 1; i < path.Count; i++)
                            {
                                var prev = path[i - 1];
                                var next = path[i];

                                if (!Eq(prev.S, "W") && Eq(next.S, "W"))
                                {
                                    var key = MembershipHelper.ResolveCategoryGroup(next.C);
                                    wlApps[key] = wlApps.GetValueOrDefault(key) + 1;
                                    appliedByEventToday.Add(memberId);
                                    continue;
                                }

                                if (Eq(prev.S, "W") && !Eq(next.S, "W") && everApplied)
                                {
                                    var key = MembershipHelper.ResolveCategoryGroup(prev.C);
                                    if (Eq(next.S, "L"))
                                        wlDrop[key] = wlDrop.GetValueOrDefault(key) + 1;
                                    else
                                        wlConv[key] = wlConv.GetValueOrDefault(key) + 1;
                                }
                            }
                        }

                        for (int i = ordered.Count - 1; i >= 0; i--)
                        {
                            var ev = ordered[i];
                            MemberDto member = null;

                            if (ev.MemberId != 0)
                            {
                                member = members.SingleOrDefault(m => m.MemberNumber == ev.MemberId);
                            }

                            if (member == null)
                            {
                                member = members.SingleOrDefault(m =>
                                    string.Equals(m.FirstName, ev.Forename, StringComparison.OrdinalIgnoreCase) &&
                                    string.Equals(m.LastName, ev.Surname, StringComparison.OrdinalIgnoreCase) &&
                                    string.Equals(m.MembershipCategory, ev.ToCategory, StringComparison.OrdinalIgnoreCase));
                            }

                            if (member == null)
                            {
                                _logger.LogWarning($"Could not find member for event: MemberId={ev.MemberId}, Forename={ev.Forename}, Surname={ev.Surname}, ToCategory={ev.ToCategory}");
                                continue;
                            }

                            if (member.MembershipCategory == ev.ToCategory)
                                member.MembershipCategory = ev.FromCategory;
                            else
                                _logger.LogWarning($"Expected current category to be {ev.ToCategory} but found {member.MembershipCategory} for member id {member.MemberNumber} on {currentDate:yyyy-MM-dd}");

                            if (member.MembershipStatus == ev.ToStatus)
                                member.MembershipStatus = ev.FromStatus;
                            else
                                _logger.LogWarning($"Expected current status to be {ev.ToStatus} but found {member.MembershipStatus} for member id {member.MemberNumber} on {currentDate:yyyy-MM-dd}");
                        }
                    }

                    foreach (var m in members.Where(m =>
                                 m.MemberNumber.HasValue &&
                                 m.MemberNumber.Value != 0 &&
                                 m.ApplicationDate.HasValue &&
                                 m.ApplicationDate.Value.Date == currentDate.Date))
                    {
                        if (appliedByEventToday.Contains(m.MemberNumber!.Value)) continue;
                        var g = string.IsNullOrWhiteSpace(m.MembershipCategoryGroup) ? "Unknown" : m.MembershipCategoryGroup;
                        wlApps[g] = wlApps.GetValueOrDefault(g) + 1;
                    }

                    foreach (var member in members)
                        member.SetPrimaryCategory(currentDate).SetCategoryGroup();

                    var todayActiveGroups = members
                         .Where(m => m.IsActive == true && m.MemberNumber.HasValue && m.MemberNumber.Value != 0)
                         .ToDictionary(m => m.MemberNumber!.Value, m => m.MembershipCategoryGroup);

                    var (joinersByGroup, leaversByGroup) = ComputeGroupJoinersAndLeavers(previousDayGroupings, todayActiveGroups);

                    var dailyReportEntry = GetReportEntry(currentDate, members, categoryFees, categoryFeeLookup, dailyTargetRevenue, dailyReceived, dailyBilled);

                    dailyReportEntry.DailyJoinersByCategoryGroup = joinersByGroup;
                    dailyReportEntry.DailyLeaversByCategoryGroup = leaversByGroup;

                    dailyReportEntry.WaitingListApplicationsByGroup = wlApps;
                    dailyReportEntry.WaitingListConversionsByAppliedGroup = wlConv;
                    dailyReportEntry.WaitingListDropoutsByAppliedGroup = wlDrop;
                    dailyReportEntry.WaitingListApplicationsTotal = wlApps.Values.Sum();
                    dailyReportEntry.WaitingListConversionsTotal = wlConv.Values.Sum();
                    dailyReportEntry.WaitingListDropoutsTotal = wlDrop.Values.Sum();

                    dataPoints.Insert(0, dailyReportEntry);

                    if (IsMonthEnd(currentDate) || currentDate.Date == today.Date.AddDays(-1))
                    {
                        monthlySnapshots[currentDate.Date] = CreateSnapshot(members, currentDate);
                    }

                    previousDayGroupings = todayActiveGroups;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing date {currentDate:yyyy-MM-dd}. Skipping this date.");
                }
            }

            var plotFrom = fyStart.AddMonths(-6).AddDays(-1);
            var plotTo = fyEnd.AddMonths(6);

            EnsureFullYearData(dataPoints, plotTo, dailyTargetRevenue);
            ApplyGrowthTargets(dataPoints, fyStart, plotTo);

            report.DataPoints = dataPoints
                .Where(dp => dp.Date >= plotFrom && dp.Date <= plotTo)
                .ToList();
            report.DataPointsCsv = ConvertToCsv(report.DataPoints);

            EnsureMonthlyAndQuarterlyStats(report, monthlySnapshots, asAtDate);


            var aprSepYear = report.FinancialYearEnd.Year;

            //var windowStart = new DateTime(aprSepYear, 4, 1);
            //var windowEnd = new DateTime(aprSepYear, 9, 30);
            //var baselineDate = windowStart.AddDays(-1);

            var baselineDate = report.FinancialYearStart.Date;
            var windowStart = new DateTime(report.FinancialYearEnd.Year, 4, 1);
            var windowEnd = new DateTime(report.FinancialYearEnd.Year, 9, 30);

            if (monthlySnapshots.TryGetValue(baselineDate.Date.AddDays(-1), out var baselineSnapshot))
            {
                report.LostRevenueAprToSep = CalculateAprToSepLostRevenueByMonth_ActualSums(
                    report.DataPoints,
                    baselineSnapshot.Members,
                    baselineDate,
                    windowStart,
                    windowEnd,
                    categoryFeeLookup);
            }
            else
            {
                _logger.LogWarning("Baseline snapshot missing for {Baseline:yyyy-MM-dd}; cannot calculate Apr–Sep lost revenue.", baselineDate);
            }


            PopulateWaitingListAggregatesForPeriods(report.MonthlyStats, report.DataPoints);
            PopulateWaitingListAggregatesForPeriods(report.QuarterlyStats, report.DataPoints);

            var totalActualRevenue = report.DataPoints.Where(dp => dp.Date >= fyStart && dp.Date <= fyEnd).Select(dp => dp.ActualRevenue).Sum();
            var totalBilledRevenue = report.DataPoints.Where(dp => dp.Date >= fyStart && dp.Date <= fyEnd).Select(dp => dp.BilledRevenue).Sum();
            var totalReceivedRevenue = report.DataPoints.Where(dp => dp.Date >= fyStart && dp.Date <= fyEnd).Select(dp => dp.ReceivedRevenue).Sum();

            _logger.LogInformation(
                $"Total Actual Revenue: {totalActualRevenue:C}, " +
                $"Total Billed Revenue: {totalBilledRevenue:C}, " +
                $"Total Received Revenue: {totalReceivedRevenue:C}");

            return report;
        }

        /// <summary>
        /// Returns the two clean payment lists that the rest of the pipeline
        /// expects.  Handles the Oct-23 → Mar-24 “18-month” transition
        /// automatically, injecting the synthetic 24/25 invoices when needed.
        /// </summary>
        private async Task<(List<SubscriptionPaymentDto> previous,
                            List<SubscriptionPaymentDto> current)>
            GetReportSubscriptionPaymentsAsync(
                DateTime startOfPreviousSubsYear,
                DateTime endOfPreviousSubsYear,
                DateTime startOfCurrentSubsYear,
                DateTime endOfCurrentSubsYear,
                DateTime startOfPreviousFinancialYear,
                CancellationToken cancellationToken,
                int? filterByMemberId = null)
        {
            var previousSubscriptionPaymentsQuery = new GetSubscriptionPaymentsByDateRangeQuery() { FromDate = startOfPreviousSubsYear, ToDate = endOfPreviousSubsYear };
            var previousTask = _mediator.Send(previousSubscriptionPaymentsQuery, cancellationToken);

            var currentSubscriptionPaymentsQuery = new GetSubscriptionPaymentsByDateRangeQuery() { FromDate = startOfCurrentSubsYear, ToDate = endOfCurrentSubsYear };
            var currentTask = _mediator.Send(currentSubscriptionPaymentsQuery, cancellationToken);

            Task<List<SubscriptionPaymentDto>> transitionTask =
                Task.FromResult(new List<SubscriptionPaymentDto>());

            if (startOfPreviousSubsYear.Year == 2024)
            {
                var transitionSubscriptionPaymentsQuery = new GetSubscriptionPaymentsByDateRangeQuery() { FromDate = startOfPreviousFinancialYear, ToDate = startOfPreviousSubsYear.AddDays(-1) };
                transitionTask = _mediator.Send(transitionSubscriptionPaymentsQuery, cancellationToken);
            }

            await Task.WhenAll(previousTask, currentTask, transitionTask);

            var previousInvoices = await previousTask;
            var currentInvoices = await currentTask;
            var transitionInvoices = await transitionTask;

            if (filterByMemberId != null)
            {
                previousInvoices = previousInvoices
                    .Where(p => p.MemberId == filterByMemberId)
                    .ToList();

                currentInvoices = currentInvoices
                    .Where(p => p.MemberId == filterByMemberId)
                    .ToList();

                transitionInvoices = transitionInvoices
                    .Where(p => p.MemberId == filterByMemberId)
                    .ToList();
            }

            if (startOfPreviousSubsYear.Year == 2024)
            {
                var headline18m = await BuildHeadline18mFeesAsync();

                var synthetic24_25 = MakeSyntheticInvoices24_25(
                                         transitionInvoices, headline18m);

                previousInvoices.AddRange(synthetic24_25);
            }

            return (previousInvoices, currentInvoices);
        }

        private SubscriptionPaymentDto MakeDailyProrataSlice(
                SubscriptionPaymentDto inv,
                DateTime sliceStart,
                DateTime sliceEnd)
        {
            var coverStart = inv.DateDue.Day >= 16
                ? new DateTime(inv.DateDue.Year, inv.DateDue.Month, 1).AddMonths(1)
                : new DateTime(inv.DateDue.Year, inv.DateDue.Month, 1);

            var coverEnd = new DateTime(2025, 3, 31);
            if (coverStart > coverEnd) coverEnd = coverStart;

            var totalDays = (coverEnd - coverStart).Days + 1;
            if (totalDays <= 0) totalDays = 1;

            var dailyRate = inv.BillAmount / totalDays;

            var sliceCovStart = sliceStart > coverStart ? sliceStart : coverStart;
            var sliceDays = (sliceEnd - sliceCovStart).Days + 1;
            if (sliceDays <= 0) sliceDays = 0;

            var sliceBill = Math.Round(dailyRate * sliceDays, 2,
                                       MidpointRounding.AwayFromZero);

            var paidFactor = inv.BillAmount == 0m ? 0m : sliceBill / inv.BillAmount;
            var slicePaid = Math.Round((inv.AmountPaid ?? 0m) * paidFactor, 2,
                                        MidpointRounding.AwayFromZero);

            var payDate = inv.PaymentDate.HasValue && inv.PaymentDate.Value >= sliceStart
                ? inv.PaymentDate
                : (DateTime?)null;

            return new SubscriptionPaymentDto
            {
                MemberId = inv.MemberId,
                DateDue = sliceStart,
                BillAmount = sliceBill,
                AmountPaid = slicePaid,
                PaymentDate = slicePaid > 0m ? payDate : null,
                MembershipCategory = inv.MembershipCategory?.Trim() ?? string.Empty
            };
        }

        private async Task<Dictionary<string, decimal>> BuildHeadline18mFeesAsync()
        {
            var (annual, _, _) = await LoadRevenueConfig(
                                    new DateTime(2023, 4, 1),
                                    new DateTime(2024, 3, 31),
                                    new DateTime(2023, 10, 1),
                                    new DateTime(2024, 9, 30));

            var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in annual)
            {
                var yearly = kvp.Value[0].fee[0].Amount;
                if (yearly <= 0m) continue;

                var eighteen = Math.Round(yearly * 1.5m / 10m, 0,
                                           MidpointRounding.AwayFromZero) * 10m;

                result[kvp.Key.Trim()] = eighteen;
            }

            return result;
        }

        private IEnumerable<SubscriptionPaymentDto> MakeSyntheticInvoices24_25(
            IEnumerable<SubscriptionPaymentDto> transitionInvoices,
            Dictionary<string, decimal> headline18m)
        {
            var sliceStart = new DateTime(2024, 10, 1);
            var sliceEnd = new DateTime(2025, 3, 31);

            var bundles = transitionInvoices
                .GroupBy(p => (p.MemberId,
                               (p.MembershipCategory ?? string.Empty).Trim()));

            foreach (var bundle in bundles)
            {
                var (memberId, currentCat) = bundle.Key;
                string chosenCat = null;
                decimal head18m = 0m;

                decimal totalBillAmount = 0m;

                foreach (var inv in bundle.OrderBy(p => p.DateDue))
                {
                    totalBillAmount += inv.BillAmount;
                    if (inv.BillAmount <= 0m) continue;

                    var candidates = headline18m
                        .Where(kv =>
                        {
                            if (kv.Value <= 0m) return false;

                            var monthly = kv.Value / 18m;
                            var estMonths = inv.BillAmount / monthly;
                            var nearestMon = Math.Round(estMonths, MidpointRounding.AwayFromZero);

                            if (nearestMon < 1 || nearestMon > 18) return false;

                            var reconFee = Math.Round(nearestMon * monthly / 10m, 0,
                                                      MidpointRounding.AwayFromZero) * 10m;
                            return Math.Abs(reconFee - inv.BillAmount) <= 1m;
                        })
                        .Select(kv => kv.Key)
                        .ToList();

                    if (candidates.Count == 0)
                    {
                        continue;
                    }
                    else if (candidates.Count == 1)
                    {
                        chosenCat = candidates[0];
                        head18m = headline18m[chosenCat];
                        break;
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(currentCat) &&
                            candidates.Contains(currentCat))
                        {
                            chosenCat = currentCat;
                            head18m = headline18m[chosenCat];
                            break;
                        }
                    }
                }

                if (totalBillAmount > 0 && chosenCat == null)
                {
                    _logger.LogWarning(
                        $"[TRANSITION]  Member {memberId}: could not infer a unique " +
                        $"18-month headline fee from {bundle.Count()} invoice(s).  " +
                        $"Falling back to daily pro-rata.");

                    foreach (var inv in bundle)
                        yield return MakeDailyProrataSlice(inv, sliceStart, sliceEnd);

                    continue;
                }

                decimal monthlyRate = head18m / 18m;

                var monthSet = new HashSet<(int year, int month)>();

                foreach (var inv in bundle)
                {
                    var covStart = inv.DateDue.Day >= 16
                                 ? new DateTime(inv.DateDue.Year, inv.DateDue.Month, 1).AddMonths(1)
                                 : new DateTime(inv.DateDue.Year, inv.DateDue.Month, 1);

                    for (var m = covStart; m <= sliceEnd; m = m.AddMonths(1))
                        monthSet.Add((m.Year, m.Month));
                }

                var months24_25 = monthSet.Count(t =>
                       (t.year > 2024 || (t.year == 2024 && t.month >= 10)) &&
                       (t.year < 2025 || (t.year == 2025 && t.month <= 3)));

                if (months24_25 == 0)
                    continue;

                var sliceBill = Math.Round(months24_25 * monthlyRate / 10m, 0,
                                           MidpointRounding.AwayFromZero) * 10m;

                var bundleBill = bundle.Sum(p => p.BillAmount);
                var bundlePaid = bundle.Sum(p => p.AmountPaid ?? 0m);
                var slicePaid = bundleBill == 0m ? 0m
                                 : bundlePaid * (sliceBill / bundleBill);

                var firstPayOnOrAfter = bundle
                    .Where(p => p.PaymentDate.HasValue &&
                                p.PaymentDate.Value >= sliceStart)
                    .Min(p => p.PaymentDate) ?? sliceStart;

                yield return new SubscriptionPaymentDto
                {
                    MemberId = memberId,
                    DateDue = sliceStart,
                    BillAmount = sliceBill,
                    AmountPaid = slicePaid,
                    PaymentDate = slicePaid > 0m ? firstPayOnOrAfter : (DateTime?)null,
                    MembershipCategory = chosenCat
                };

                if (bundle.Count() > 1)
                {
                    _logger.LogDebug(
                        $"[TRANSITION]  Member {memberId}: merged {bundle.Count()} " +
                        $"transition invoices into synthetic £{sliceBill:N0} ({chosenCat}).");
                }
            }
        }

        private (Dictionary<string, int> Joiners, Dictionary<string, int> Leavers) ComputeGroupJoinersAndLeavers(Dictionary<int, string> previousDayGroups, Dictionary<int, string> todayGroups)
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
                "Date,PlayingMembers,NonPlayingMembers,TargetPlayingMembers,AveragePlayingMemberAge,WaitingListTotal"
            };

            csvLines.AddRange(entries.Select(e =>
                $"{e.Date:yyyy-MM-dd}," +
                $"{e.PlayingMembers}," +
                $"{e.NonPlayingMembers}," +
                $"{e.TargetPlayingMembers}," +
                $"{e.AveragePlayingMembersAge:F2}," +
                $"{e.WaitingListCategoryBreakdown?.Values.Sum() ?? 0}"
            ));

            return string.Join("\n", csvLines);
        }

        private bool IsMonthEnd(DateTime date)
        {
            return date.Day == DateTime.DaysInMonth(date.Year, date.Month);
        }

        private bool IsQuarterEnd(DateTime date)
        {
            return (date.Month == 3 && date.Day == 31) ||
                   (date.Month == 6 && date.Day == 30) ||
                   (date.Month == 9 && date.Day == 30) ||
                   (date.Month == 12 && date.Day == 31);
        }

        private int GetQuarterNumber(DateTime d)
        {
            return d.Month switch
            {
                10 or 11 or 12 => 1,
                1 or 2 or 3 => 2,
                4 or 5 or 6 => 3,
                7 or 8 or 9 => 4,
                _ => throw new InvalidOperationException("Invalid month")
            };
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

        private MembershipReportEntryDto GetReportEntry(
            DateTime date,
            List<MemberDto> members,
            Dictionary<string, List<(DateTime start, DateTime end, Fee[] fee)>> categoryFees,
            Dictionary<(string category, DateTime date), Fee[]> categoryFeeLookup,
            Dictionary<DateTime, Fee[]> dailyTargetRevenue,
            Dictionary<DateTime, decimal> dailyReceived,
            Dictionary<DateTime, decimal> dailyBilled)
        {
            var playingMembers = members.Where(m => m.PrimaryCategory == MembershipPrimaryCategories.PlayingMember && m.IsActive!.Value).ToList();
            var nonPlayingMembers = members.Where(m => m.PrimaryCategory == MembershipPrimaryCategories.NonPlayingMember && m.IsActive!.Value).ToList();

            double averageAge = playingMembers
               .Where(m => m.DateOfBirth.HasValue)
               .Select(m => (date - m.DateOfBirth.Value).TotalDays / 365.25)
               .DefaultIfEmpty(0)
               .Average();

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

            var waitingListBreakdown = members
                .Where(m =>
                    m.MembershipStatus == "W" &&
                    m.ApplicationDate.HasValue &&
                    m.ApplicationDate.Value.Date <= date
                )
                .GroupBy(m => m.MembershipCategoryGroup ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count());

            decimal actualRevenue = 0;

            foreach (var member in members.Where(m => m.IsActive == true))
            {
                if (!string.IsNullOrWhiteSpace(member.MembershipCategory))
                {
                    var trimmedCategory = member.MembershipCategory.Trim();

                    if (categoryFeeLookup.TryGetValue((trimmedCategory, date.Date), out var fees))
                    {
                        Fee chosenFee = null;

                        if (date.Date < new DateTime(2025, 04, 01))
                        {
                            chosenFee = (member.JoinDate.HasValue &&
                                          member.JoinDate.Value.Date < new DateTime(2024, 4, 1))
                                         ? fees.FirstOrDefault(f => f.YearStart == 23)
                                         : fees.FirstOrDefault(f => f.YearStart == 24);

                            if (chosenFee == null)
                            {
                                chosenFee = fees.OrderBy(f => f.YearStart).First();
                            }
                        }
                        else
                        {
                            chosenFee = fees.FirstOrDefault();
                        }

                        if (chosenFee != null)
                        {
                            actualRevenue += chosenFee.Amount / 365m;
                        }
                        else
                        {
                            _logger.LogWarning("Failed to find fee for category '{Category}' on {Date:yyyy-MM-dd} for member ID {MemberId}",
                                               trimmedCategory, date, member.MemberNumber);
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"No fee found for category '{trimmedCategory}' on {date:yyyy-MM-dd} for member ID {member.MemberNumber}");
                    }
                }
            }

            decimal targetRevenue = dailyTargetRevenue.TryGetValue(date, out var target) ? target[0].Amount : 0;

            return new MembershipReportEntryDto
            {
                Date = date,
                PlayingMembers = playingMembers.Count,
                NonPlayingMembers = nonPlayingMembers.Count,
                AveragePlayingMembersAge = averageAge,
                PlayingCategoryBreakdown = playingCategoryBreakdown,
                NonPlayingCategoryBreakdown = nonPlayingCategoryBreakdown,
                WaitingListCategoryBreakdown = waitingListBreakdown,
                CategoryGroupBreakdown = categoryGroupBreakdown,
                ActualRevenue = actualRevenue,
                TargetRevenue = targetRevenue,
                BilledRevenue = dailyBilled.GetValueOrDefault(date),
                ReceivedRevenue = dailyReceived.GetValueOrDefault(date)
            };
        }

        private static void PopulateWaitingListAggregatesForPeriods(
            IEnumerable<MembershipDeltaDto> periods,
            IEnumerable<MembershipReportEntryDto> dataPoints)
        {
            foreach (var p in periods)
            {
                var window = dataPoints.Where(dp => dp.Date >= p.FromDate && dp.Date <= p.ToDate);

                p.WaitingListApplications = window.Sum(dp => dp.WaitingListApplicationsTotal);
                p.WaitingListConversions = window.Sum(dp => dp.WaitingListConversionsTotal);
                p.WaitingListDropouts = window.Sum(dp => dp.WaitingListDropoutsTotal);

                var apps = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var conv = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                var drop = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                foreach (var dp in window)
                {
                    if (dp.WaitingListApplicationsByGroup != null)
                        foreach (var kv in dp.WaitingListApplicationsByGroup)
                            apps[kv.Key] = apps.GetValueOrDefault(kv.Key) + kv.Value;

                    if (dp.WaitingListConversionsByAppliedGroup != null)
                        foreach (var kv in dp.WaitingListConversionsByAppliedGroup)
                            conv[kv.Key] = conv.GetValueOrDefault(kv.Key) + kv.Value;

                    if (dp.WaitingListDropoutsByAppliedGroup != null)
                        foreach (var kv in dp.WaitingListDropoutsByAppliedGroup)
                            drop[kv.Key] = drop.GetValueOrDefault(kv.Key) + kv.Value;
                }

                p.WaitingListApplicationsByGroup = apps;
                p.WaitingListConversionsByAppliedGroup = conv;
                p.WaitingListDropoutsByAppliedGroup = drop;
            }
        }

        private void EnsureFullYearData(List<MembershipReportEntryDto> dataPoints, DateTime endOfCurrentFinancialYear, Dictionary<DateTime, Fee[]> dailyTargetRevenue)
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
                    ActualRevenue = lastEntry.ActualRevenue,
                    TargetRevenue = dailyTargetRevenue.TryGetValue(currentDate, out var t) ? t[0].Amount : 0,
                    CategoryGroupBreakdown = new Dictionary<string, int>(lastCategoryGroupBreakdown),
                    PlayingCategoryBreakdown = new Dictionary<string, int>(lastPlayingCategoryBreakdown),
                    NonPlayingCategoryBreakdown = new Dictionary<string, int>(lastNonPlayingCategoryBreakdown),
                    ReceivedRevenue = lastEntry.ReceivedRevenue,
                    BilledRevenue = lastEntry.BilledRevenue
                });
            }
        }

        private void EnsureMonthlyAndQuarterlyStats(MembershipReportDto report, Dictionary<DateTime, MembershipSnapshotDto> monthlySnapshots, DateTime asAtDate)
        {
            report.MonthlyStats = new List<MembershipDeltaDto>();

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

            var quarterEndDates = monthEndDates.Where(IsQuarterEnd).OrderBy(d => d).ToList();

            for (int i = 1; i < quarterEndDates.Count; i++)
            {
                var fromSnapshot = monthlySnapshots[quarterEndDates[i - 1]];
                var toSnapshot = monthlySnapshots[quarterEndDates[i]];

                if (fromSnapshot.SnapshotDate >= toSnapshot.SnapshotDate)
                    throw new InvalidOperationException($"Quarterly snapshots are out of order! {fromSnapshot.SnapshotDate} is not before {toSnapshot.SnapshotDate}");

                report.QuarterlyStats.Add(ComputeMembershipDelta(fromSnapshot, toSnapshot, GetPeriodDescription(toSnapshot.SnapshotDate, asAtDate)));
            }

            var toDateMarker = asAtDate.Date.AddDays(-1);

            if (quarterEndDates.Count > 0 && monthlySnapshots.ContainsKey(toDateMarker) && toDateMarker > quarterEndDates.Last())
            {
                var fromSnapshot = monthlySnapshots[quarterEndDates.Last()];
                var toSnapshot = monthlySnapshots[toDateMarker];
                var periodDesc = GetPeriodDescription(toDateMarker, asAtDate);

                report.QuarterlyStats.Add(
                    ComputeMembershipDelta(fromSnapshot, toSnapshot, periodDesc)
                );
            }
        }

        private string GetPeriodDescription(DateTime snapshotDate, DateTime asAtDate)
        {
            var sd = snapshotDate;
            int fq = GetQuarterNumber(sd);

            int labelYear = sd.Month == 12 ? sd.Year + 1 : sd.Year;
            var toDateMarker = asAtDate.Date.AddDays(-1);
            string suffix = (sd.Date == toDateMarker) ? " (To Date)" : "";

            return $"Q{fq} {labelYear % 100:00}{suffix}";
        }

        private void ApplyGrowthTargets(List<MembershipReportEntryDto> dataPoints, DateTime start, DateTime end)
        {
            var lastActualEntry = dataPoints.LastOrDefault(dp => dp.Date.Date < start.Date);
            double startValue = lastActualEntry?.PlayingMembers ?? 0;
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

        private record Fee
        {
            private int yearStart;
            private int yearEnd;
            private decimal amount;

            public Fee(int yearStart, int yearEnd, decimal amount)
            {
                YearStart = yearStart;
                YearEnd = yearEnd;
                Amount = amount;
            }

            public int YearStart { get => yearStart; set => yearStart = value; }
            public int YearEnd { get => yearEnd; set => yearEnd = value; }
            public decimal Amount { get => amount; set => amount = value; }
        }

        private async Task<(
            Dictionary<string, List<(DateTime start, DateTime end, Fee[] fee)>> categoryFees,
            Dictionary<(string category, DateTime date), Fee[]> categoryFeeLookup,
            Dictionary<DateTime, Fee[]> dailyTargetRevenue
        )> LoadRevenueConfig(DateTime feeCoverageStart, DateTime feeCoverageEnd,
                  DateTime budgetFinancialYearStart, DateTime budgetFinancialYearEnd)
        {
            string basePath = Path.Combine(AppContext.BaseDirectory, "Data");

            var categoryFees = new Dictionary<string, List<(DateTime start, DateTime end, Fee[] fee)>>();

            foreach (var file in Directory.EnumerateFiles(basePath, "MembershipFees *.json"))
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                var yearPart = fileName.Replace("MembershipFees ", "").Trim();

                if (!Regex.IsMatch(yearPart, @"^\d{4}$")) continue;

                var startYear = int.Parse(yearPart.Substring(0, 2)) + 2000;
                var endYear = int.Parse(yearPart.Substring(2, 2)) + 2000;

                var subStart = new DateTime(startYear, 4, 1);
                var subEnd = new DateTime(endYear, 3, 31);

                if (subEnd < feeCoverageStart || subStart > feeCoverageEnd) continue;

                var feeData = JsonSerializer.Deserialize<Dictionary<string, decimal>>(await File.ReadAllTextAsync(file))!;
                foreach (var kvp in feeData)
                {
                    if (!categoryFees.TryGetValue(kvp.Key, out var list))
                    {
                        list = new List<(DateTime start, DateTime end, Fee[] fee)>();
                        categoryFees[kvp.Key] = list;
                    }
                    list.Add((subStart, subEnd, [new Fee(startYear - 2000, endYear - 2000, kvp.Value)]));
                }
            }

            var categoryFeeLookup = new Dictionary<(string category, DateTime date), Fee[]>();
            foreach (var kvp in categoryFees)
            {
                foreach (var (start, end, fee) in kvp.Value)
                {
                    for (var d = start.Date; d <= end.Date; d = d.AddDays(1))
                    {
                        categoryFeeLookup[(kvp.Key, d)] = fee;
                    }
                }
            }

            var budgetFileKey = $"{budgetFinancialYearStart.Year % 100}{budgetFinancialYearEnd.Year % 100:D2}";
            var budgetFileName = $"Membership Subscription Budget {budgetFileKey}.json";
            var budgetPath = Path.Combine(basePath, budgetFileName);

            var budgetYearStart = int.Parse($"{budgetFinancialYearStart.Year % 100}");
            var budgetYearEnd = int.Parse($"{budgetFinancialYearEnd.Year % 100}");

            var dailyTargetRevenue = new Dictionary<DateTime, Fee[]>();

            if (File.Exists(budgetPath))
            {
                var monthlyBudget = JsonSerializer.Deserialize<Dictionary<string, decimal>>(await File.ReadAllTextAsync(budgetPath))!;

                foreach (var month in monthlyBudget)
                {
                    var monthStart = DateTime.ParseExact(month.Key + "-01", "yyyy-MM-dd", null);
                    int daysInMonth = DateTime.DaysInMonth(monthStart.Year, monthStart.Month);
                    decimal dailyAmount = month.Value / daysInMonth;

                    for (int day = 0; day < daysInMonth; day++)
                    {
                        var current = monthStart.AddDays(day);
                        if (current >= budgetFinancialYearStart && current <= budgetFinancialYearEnd)
                            dailyTargetRevenue[current] = [new Fee(budgetYearStart, budgetYearEnd, dailyAmount)];
                    }
                }
            }
            else
            {
                _logger.LogWarning("Failed to load budget file: " + budgetPath);
            }

            if (feeCoverageStart <= new DateTime(2024, 10, 1) &&
                feeCoverageEnd >= new DateTime(2025, 3, 31))
            {
                var legacyTblPath = Path.Combine(basePath, "MembershipFees 2324.json");
                if (File.Exists(legacyTblPath))
                {
                    var legacyTbl = JsonSerializer.Deserialize<Dictionary<string, decimal>>
                                        (await File.ReadAllTextAsync(legacyTblPath))!;

                    var overlapStart = new DateTime(2024, 10, 1);
                    var overlapEnd = new DateTime(2025, 3, 31);

                    foreach (var (cat, annual) in legacyTbl)
                    {
                        var legacyFee = new Fee(23, 24, annual);

                        for (var d = overlapStart; d <= overlapEnd; d = d.AddDays(1))
                        {
                            if (categoryFeeLookup.TryGetValue((cat, d), out var arr))
                            {
                                var combined = new Fee[arr.Length + 1];
                                Array.Copy(arr, combined, arr.Length);
                                combined[^1] = legacyFee;
                                categoryFeeLookup[(cat, d)] = combined;
                            }
                            else
                            {
                                categoryFeeLookup[(cat, d)] = [legacyFee];
                            }
                        }
                    }
                }
                else
                {
                    _logger.LogWarning($"Legacy fee table missing: {legacyTblPath}");
                }
            }

            return (categoryFees, categoryFeeLookup, dailyTargetRevenue);
        }

        private (Dictionary<DateTime, decimal> dailyBilled,
                 Dictionary<DateTime, decimal> dailyReceived)
            BuildDailyRevenueSpread(IEnumerable<SubscriptionPaymentDto> payments,
                                    List<MemberDto> members,
                                    DateTime fyStart,
                                    DateTime fyEnd)
        {
            var billed = new Dictionary<DateTime, decimal>();
            var received = new Dictionary<DateTime, decimal>();

            foreach (var p in payments)
            {
                if (p.BillAmount == 0m && (p.AmountPaid ?? 0m) == 0m) continue;

                var subStart = new DateTime(p.DateDue.Year, 4, 1);
                var subEnd = subStart.AddYears(1).AddDays(-1);

                var m = members.FirstOrDefault(x => x.MemberNumber == p.MemberId);
                if (m == null)
                {
                    _logger.LogWarning($"No member found for subscription payment with ID {p.MemberId}");
                    continue;
                }

                var coverStart = subStart > (m.JoinDate?.Date ?? subStart)
                               ? subStart
                               : m.JoinDate?.Date ?? subStart;

                if (p.DateDue == new DateTime(2024, 10, 1))
                    coverStart = p.DateDue;

                var coverEnd = subEnd < (m.LeaveDate?.Date ?? subEnd)
                             ? subEnd
                             : m.LeaveDate?.Date ?? subEnd;

                if (coverStart > coverEnd) continue;

                var origDays = Math.Max((coverEnd - coverStart).Days + 1, 1);

                if (coverStart < fyStart) coverStart = fyStart;
                if (coverEnd > fyEnd) coverEnd = fyEnd;
                if (coverStart > coverEnd) continue;

                var clipDays = Math.Max((coverEnd - coverStart).Days + 1, 1);
                var clipRatio = clipDays / (decimal)origDays;

                var billSlice = p.BillAmount * clipRatio;
                var perDayBill = billSlice / clipDays;

                for (var d = coverStart; d <= coverEnd; d = d.AddDays(1))
                    billed[d] = billed.GetValueOrDefault(d) + perDayBill;

                if (p.AmountPaid is decimal paid && paid > 0m && p.PaymentDate.HasValue)
                {
                    var payStart = p.PaymentDate.Value.Date;
                    if (payStart < coverStart) payStart = coverStart;
                    if (payStart > coverEnd) continue;

                    var payDays = Math.Max((coverEnd - payStart).Days + 1, 1);
                    var payRatio = payDays / (decimal)origDays;
                    var paidSlice = paid * payRatio;
                    var perDayPaid = paidSlice / payDays;

                    for (var d = payStart; d <= coverEnd; d = d.AddDays(1))
                        received[d] = received.GetValueOrDefault(d) + perDayPaid;
                }
            }

            return (billed, received);
        }

        private void AddToCount(Dictionary<string, int> dict, string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            if (!dict.TryAdd(key, 1))
                dict[key]++;
        }

        private MembershipDeltaDto ComputeMembershipDelta(MembershipSnapshotDto fromSnapshot, MembershipSnapshotDto toSnapshot, string periodDescription)
        {
            var filteredFromMembers = fromSnapshot.Members
                .Where(m => !m.JoinDate.HasValue || m.JoinDate!.Value <= fromSnapshot.SnapshotDate)
                .Where(m => m.MemberNumber != 0)
                .ToDictionary(m => m.MemberNumber!.Value);

            var filteredToMembers = toSnapshot.Members
                .Where(m => !m.JoinDate.HasValue || m.JoinDate!.Value <= toSnapshot.SnapshotDate)
                .Where(m => m.MemberNumber != 0)
                .ToDictionary(m => m.MemberNumber!.Value);

            var newMembers = filteredToMembers.Keys.Except(filteredFromMembers.Keys).ToList();

            var leavers = filteredToMembers.Values
                .Where(m => m.MembershipStatus == "L"
                            && filteredFromMembers.TryGetValue(m.MemberNumber!.Value, out var prevMember)
                            && prevMember.MembershipStatus != "L")
                .ToList();

            var deaths = filteredToMembers.Values
                .Where(m => m.MembershipStatus == "D"
                            && filteredFromMembers.TryGetValue(m.MemberNumber!.Value, out var prevMember)
                            && prevMember.MembershipStatus != "D")
                .ToList();

            var categoryGroupTotals = toSnapshot.Members
                .Where(m => m.IsActive == true)
                .GroupBy(m => m.MembershipCategoryGroup ?? "Other")
                .ToDictionary(g => g.Key, g => g.Count());

            var categoryChanges = new Dictionary<string, int>();

            foreach (var member in filteredToMembers.Values)
            {
                if (filteredFromMembers.TryGetValue(member.MemberNumber!.Value, out var prevMember))
                {
                    string prevGroup = prevMember.MembershipCategoryGroup ?? "Other";
                    string newGroup = member.MembershipCategoryGroup ?? "Other";

                    if (!string.Equals(prevGroup, newGroup, StringComparison.Ordinal))
                    {
                        string transitionKey = $"{prevGroup} → {newGroup}";
                        if (!categoryChanges.TryAdd(transitionKey, 1))
                            categoryChanges[transitionKey]++;
                    }
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
                CategoryChanges = categoryChanges,
                CategoryGroupTotals = categoryGroupTotals
            };
        }

        private LostRevenueBreakdownDto CalculateAprToSepLostRevenueByMonth_ActualSums(
    List<MembershipReportEntryDto> dataPoints,
    List<MemberDto> baselineMembers,
    DateTime baselineDate,
    DateTime windowStart,
    DateTime windowEnd,
    Dictionary<(string category, DateTime date), Fee[]> categoryFeeLookup)
        {
            baselineDate = baselineDate.Date;
            windowStart = windowStart.Date;
            windowEnd = windowEnd.Date;

            var baselinePoint = dataPoints.SingleOrDefault(d => d.Date.Date == baselineDate);
            if (baselinePoint == null)
            {
                throw new InvalidOperationException($"No datapoint found for baseline date {baselineDate:yyyy-MM-dd}.");
            }

            var baselinePlayingCount = baselinePoint.PlayingMembers;
            var baselineNonPlayingCount = baselinePoint.NonPlayingMembers;

            var baselinePlayingDailyFeesDesc = BuildBaselineDailyFeesDescending(
                baselineMembers.Where(m => m.IsActive == true && m.PrimaryCategory == MembershipPrimaryCategories.PlayingMember).ToList(),
                baselineDate,
                categoryFeeLookup);

            var baselineNonPlayingDailyFeesDesc = BuildBaselineDailyFeesDescending(
                baselineMembers.Where(m => m.IsActive == true && m.PrimaryCategory == MembershipPrimaryCategories.NonPlayingMember).ToList(),
                baselineDate,
                categoryFeeLookup);

            var points = dataPoints
                .Where(d => d.Date.Date >= windowStart && d.Date.Date <= windowEnd)
                .OrderBy(d => d.Date.Date)
                .ToList();

            var byMonth = new Dictionary<string, decimal>(StringComparer.Ordinal);

            foreach (var day in points)
            {
                var deficitPlaying = Math.Max(0, baselinePlayingCount - day.PlayingMembers);
                var deficitNonPlaying = Math.Max(0, baselineNonPlayingCount - day.NonPlayingMembers);

                var lostPlaying = SumTopK(baselinePlayingDailyFeesDesc, deficitPlaying);
                var lostNonPlaying = SumTopK(baselineNonPlayingDailyFeesDesc, deficitNonPlaying);

                var dailyLost = lostPlaying + lostNonPlaying;
                if (dailyLost == 0m) continue;

                var monthKey = day.Date.ToString("yyyy-MM");
                byMonth[monthKey] = byMonth.GetValueOrDefault(monthKey) + dailyLost;
            }

            foreach (var k in byMonth.Keys.ToList())
            {
                byMonth[k] = Math.Round(byMonth[k], 2, MidpointRounding.AwayFromZero);
            }

            return new LostRevenueBreakdownDto
            {
                ByMonth = byMonth,
                Total = Math.Round(byMonth.Values.Sum(), 2, MidpointRounding.AwayFromZero)
            };
        }

        private List<decimal> BuildBaselineDailyFeesDescending(
            List<MemberDto> members,
            DateTime date,
            Dictionary<(string category, DateTime date), Fee[]> categoryFeeLookup)
        {
            var fees = new List<decimal>(members.Count);

            foreach (var m in members)
            {
                var cat = (m.MembershipCategory ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(cat)) continue;

                if (!categoryFeeLookup.TryGetValue((cat, date.Date), out var arr) || arr.Length == 0) continue;

                Fee? chosenFee;

                if (date.Date < new DateTime(2025, 4, 1))
                {
                    chosenFee =
                        (m.JoinDate.HasValue && m.JoinDate.Value.Date < new DateTime(2024, 4, 1))
                            ? arr.FirstOrDefault(f => f.YearStart == 23)
                            : arr.FirstOrDefault(f => f.YearStart == 24);

                    chosenFee ??= arr.OrderBy(f => f.YearStart).FirstOrDefault();
                }
                else
                {
                    chosenFee = arr.FirstOrDefault();
                }

                if (chosenFee == null) continue;

                fees.Add(chosenFee.Amount / 365m);
            }

            fees.Sort();
            fees.Reverse();
            return fees;
        }

        private decimal SumTopK(List<decimal> descFees, int k)
        {
            if (k <= 0 || descFees.Count == 0) return 0m;
            if (k > descFees.Count) k = descFees.Count;

            decimal sum = 0m;

            for (var i = 0; i < k; i++)
            {
                sum += descFees[i];
            }

            return sum;
        }

    }
}
