using System.Text;
using BOTGC.API.Dto;

namespace BOTGC.API.Services;

public static class CompetitionWinnersBlobNaming
{
    // Container to hold all public winners pages
    public const string ContainerName = "competition-winners";

    public static string GetBlobName(int competitionId, DateTime competitionDate, string competitionName)
    {
        // Use date purely for readability; id makes it unique.
        var datePart = competitionDate.ToString("yyyyMMdd");
        var safeName = ToSlug(competitionName);

        return $"{datePart}-comp-{competitionId}-{safeName}.html";
    }

    public static string GetBlobName(CompetitionWinningsSummaryDto summary)
        => GetBlobName(summary.CompetitionId, summary.CompetitionDate, summary.CompetitionName);

    private static string ToSlug(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "competition";

        value = value.Trim().ToLowerInvariant();

        var sb = new StringBuilder(value.Length);
        var lastDash = false;

        foreach (var ch in value)
        {
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(ch);
                lastDash = false;
            }
            else if (!lastDash && (char.IsWhiteSpace(ch) || ch is '-' or '_' or '/' or '\\'))
            {
                sb.Append('-');
                lastDash = true;
            }
        }

        var slug = sb.ToString().Trim('-');
        return string.IsNullOrEmpty(slug) ? "competition" : slug;
    }
}
