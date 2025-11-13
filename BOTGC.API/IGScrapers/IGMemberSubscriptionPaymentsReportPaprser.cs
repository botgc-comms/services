using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace BOTGC.API.IGScrapers
{
    public class IGSubscriptionPaymentsReportParser : IReportParser<SubscriptionPaymentDto>
    {
        private readonly ILogger<IGSubscriptionPaymentsReportParser> _logger;

        public IGSubscriptionPaymentsReportParser(ILogger<IGSubscriptionPaymentsReportParser> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<SubscriptionPaymentDto>> ParseReport(HtmlDocument document)
        {
            var payments = new List<SubscriptionPaymentDto>();

            var rows = document.DocumentNode.SelectNodes("//table[contains(@class, 'admin')]//tbody//tr");
            if (rows == null || rows.Count == 0)
            {
                _logger.LogWarning("No subscription payment rows found.");
                return payments;
            }

            foreach (var row in rows)
            {
                var columns = row.SelectNodes("./td")?.Select(td => td.InnerText.Trim()).ToArray();
                if (columns == null || columns.Length < 9) continue;

                var item = columns[6];
                if (!string.Equals(item, "Subscriptions", StringComparison.OrdinalIgnoreCase)) continue;

                try
                {
                    var payment = new SubscriptionPaymentDto
                    {
                        DateDue = ParseDate(columns[0]) ?? default,
                        PaymentDate = ParseDate(columns[1]),
                        MemberId = int.TryParse(columns[3], out var id) ? id : 0,
                        MembershipCategory = columns[4],
                        BillAmount = ParseDecimal(columns[7]),
                        AmountPaid = ParseDecimal(columns[8])
                    };

                    payments.Add(payment);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error parsing subscription payment row: {Row}", string.Join(", ", columns));
                }
            }

            _logger.LogInformation("Parsed {Count} subscription payments (filtered to Subscriptions only).", payments.Count);
            return payments;
        }

        private DateTime? ParseDate(string input)
        {
            if (DateTime.TryParseExact(input, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                return parsed;
            return null;
        }

        private decimal ParseDecimal(string input)
        {
            return decimal.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out var value)
                ? value
                : 0.00m;
        }
    }
}
