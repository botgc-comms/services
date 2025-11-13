using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using BOTGC.API.Dto;

namespace BOTGC.API.Services
{
    public static class CompetitionWinnersHtmlBuilder
    {
        private static readonly Lazy<string?> LogoDataUri = new(GetLogoDataUri);

        public static string BuildWinnersPageHtml(CompetitionWinningsSummaryDto summary)
        {
            if (summary == null) throw new ArgumentNullException(nameof(summary));

            var culture = CultureInfo.GetCultureInfo("en-GB");
            var dateText = summary.CompetitionDate.ToString("d MMM yyyy", culture);

            var title = $"{summary.CompetitionName} – Results & Prize Winners";
            var encodedTitle = WebUtility.HtmlEncode(title);
            var encodedCompName = WebUtility.HtmlEncode(summary.CompetitionName);
            var encodedDate = WebUtility.HtmlEncode(dateText);
            var logoDataUri = LogoDataUri.Value;

            var sb = new StringBuilder();

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"en\">");
            sb.AppendLine("<head>");
            sb.AppendLine("  <meta charset=\"utf-8\" />");
            sb.AppendLine($"  <title>{encodedTitle}</title>");
            sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
            sb.AppendLine("  <style>");
            sb.AppendLine("    html, body { margin: 0; padding: 0; }");
            sb.AppendLine("    body {");
            sb.AppendLine("      font-family: 'DM Sans', Arial, sans-serif;");
            sb.AppendLine("      background: #f0f2f5;");
            sb.AppendLine("      color: #222;");
            sb.AppendLine("      display: flex;");
            sb.AppendLine("      justify-content: center;");
            sb.AppendLine("      padding: 24px 12px;");
            sb.AppendLine("      box-sizing: border-box;");
            sb.AppendLine("    }");
            sb.AppendLine("    .page-wrapper {");
            sb.AppendLine("      width: 100%;");
            sb.AppendLine("      max-width: 900px;");
            sb.AppendLine("      background-color: #ffffff;");
            sb.AppendLine("      padding: 24px 24px 28px;");
            sb.AppendLine("      border-radius: 10px;");
            sb.AppendLine("      box-shadow: 0 4px 16px rgba(0, 0, 0, 0.12);");
            sb.AppendLine("      box-sizing: border-box;");
            sb.AppendLine("    }");
            sb.AppendLine("    .logo {");
            sb.AppendLine("      text-align: center;");
            sb.AppendLine("      margin-bottom: 12px;");
            sb.AppendLine("    }");
            sb.AppendLine("    .logo img {");
            sb.AppendLine("      max-width: 180px;");
            sb.AppendLine("      width: 30vw;");
            sb.AppendLine("      height: auto;");
            sb.AppendLine("      filter: drop-shadow(0 4px 12px rgba(0, 0, 0, 0.25));");
            sb.AppendLine("    }");
            sb.AppendLine("    h1 {");
            sb.AppendLine("      font-size: 24px;");
            sb.AppendLine("      margin: 0 0 4px;");
            sb.AppendLine("      text-align: center;");
            sb.AppendLine("      color: #585c7b;");
            sb.AppendLine("    }");
            sb.AppendLine("    p.meta {");
            sb.AppendLine("      color: #666;");
            sb.AppendLine("      margin: 0 0 16px;");
            sb.AppendLine("      text-align: center;");
            sb.AppendLine("      font-size: 14px;");
            sb.AppendLine("    }");
            sb.AppendLine("    p.intro {");
            sb.AppendLine("      margin: 0 0 16px;");
            sb.AppendLine("      text-align: center;");
            sb.AppendLine("      font-size: 14px;");
            sb.AppendLine("      color: #555;");
            sb.AppendLine("    }");
            sb.AppendLine("    h2 {");
            sb.AppendLine("      font-size: 18px;");
            sb.AppendLine("      margin: 24px 0 8px;");
            sb.AppendLine("      text-align: left;");
            sb.AppendLine("      color: #3a3f63;");
            sb.AppendLine("    }");
            sb.AppendLine("    table.winners-table {");
            sb.AppendLine("      border-collapse: collapse;");
            sb.AppendLine("      width: 100%;");
            sb.AppendLine("      font-size: 13px;");
            sb.AppendLine("      margin-bottom: 12px;");
            sb.AppendLine("    }");
            sb.AppendLine("    table.winners-table th, table.winners-table td {");
            sb.AppendLine("      padding: 6px 8px;");
            sb.AppendLine("      border-bottom: 1px solid #e0e0e0;");
            sb.AppendLine("      text-align: left;");
            sb.AppendLine("      vertical-align: top;");
            sb.AppendLine("      box-sizing: border-box;");
            sb.AppendLine("    }");
            sb.AppendLine("    table.winners-table thead th {");
            sb.AppendLine("      background-color: #f7f8fb;");
            sb.AppendLine("      font-weight: 600;");
            sb.AppendLine("      color: #444;");
            sb.AppendLine("    }");
            sb.AppendLine("    .col-position { width: 18%; }");
            sb.AppendLine("    .col-player { width: 52%; }");
            sb.AppendLine("    .col-prize { width: 30%; text-align: right; }");
            sb.AppendLine("    td.col-prize { text-align: right; }");
            sb.AppendLine("    .footer-note {");
            sb.AppendLine("      margin-top: 24px;");
            sb.AppendLine("      font-size: 11px;");
            sb.AppendLine("      color: #777;");
            sb.AppendLine("      text-align: center;");
            sb.AppendLine("    }");
            sb.AppendLine("    @media (max-width: 600px) {");
            sb.AppendLine("      .page-wrapper { padding: 16px 12px 20px; }");
            sb.AppendLine("      h1 { font-size: 20px; }");
            sb.AppendLine("      h2 { font-size: 16px; }");
            sb.AppendLine("      table.winners-table { font-size: 12px; }");
            sb.AppendLine("    }");
            sb.AppendLine("  </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("  <div class=\"page-wrapper\">");

