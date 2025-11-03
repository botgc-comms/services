using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using HtmlAgilityPack;
using System.Globalization;

namespace BOTGC.API.Common;

public sealed class IGCompetitionSignupSettingsReportParser : IReportParser<CompetitionSignupSettingsDto>
{
    private readonly ILogger<IGCompetitionSignupSettingsReportParser> _logger;

    public IGCompetitionSignupSettingsReportParser(ILogger<IGCompetitionSignupSettingsReportParser> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<CompetitionSignupSettingsDto>> ParseReport(HtmlDocument document)
    {
        try
        {
            var dto = new CompetitionSignupSettingsDto();

            var paymentSection = FindPaymentOptionsSection(document);

            dto.CompetitionPaymentsRequired = ExtractYesNoByLabel(paymentSection, ".//label[@for='reqpayment']");
            dto.PaymentsFromMemberAccounts = ExtractYesNoByLabel(paymentSection, ".//label[normalize-space()='Payments from member accounts:']");
            dto.DirectPayments = ExtractYesNoByLabel(paymentSection, ".//label[normalize-space()='Direct payments:']");
            dto.PaymentDue = ExtractFreeTextByLabel(paymentSection, ".//label[normalize-space()='Payment Due:']");

            dto.Charges = ExtractCharges(paymentSection);

            return new List<CompetitionSignupSettingsDto> { dto };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing competition signup settings.");
            return new List<CompetitionSignupSettingsDto>();
        }
    }

    private static HtmlNode? FindPaymentOptionsSection(HtmlDocument doc)
    {
        // 1) Most robust: the section that contains the reqpayment label
        var byLabel = doc.DocumentNode.SelectSingleNode("//section[.//label[@for='reqpayment']]");
        if (byLabel != null) return byLabel;

        // 2) Fallback: the section that contains the charges table (Amount / Required? headers)
        var byTable = doc.DocumentNode.SelectSingleNode(
            "//section[.//table//th[contains(translate(normalize-space(.),'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'amount')]" +
            " and .//table//th[contains(translate(normalize-space(.),'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'required')]]"
        );
        if (byTable != null) return byTable;

        // 3) Fallback: the section whose title contains “Payment Options” (case/nbsp tolerant)
        var byTitle = doc.DocumentNode.SelectSingleNode(
            "//section[.//h3//*[contains(@class,'title') and " +
            "contains(translate(normalize-space(translate(., '\u00A0', ' ')),'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'), 'payment options')]]"
        );
        if (byTitle != null) return byTitle;

        // 4) Last resort: any section whose H3 text contains “Payment Options”
        var byAnyH3 = doc.DocumentNode.SelectSingleNode(
            "//section[.//h3[contains(translate(normalize-space(translate(., '\u00A0', ' ')),'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'), 'payment options')]]"
        );
        return byAnyH3;
    }


    private static bool? ExtractYesNoByLabel(HtmlNode? scope, string labelXPath)
    {
        if (scope == null) return null;
        var label = scope.SelectSingleNode(labelXPath);
        if (label == null) return null;

        var parent = label.ParentNode;
        if (parent == null) return null;

        var text = GetTrailingTextFromFormGroup(parent);
        if (string.IsNullOrWhiteSpace(text)) return null;

        var t = text.Trim().ToLowerInvariant();
        if (t.StartsWith("yes")) return true;
        if (t.StartsWith("no")) return false;
        return null;
    }

    private static string? ExtractFreeTextByLabel(HtmlNode? scope, string labelXPath)
    {
        if (scope == null) return null;
        var label = scope.SelectSingleNode(labelXPath);
        if (label == null) return null;

        var parent = label.ParentNode;
        if (parent == null) return null;

        var text = GetTrailingTextFromFormGroup(parent);
        return string.IsNullOrWhiteSpace(text) ? null : HtmlEntity.DeEntitize(text.Trim());
    }

    private static string? GetTrailingTextFromFormGroup(HtmlNode formGroupDiv)
    {
        var texts = formGroupDiv.ChildNodes
            .Where(n => n.NodeType == HtmlNodeType.Text)
            .Select(n => n.InnerText.Trim())
            .Where(s => s.Length > 0)
            .ToList();

        if (texts.Count > 0) return texts[^1];
        return null;
    }

    private static List<SignupChargeDto> ExtractCharges(HtmlNode? paymentSection)
    {
        var result = new List<SignupChargeDto>();
        if (paymentSection == null) return result;

        var table = paymentSection.SelectSingleNode(".//table[contains(@class,'table')]");
        if (table == null) return result;

        var rows = table.SelectNodes("./tr");
        if (rows == null) return result;

        foreach (var row in rows)
        {
            var cells = row.SelectNodes("./td");
            if (cells == null || cells.Count < 3) continue;

            var desc = HtmlEntity.DeEntitize(cells[0].InnerText.Trim());
            var requiredText = HtmlEntity.DeEntitize(cells[1].InnerText.Trim()).ToLowerInvariant();
            var amountText = HtmlEntity.DeEntitize(cells[2].InnerText.Trim());

            bool? required = requiredText.StartsWith("yes") ? true :
                                requiredText.StartsWith("no") ? false : (bool?)null;

            decimal? amount = TryParseDecimal(amountText);

            result.Add(new SignupChargeDto
            {
                Description = desc,
                Required = required,
                Amount = amount
            });
        }

        return result;
    }

    private static decimal? TryParseDecimal(string input)
    {
        var cleaned = new string(input.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());
        if (string.IsNullOrWhiteSpace(cleaned)) return null;

        if (decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out var val)) return val;

        if (decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.GetCultureInfo("en-GB"), out val)) return val;

        return null;
    }
}

