using System;
using PhoneNumbers;

namespace BOTGC.API.Common
{
    public static class UkPhoneNormaliser
    {
        private static readonly PhoneNumberUtil phoneUtil = PhoneNumberUtil.GetInstance();

        public static string Standardise(string rawInput, bool includeTrunkPrefix = false)
        {
            if (rawInput == null) throw new ArgumentNullException(nameof(rawInput));

            try
            {
                var number = phoneUtil.Parse(rawInput, "GB");

                if (!phoneUtil.IsValidNumberForRegion(number, "GB"))
                    throw new ArgumentException($"Not a valid UK number: {rawInput}");

                if (includeTrunkPrefix)
                {
                    // national format gives e.g. "07123 456789"
                    string national = phoneUtil.Format(number, PhoneNumberFormat.NATIONAL);
                    if (national.StartsWith("0"))
                    {
                        string withoutZero = national.Substring(1);
                        return $"+{number.CountryCode} (0){withoutZero}";
                    }
                }

                return phoneUtil.Format(number, PhoneNumberFormat.INTERNATIONAL);
            }
            catch (Exception)
            {
                return rawInput;
            }
        }
    }
}