            if (!string.IsNullOrEmpty(logoDataUri))
            {
                sb.AppendLine("    <div class=\"logo\">");
                sb.AppendLine($"      <img src=\"{logoDataUri}\" alt=\"Burton-on-Trent Golf Club logo\" />");
                sb.AppendLine("    </div>");
            }

            sb.AppendLine($"    <h1>{encodedCompName}</h1>");
            sb.AppendLine($"    <p class=\"meta\">Played on {encodedDate}</p>");
            sb.AppendLine("    <p class=\"intro\">Official results and prize winners from Burton-on-Trent Golf Club.</p>");

            if (summary.Divisions == null || summary.Divisions.Count == 0)
            {
                sb.AppendLine("    <p>No recorded prize winners for this competition.</p>");
            }
            else
            {
                var anyPrize = summary.Divisions
                    .SelectMany(d => d.Placings ?? new List<PlacingDto>())
                    .Any(p => p.Amount > 0m);

                if (!anyPrize)
                {
                    sb.AppendLine("    <p style=\"text-align:center\">No recorded prize winners for this competition.</p>");
                }
                else
                {
                    foreach (var division in summary.Divisions.OrderBy(d => d.DivisionNumber))
                    {
                        var divName = string.IsNullOrWhiteSpace(division.DivisionName)
                            ? $"Division {division.DivisionNumber}"
                            : division.DivisionName;

                        var encodedDivName = WebUtility.HtmlEncode(divName);

                        var paidPlacings = (division.Placings ?? new List<PlacingDto>())
                            .Where(p => p.Amount > 0m)
                            .OrderBy(p => p.Position)
                            .ToList();

                        if (paidPlacings.Count == 0)
                        {
                            continue;
                        }

                        sb.AppendLine($"    <h2>{encodedDivName}</h2>");
                        sb.AppendLine("    <table class=\"winners-table\">");
                        sb.AppendLine("      <thead>");
                        sb.AppendLine("        <tr>");
                        sb.AppendLine("          <th class=\"col-position\">Position</th>");
                        sb.AppendLine("          <th class=\"col-player\">Player</th>");
                        sb.AppendLine("          <th class=\"col-prize\">Prize</th>");
                        sb.AppendLine("        </tr>");
                        sb.AppendLine("      </thead>");
                        sb.AppendLine("      <tbody>");

                        foreach (var placing in paidPlacings)
                        {
                            var ordinal = ToOrdinal(placing.Position);
                            var playerName = string.IsNullOrWhiteSpace(placing.CompetitorName)
                                ? placing.CompetitorId
                                : placing.CompetitorName;

                            var encodedPlayer = WebUtility.HtmlEncode(playerName ?? string.Empty);
                            var encodedPos = WebUtility.HtmlEncode(ordinal);
                            var amount = placing.Amount.ToString("£0.00", culture);
                            var encodedAmount = WebUtility.HtmlEncode(amount);

                            sb.AppendLine("        <tr>");
                            sb.AppendLine($"          <td class=\"col-position\">{encodedPos}</td>");
                            sb.AppendLine($"          <td class=\"col-player\">{encodedPlayer}</td>");
                            sb.AppendLine($"          <td class=\"col-prize\">{encodedAmount}</td>");
                            sb.AppendLine("        </tr>");
                        }

                        sb.AppendLine("      </tbody>");
                        sb.AppendLine("    </table>");
                    }
                }
            }

            sb.AppendLine("    <div class=\"footer-note\">");
            sb.AppendLine("      This page is provided by Burton-on-Trent Golf Club to publish official competition results.");
            sb.AppendLine("    </div>");

            sb.AppendLine("  </div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }

        private static string ToOrdinal(int n)
        {
            if (n <= 0) return n.ToString(CultureInfo.InvariantCulture);

            var rem100 = n % 100;
            if (rem100 is 11 or 12 or 13) return $"{n}th";

            return (n % 10) switch
            {
                1 => $"{n}st",
                2 => $"{n}nd",
                3 => $"{n}rd",
                _ => $"{n}th"
            };
        }

        private static string? GetLogoDataUri()
        {
            const string resourceName = "BOTGC.API.assets.img.logo.png";

            try
            {
                var assembly = typeof(CompetitionWinnersHtmlBuilder).Assembly;
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    return null;
                }

                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                var bytes = ms.ToArray();
                if (bytes.Length == 0)
                {
                    return null;
                }

                var base64 = Convert.ToBase64String(bytes);
                return $"data:image/png;base64,{base64}";
            }
            catch
            {
                return null;
            }
        }
    }
}
