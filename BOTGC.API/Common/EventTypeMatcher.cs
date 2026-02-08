using static QuestPDF.Helpers.Colors;

namespace BOTGC.API.Common;

public static class EventTypeMatcher
{
    public static bool IsMatch(string candidateEventClrType, string configuredKey)
    {
        if (string.IsNullOrWhiteSpace(candidateEventClrType) || string.IsNullOrWhiteSpace(configuredKey))
        {
            return false;
        }

        candidateEventClrType = candidateEventClrType.Trim();
        configuredKey = configuredKey.Trim();

        if (candidateEventClrType.Equals(configuredKey, StringComparison.Ordinal))
        {
            return true;
        }

        var idx = configuredKey.LastIndexOf(":v", StringComparison.Ordinal);
        if (idx >= 0 && idx + 2 < configuredKey.Length)
        {
            var versionPart = configuredKey.AsSpan(idx + 2);
            if (versionPart.Length > 0 && IsAllDigits(versionPart))
            {
                return false;
            }
        }

        var prefix = configuredKey + ":v";
        return candidateEventClrType.StartsWith(prefix, StringComparison.Ordinal);
    }

    private static bool IsAllDigits(ReadOnlySpan<char> span)
    {
        for (var i = 0; i < span.Length; i++)
        {
            var c = span[i];
            if (c < '0' || c > '9')
            {
                return false;
            }
        }

        return span.Length > 0;
    }
}

