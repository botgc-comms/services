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
            return await PrepareManagementReport(null, cancellationToken);
        }

        private async Task<MembershipReportDto> PrepareManagementReport(int? filterByMemberId, CancellationToken cancellationToken)
        { 
            var membersQuery = new GetMembershipReportQuery();
            var membershipReport = await _mediator.Send(membersQuery, cancellationToken);

            var members = membershipReport.Where(m => (m.MembershipStatus != "W" && m.MemberNumber != 0) || m.MembershipStatus == "W").ToList();
            if (filterByMemberId != null) members = members.Where(m => m.MemberNumber == filterByMemberId).ToList();

            var report = new MembershipReportDto();
            var today = DateTime.UtcNow.Date;

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

            report.Today = today;
            report.SubscriptionYearStart = startOfCurrentSubsYear;
            report.SubscriptionYearEnd = endOfCurrentSubsYear;
            report.FinancialYearStart = startOfCurrentFinancialYear;
            report.FinancialYearEnd = endOfCurrentFinancialYear;

            // Load budget and subscription fees
            var (categoryFees, categoryFeeLookup, dailyTargetRevenue) = await LoadRevenueConfig(startOfCurrentFinancialYear, endOfCurrentFinancialYear);
            
            // Get all of the events that have taken place up to the start of the last financial year
            var memberEventsQuery = new GetMemberEventsQuery
            {
                FromDate = startOfPreviousSubsYear.AddDays(-1),
                ToDate = today
            };

            var memberEvents = await _mediator.Send(memberEventsQuery);

            // First time a member appears on the waiting list (by event or ApplicationDate)
            var firstAppliedAt = new Dictionary<int, DateTime>();

            // From events: first non-W -> W
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

            // From data: ApplicationDate present (covers members created directly as W)
            foreach (var m in members.Where(m => m.MemberNumber.HasValue && m.MemberNumber.Value != 0 && m.ApplicationDate.HasValue))
            {
                var id = m.MemberNumber!.Value;
                var d = m.ApplicationDate!.Value.Date;
                if (!firstAppliedAt.TryGetValue(id, out var existing) || d < existing)
                    firstAppliedAt[id] = d;
            }

            // Get Subscription Payments
            var (previousSubscriptionYearPayments, currentSubscriptionYearPayments)
                = await GetReportSubscriptionPaymentsAsync(
                      startOfPreviousSubsYear,
                      endOfPreviousSubsYear,
                      startOfCurrentSubsYear,
                      endOfCurrentSubsYear,
                      startOfPreviousFinancialYear, 
                      cancellationToken, 
                      filterByMemberId);

            // You may keep them separate or merge for the daily-spread step:
            var allPayments = previousSubscriptionYearPayments
                                  .Concat(currentSubscriptionYearPayments);

            if (filterByMemberId != null) allPayments = allPayments.Where(p => p.MemberId == filterByMemberId).ToList();          

            var (dailyBilled, dailyReceived) = BuildDailyRevenueSpread(allPayments, members, startOfCurrentFinancialYear, endOfCurrentFinancialYear);

            // Start with todays results
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

            // Process each day backwards from yesterday to the start of the previous financial year
            for (var currentDate = today.AddDays(-1); currentDate >= startOfPreviousSubsYear.AddDays(-1); currentDate = currentDate.AddDays(-1))
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

                        // Sort once (chronological for counting; we’ll use the same array reversed for rewinding)
                        var ordered = byMember
                            .OrderBy(e => e.DateOfChange!.Value)
                            .ThenBy(e => e.ChangeIndex)
                            .ToList();

                        if (ordered.Count == 0) continue;

                        // Have they *ever* applied (by event or ApplicationDate) on/before today?
                        bool everApplied = firstAppliedAt.TryGetValue(memberId, out var firstAppDate)
                                           && firstAppDate <= currentDate.Date;

                        // ── 1) Build reduced path of (Status, Category) with loop-collapsing ──
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

                        // Net no-op → ignore for WL counts
                        if (!(path.Count < 2 || (Eq(path[0].S, path[^1].S) && Eq(path[0].C, path[^1].C))))
                        {
                            // ── Count WL crossings on reduced path (keys = raw category strings) ──
                            for (int i = 1; i < path.Count; i++)
                            {
                                var prev = path[i - 1];
                                var next = path[i];

                                // non-W → W  = application (attribute to next category)
                                if (!Eq(prev.S, "W") && Eq(next.S, "W"))
                                {
                                    var key = MembershipHelper.ResolveCategoryGroup(next.C);
                                    wlApps[key] = wlApps.GetValueOrDefault(key) + 1;
                                    appliedByEventToday.Add(memberId);
                                    continue;
                                }

                                // W → non-W outcome (only if they have a recorded application on/before today)
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

                        // ── 2) Rewind this member’s state using the same list, reversed ──
                        for (int i = ordered.Count - 1; i >= 0; i--)
                        {
                            var ev = ordered[i];
                            var member = members.Find(m => m.MemberNumber == ev.MemberId);
                            if (member == null)
                            {
                                _logger.LogWarning($"Expected to find a member with id {ev.MemberId} but no member was found");
                                continue;
                            }

                            // Reverse category
                            if (member.MembershipCategory == ev.ToCategory)
                                member.MembershipCategory = ev.FromCategory;
                            else
                                _logger.LogWarning($"Expected current category to be {ev.ToCategory} but found {member.MembershipCategory} for member id {member.MemberNumber} on {currentDate:yyyy-MM-dd}");

                            // Reverse status
                            if (member.MembershipStatus == ev.ToStatus)
                                member.MembershipStatus = ev.FromStatus;
                            else
                                _logger.LogWarning($"Expected current status to be {ev.ToStatus} but found {member.MembershipStatus} for member id {member.MemberNumber} on {currentDate:yyyy-MM-dd}");
                        }
                    }

                    // Add apps that arrived today via ApplicationDate but had no W-event today
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

                    // Update the playing status of all members
                    foreach (var member in members)
                        member.SetPrimaryCategory(currentDate).SetCategoryGroup();

                    // Build today's active members with their current group
                    var todayActiveGroups = members
                         .Where(m => m.IsActive == true && m.MemberNumber.HasValue && m.MemberNumber.Value != 0)
                         .ToDictionary(m => m.MemberNumber!.Value, m => m.MembershipCategoryGroup);

                    var (joinersByGroup, leaversByGroup) = ComputeGroupJoinersAndLeavers(previousDayGroupings, todayActiveGroups);

                    // Compute and store the statistics for this day after applying reversals
                    var dailyReportEntry = GetReportEntry(currentDate, members, categoryFees, categoryFeeLookup, dailyTargetRevenue, dailyReceived, dailyBilled);

                    dailyReportEntry.DailyJoinersByCategoryGroup = joinersByGroup;
                    dailyReportEntry.DailyLeaversByCategoryGroup = leaversByGroup;

                    // Attach to the daily entry (as you already do after building dailyReportEntry)
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

            // Fill future days + targets first
            EnsureFullYearData(dataPoints, endOfCurrentSubsYear, dailyTargetRevenue);
            ApplyGrowthTargets(dataPoints, startOfCurrentFinancialYear, endOfCurrentFinancialYear);

            // Then pin the output list for this report window
            report.DataPoints = dataPoints
                .Where(dp => dp.Date >= startOfPreviousSubsYear && dp.Date <= endOfCurrentSubsYear)
                .ToList();
            report.DataPointsCsv = ConvertToCsv(report.DataPoints);

            // Build month/quarter snapshots, then aggregate WL into them
            EnsureMonthlyAndQuarterlyStats(report, monthlySnapshots);
            PopulateWaitingListAggregatesForPeriods(report.MonthlyStats, report.DataPoints);
            PopulateWaitingListAggregatesForPeriods(report.QuarterlyStats, report.DataPoints);

            var totalActualRevenue = report.DataPoints.Where(dp => dp.Date >= new DateTime(2024, 10, 1) && dp.Date <= new DateTime(2025, 9, 30)).Select(dp => dp.ActualRevenue).Sum();
            var totalBilledRevenue = report.DataPoints.Where(dp => dp.Date >= new DateTime(2024, 10, 1) && dp.Date <= new DateTime(2025, 9, 30)).Select(dp => dp.BilledRevenue).Sum();
            var totalReceivedRevenue = report.DataPoints.Where(dp => dp.Date >= new DateTime(2024, 10, 1) && dp.Date <= new DateTime(2025, 9, 30)).Select(dp => dp.ReceivedRevenue).Sum();

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

            if (startOfPreviousSubsYear.Year == 2024)          // year we transitioned
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

            // 23/24 was a special year as we transition to a different subscription period
            // The following code works out how to assign billed and received payyment amounts
            // to the correct financial year and subscription year.
            if (startOfPreviousSubsYear.Year == 2024)          
            {

                var headline18m = await BuildHeadline18mFeesAsync();

                var synthetic24_25 = MakeSyntheticInvoices24_25(
                                         transitionInvoices, headline18m);

                previousInvoices.AddRange(synthetic24_25);
            }

            return (previousInvoices, currentInvoices);
        }


        /// <summary>
        /// Fallback: create a synthetic invoice for Apr-24 → Mar-25 by simple
        /// daily pro-rata when the headline-fee inference fails.
        /// </summary>
        private SubscriptionPaymentDto MakeDailyProrataSlice(
                SubscriptionPaymentDto inv,
                DateTime sliceStart,
                DateTime sliceEnd)
        {
            // Work out the coverage window the club intended for this invoice
            var coverStart = inv.DateDue.Day >= 16
                ? new DateTime(inv.DateDue.Year, inv.DateDue.Month, 1).AddMonths(1)
                : new DateTime(inv.DateDue.Year, inv.DateDue.Month, 1);

            var coverEnd = new DateTime(2025, 3, 31);          // fixed
            if (coverStart > coverEnd) coverEnd = coverStart;    // safety

            var totalDays = (coverEnd - coverStart).Days + 1;
            if (totalDays <= 0) totalDays = 1;                   // guard ÷0

            var dailyRate = inv.BillAmount / totalDays;

            // Slice to Apr-24 → Mar-25
            var sliceCovStart = sliceStart > coverStart ? sliceStart : coverStart;
            var sliceDays = (sliceEnd - sliceCovStart).Days + 1;
            if (sliceDays <= 0) sliceDays = 0;

            var sliceBill = Math.Round(dailyRate * sliceDays, 2,
                                       MidpointRounding.AwayFromZero);

            var paidFactor = inv.BillAmount == 0m ? 0m : sliceBill / inv.BillAmount;
            var slicePaid = Math.Round((inv.AmountPaid ?? 0m) * paidFactor, 2,
                                        MidpointRounding.AwayFromZero);

            // First payment on/after the slice window
            var payDate = inv.PaymentDate.HasValue && inv.PaymentDate.Value >= sliceStart
                ? inv.PaymentDate
                : (DateTime?)null;

            return new SubscriptionPaymentDto
            {
                MemberId = inv.MemberId,
                DateDue = sliceStart,          // makes it a 1-Apr-24 bill
                BillAmount = sliceBill,
                AmountPaid = slicePaid,
                PaymentDate = slicePaid > 0m ? payDate : null,
                MembershipCategory = inv.MembershipCategory?.Trim() ?? string.Empty
            };
        }

        /// <summary>
        /// Build the table of 18-month "headline" fees that were billed 01-Oct-23.
        /// Key: trimmed category string   Value: £18-month rounded to nearest £10.
        /// </summary>
        private async Task<Dictionary<string, decimal>> BuildHeadline18mFeesAsync()
        {
            // 23/24 subscription year (01-Apr-23 → 31-Mar-24) only
            var (annual, _, _) = await LoadRevenueConfig(
                                      new DateTime(2023, 4, 1),
                                      new DateTime(2024, 3, 31));

            var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in annual)               // kvp.Value = List<(start,end,Fee[])>
            {
                // One slice per category in that year – take the first Fee in the array
                var yearly = kvp.Value[0].fee[0].Amount;
                if (yearly <= 0m) continue;           // skip honorary / £0 categories

                var eighteen = Math.Round(yearly * 1.5m / 10m, 0,
                                           MidpointRounding.AwayFromZero) * 10m;

                result[kvp.Key.Trim()] = eighteen;
            }

            return result;
        }


        /// <summary>
        /// Converts the 18-month “transition” invoices (01-Oct-23 → 31-Mar-24)
        /// into clean 24/25-only synthetic invoices, validating the headline-fee
        /// rule and falling back to daily pro-rata whenever an invoice cannot be
        /// matched unambiguously.  _logger warnings are emitted for every
        /// ambiguous or unmatched bundle so you can audit the raw data later.
        /// </summary>
        private IEnumerable<SubscriptionPaymentDto> MakeSyntheticInvoices24_25(
            IEnumerable<SubscriptionPaymentDto> transitionInvoices,
            Dictionary<string, decimal> headline18m)
        {
            var sliceStart = new DateTime(2024, 10, 1); 
            var sliceEnd = new DateTime(2025, 3, 31);  

            // ───────────────────── STEP 1 – bundle by (MemberId, currentCat) ─────────────────────
            var bundles = transitionInvoices
                .GroupBy(p => (p.MemberId,
                               (p.MembershipCategory ?? string.Empty).Trim()));

            foreach (var bundle in bundles)
            {
                var (memberId, currentCat) = bundle.Key;      // tuple de-construction
                string chosenCat = null;                     // decided category
                decimal head18m = 0m;                       // its 18-month fee

                decimal totalBillAmount = 0m;

                // ───────────────────── STEP 2 – try to infer the category ─────────────────────
                foreach (var inv in bundle.OrderBy(p => p.DateDue))
                {
                    totalBillAmount += inv.BillAmount;
                    if (inv.BillAmount <= 0m) continue;       // ignore zero rows

                    var candidates = headline18m
                        .Where(kv =>
                        {
                            if (kv.Value <= 0m) return false;

                            var monthly = kv.Value / 18m;
                            var estMonths = inv.BillAmount / monthly;
                            var nearestMon = Math.Round(estMonths, MidpointRounding.AwayFromZero);

                            // *** new guard: ignore nonsense like 300 or 600 months ***
                            if (nearestMon < 1 || nearestMon > 18) return false;

                            var reconFee = Math.Round(nearestMon * monthly / 10m, 0,
                                                      MidpointRounding.AwayFromZero) * 10m;
                            return Math.Abs(reconFee - inv.BillAmount) <= 1m;
                        })
                        .Select(kv => kv.Key)
                        .ToList();

                    if (candidates.Count == 0)
                    {
                        // keep looping – maybe another invoice in the bundle helps
                        continue;
                    }
                    else if (candidates.Count == 1)
                    {
                        chosenCat = candidates[0];
                        head18m = headline18m[chosenCat];
                        break;
                    }
                    else  // 2+ matches
                    {
                        if (!string.IsNullOrWhiteSpace(currentCat) &&
                            candidates.Contains(currentCat))
                        {
                            chosenCat = currentCat;           // tie-break with currentCat
                            head18m = headline18m[chosenCat];
                            break;
                        }
                        // still ambiguous – check next invoice
                    }
                }

                // ───────────────────── STEP 3 – fall-back if no clear match ─────────────────────
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

                // ───────────────────── STEP 4 – union of all months covered ─────────────────────
                var monthSet = new HashSet<(int year, int month)>();

                foreach (var inv in bundle)
                {
                    var covStart = inv.DateDue.Day >= 16
                                 ? new DateTime(inv.DateDue.Year, inv.DateDue.Month, 1).AddMonths(1)
                                 : new DateTime(inv.DateDue.Year, inv.DateDue.Month, 1);

                    for (var m = covStart; m <= sliceEnd; m = m.AddMonths(1))
                        monthSet.Add((m.Year, m.Month));
                }

                // ─── replace the months-filter (STEP 5) ───────────────────────────────
                var months24_25 = monthSet.Count(t =>
                       (t.year > 2024 || (t.year == 2024 && t.month >= 10)) &&   // Oct-24+
                       (t.year < 2025 || (t.year == 2025 && t.month <= 3)));    // …to Mar-25

                if (months24_25 == 0)
                    continue;        // nothing for this member in 24/25

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

                // ─────────── STEP 6 – emit synthetic invoice for Apr-24 ────────────
                yield return new SubscriptionPaymentDto
                {
                    MemberId = memberId,
                    DateDue = sliceStart,               // 01-Apr-24
                    BillAmount = sliceBill,
                    AmountPaid = slicePaid,
                    PaymentDate = slicePaid > 0m ? firstPayOnOrAfter : (DateTime?)null,
                    MembershipCategory = chosenCat
                };

                // Log success if the bundle contained more than one raw invoice
                if (bundle.Count() > 1)
                {
                    _logger.LogDebug(
                        $"[TRANSITION]  Member {memberId}: merged {bundle.Count()} " +
                        $"transition invoices into synthetic £{sliceBill:N0} ({chosenCat}).");
                }
            }
        }

        private (Dictionary<string, int> Joiners, Dictionary<string, int> Leavers) ComputeGroupJoinersAndLeavers(Dictionary<int, string> previousDayGroups,  Dictionary<int, string> todayGroups)
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
            return (date.Month == 3 && date.Day == 31) ||  // Q1 End (Mar 31)
                   (date.Month == 6 && date.Day == 30) ||  // Q2 End (Jun 30)
                   (date.Month == 9 && date.Day == 30) ||  // Q3 End (Sep 30)
                   (date.Month == 12 && date.Day == 31);   // Q4 End (Dec 31)
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
                            // Pick based on join date: before 1-Apr-24 ⇒ 23/24 price, else 24/25
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
            decimal receivedRevenue = dailyReceived.TryGetValue(date, out var r) ? r : 0m;

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
        )> LoadRevenueConfig(DateTime financialYearStart, DateTime financialYearEnd)
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

                if (subEnd < financialYearStart || subStart > financialYearEnd) continue;

                var feeData = JsonSerializer.Deserialize<Dictionary<string, decimal>>(await File.ReadAllTextAsync(file))!;
                foreach (var kvp in feeData)
                {
                    if (!categoryFees.TryGetValue(kvp.Key, out var list))
                    {
                        list = new List<(DateTime start, DateTime end, Fee[] fee)>();
                        categoryFees[kvp.Key] = list;
                    }
                    list.Add((subStart, subEnd, [ new Fee(startYear - 2000, endYear - 2000, kvp.Value) ]));
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

            var budgetFileKey = $"{financialYearStart.Year % 100}{financialYearEnd.Year % 100:D2}";
            var budgetFileName = $"Membership Subscription Budget {budgetFileKey}.json";
            var budgetPath = Path.Combine(basePath, budgetFileName);

            var budgetYearStart = int.Parse($"{financialYearStart.Year % 100}");
            var budgetYearEnd = int.Parse($"{financialYearEnd.Year % 100}");

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
                        if (current >= financialYearStart && current <= financialYearEnd)
                            dailyTargetRevenue[current] = [ new Fee(budgetYearStart, budgetYearEnd, dailyAmount) ];
                    }
                }
            }
            else
            {
                _logger.LogWarning("Failed to load budget file: " + budgetPath);
            }

            if (financialYearStart <= new DateTime(2024, 10, 1) &&
                financialYearEnd >= new DateTime(2025, 3, 31))
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
                            // There is already a 24/25 Fee[] there – append legacy one
                            if (categoryFeeLookup.TryGetValue((cat, d), out var arr))
                            {
                                var combined = new Fee[arr.Length + 1];
                                Array.Copy(arr, combined, arr.Length);
                                combined[^1] = legacyFee;
                                categoryFeeLookup[(cat, d)] = combined;
                            }
                            else
                            {
                                // Rare: category existed only in 23/24
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

        /// <summary>
        /// Converts every subscription invoice / payment into daily values **only for
        /// the part that falls inside the current financial year (fyStart → fyEnd)**.
        /// </summary>
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

                // ----- 1.  Determine the invoice’s *intended* coverage window ----------
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

                // Transition invoices dated 01-Oct-24 start exactly on the due-date
                if (p.DateDue == new DateTime(2024, 10, 1))
                    coverStart = p.DateDue;

                var coverEnd = subEnd < (m.LeaveDate?.Date ?? subEnd)
                             ? subEnd
                             : m.LeaveDate?.Date ?? subEnd;

                // No sensible window?
                if (coverStart > coverEnd) continue;

                // ----- 2.  Remember original window length for scaling  -----------------
                var origDays = Math.Max((coverEnd - coverStart).Days + 1, 1);

                // ----- 3.  Clip to FY window -------------------------------------------
                if (coverStart < fyStart) coverStart = fyStart;
                if (coverEnd > fyEnd) coverEnd = fyEnd;
                if (coverStart > coverEnd) continue;          // fell completely outside FY

                var clipDays = Math.Max((coverEnd - coverStart).Days + 1, 1);
                var clipRatio = clipDays / (decimal)origDays;

                // ----- 4.  BILLED (pro-rata slice) --------------------------------------
                var billSlice = p.BillAmount * clipRatio;
                var perDayBill = billSlice / clipDays;

                for (var d = coverStart; d <= coverEnd; d = d.AddDays(1))
                    billed[d] = billed.GetValueOrDefault(d) + perDayBill;

                // ----- 5.  RECEIVED (pro-rata slice from PaymentDate onward) ------------
                if (p.AmountPaid is decimal paid && paid > 0m && p.PaymentDate.HasValue)
                {
                    var payStart = p.PaymentDate.Value.Date;
                    if (payStart < coverStart) payStart = coverStart;
                    if (payStart > coverEnd) continue;      // payment after FY

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

        /// <summary>
        /// Per-member billing sanity-check for the current FY.
        /// Walks their category history, computes what they *should* have paid,
        /// compares with what we actually billed, and returns a list of deltas.
        /// </summary>
        private IEnumerable<MemberBillingCheckDto> CheckMemberBillingAccuracy(
                IEnumerable<MemberDto> members,
                IEnumerable<MemberEventDto> memberEvents,   // category / status change feed
                IEnumerable<SubscriptionPaymentDto> payments,       // real + synthetic
                Dictionary<(string, DateTime), decimal> categoryFeeLookup,
                DateTime fyStart,
                DateTime fyEnd,
                decimal tolerance = 1m)                            // £1 default tolerance
        {
            // Build quick look-ups
            var paymentsByMember = payments
                .GroupBy(p => p.MemberId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var eventsByMember = memberEvents
                .GroupBy(e => e.MemberId)
                .ToDictionary(g => g.Key, g => g
                    .Where(e => e.DateOfChange.HasValue)
                    .OrderBy(e => e.DateOfChange!.Value)
                    .ToList());

            foreach (var m in members.Where(m => m.MemberNumber > 0))
            {
                var memberId = m.MemberNumber!.Value;
                var expected = 0m;

                // Build a timeline of (from, to, category) inside FY
                var slices = new List<(DateTime from, DateTime to, string category)>();
                var currentCat = m.MembershipCategory?.Trim() ?? "";
                var cursor = fyEnd;               // rewind cursor

                if (eventsByMember.TryGetValue(memberId, out var evts))
                {
                    // Traverse events backwards so we can build contiguous slices
                    foreach (var ev in evts.OrderByDescending(e => e.DateOfChange!.Value))
                    {
                        var changeDate = ev.DateOfChange!.Value.Date;

                        if (changeDate > cursor) continue;           // event after FY
                        var sliceStart = changeDate.AddDays(1);      // day *after* change
                        var sliceEnd = cursor;
                        if (sliceEnd >= fyStart && sliceStart <= fyEnd)
                        {
                            slices.Add((sliceStart < fyStart ? fyStart : sliceStart,
                                        sliceEnd,
                                        currentCat));
                        }
                        cursor = changeDate;
                        currentCat = ev.FromCategory?.Trim() ?? currentCat;
                    }
                }

                // Final slice back to either join date or fyStart
                var fySliceStart = fyStart;
                if (m.JoinDate.HasValue && m.JoinDate.Value.Date > fySliceStart)
                    fySliceStart = m.JoinDate.Value.Date;

                if (cursor >= fySliceStart)
                    slices.Add((fySliceStart, cursor, currentCat));

                // Trim against leave-date if present
                if (m.LeaveDate.HasValue && m.LeaveDate.Value.Date < fyEnd)
                {
                    slices = slices
                             .Where(s => s.from <= m.LeaveDate.Value.Date)
                             .Select(s =>
                               (s.from,
                                s.to <= m.LeaveDate.Value.Date ? s.to : m.LeaveDate.Value.Date,
                                s.category))
                             .ToList();
                }

                // ---------- expected = Σ daily fee over slices ----------
                foreach (var s in slices)
                {
                    for (var d = s.from.Date; d <= s.to.Date; d = d.AddDays(1))
                    {
                        if (categoryFeeLookup.TryGetValue((s.category, d), out var annualFee))
                            expected += annualFee / 365m;
                        else
                            _logger.LogWarning($"No fee for {s.category} on {d:yyyy-MM-dd}");
                    }
                }

                // ---------- billed = Σ invoice coverage over same window ----------
                decimal billed = 0m;
                if (paymentsByMember.TryGetValue(memberId, out var invs))
                {
                    foreach (var p in invs)
                    {
                        var invStart = new DateTime(p.DateDue.Year, 4, 1);
                        var invEnd = invStart.AddYears(1).AddDays(-1);

                        var covStart = invStart;
                        if (p.DateDue == new DateTime(2024, 10, 1))        // synthetic split
                            covStart = p.DateDue;

                        var from = covStart < fyStart ? fyStart : covStart;
                        var to = invEnd > fyEnd ? fyEnd : invEnd;
                        if (from > to) continue;

                        var days = (to - from).Days + 1;
                        billed += p.BillAmount * (days / (decimal)((invEnd - covStart).Days + 1));
                    }
                }

                var delta = billed - expected;
                if (Math.Abs(delta) > tolerance)
                {
                    yield return new MemberBillingCheckDto(
                        memberId,
                        $"{m.FirstName} {m.LastName}",
                        Math.Round(expected, 2),
                        Math.Round(billed, 2),
                        Math.Round(delta, 2));
                }
            }
        }

        private MembershipDeltaDto ComputeMembershipDelta(MembershipSnapshotDto fromSnapshot, MembershipSnapshotDto toSnapshot, string periodDescription)
        {
            // Filter out future joiners from both snapshots**
            var filteredFromMembers = fromSnapshot.Members
                .Where(m => !m.JoinDate.HasValue || m.JoinDate!.Value <= fromSnapshot.SnapshotDate)
                .Where(m => m.MemberNumber != 0)
                .ToDictionary(m => m.MemberNumber!.Value);

            var filteredToMembers = toSnapshot.Members
                .Where(m => !m.JoinDate.HasValue || m.JoinDate!.Value <= toSnapshot.SnapshotDate)
                .Where(m => m.MemberNumber != 0)
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

            var categoryGroupTotals = toSnapshot.Members
                .Where(m => m.IsActive == true)
                .GroupBy(m => m.MembershipCategoryGroup ?? "Other")
                .ToDictionary(g => g.Key, g => g.Count());

            // Category Changes: Members who exist in both but changed category**
            var categoryChanges = new Dictionary<string, int>();

            foreach (var member in filteredToMembers.Values)
            {
                // Show movement between category groups
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

        private void AddToCount(Dictionary<string, int> dict, string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            if (!dict.TryAdd(key, 1))
                dict[key]++;
        }

    }
}
