using System.Globalization;
using System.Text.RegularExpressions;

namespace BOTGC.API.Common
{
    public static class NameCasingHelper
    {
        private static readonly TextInfo TextInfo = CultureInfo.InvariantCulture.TextInfo;

        public static string CapitaliseName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            name = name.ToLowerInvariant();

            // Split on spaces, hyphens or apostrophes and apply capitalisation to each part
            name = Regex.Replace(name, @"\b\w+", match =>
            {
                var word = match.Value;
                if (word.Length == 0) return word;
                return char.ToUpperInvariant(word[0]) + word.Substring(1);
            });

            // Fix Mc and Mac prefixes
            name = Regex.Replace(name, @"\b(Mc|Mac)([a-z])", m =>
                m.Groups[1].Value + char.ToUpperInvariant(m.Groups[2].Value[0]) + m.Groups[2].Value.Substring(1));

            // Handle O' / O’ type names
            name = Regex.Replace(name, @"\bO['’]([a-z])", m =>
                "O’" + char.ToUpperInvariant(m.Groups[1].Value[0]) + m.Groups[1].Value.Substring(1));

            return name;
        }

        public static string CapitaliseForename(string forename)
        {
            return CapitaliseName(forename);
        }

        public static string CapitaliseSurname(string surname)
        {
            return CapitaliseName(surname);
        }
    }
}
