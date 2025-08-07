using BOTGC.API.Dto;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace BOTGC.API.Common
{
    public static class IGMembershipApplicationMapper
    {
        private static readonly Dictionary<string, string> MembershipCategoryLookup = new()
        {
            { "Burton Flexi 20/40", "51" },
            { "Clubhouse", "49" },
            { "Family Clubhouse", "50" },
            { "Medical Exemption", "52" },
            { "Admin", "48" },
            { "7MN - Gent 7 Day (N)", "21" },
            { "7MA - Gent 7 Day (A)", "18" },
            { "7MC - Gent 7 Day (C)", "19" },
            { "6MN - Gent 6 Day (N)", "14" },
            { "7MLT - Gent 7 Day Lifetime", "43" },
            { "6MA - Gent 6 Day (A)", "12" },
            { "6MM - Gent 6 Day (C)", "13" },
            { "5MN - Gent 5 Day (N)", "6" },
            { "5MA - Gent 5 Day (A)", "5" },
            { "7FS - Lady 7 Day (S)", "17" },
            { "7FN - Lady 7 Day (N)", "16" },
            { "7FAS - Lady 7 Day (A)(S)", "15" },
            { "7FLT - Lady 7 Day Lifetime", "44" },
            { "6FAC - Lady 6 Day (A)(C)", "7" },
            { "6FASC - Lady 6 Day (A)(S)(C)", "8" },
            { "6FSC - Lady 6 Day (S)(C)", "11" },
            { "6FN - Lady 6 Day (N)", "10" },
            { "6FC - Lady 6 Day (C)", "9" },
            { "5FS - Lady 5 Day (S)", "4" },
            { "5FAS - Lady 5 Day (A)(S)", "2" },
            { "5FA - Lady 5 Day (A)", "1" },
            { "5FN - Lady 5 Day (N)", "3" },
            { "Intermediate 22", "34" },
            { "Intermediate 23", "35" },
            { "Intermediate 24", "36" },
            { "Intermediate 25", "37" },
            { "Intermediate 26", "38" },
            { "Intermediate 27", "39" },
            { "Intermediate 28", "40" },
            { "Intermediate 29", "41" },
            { "Pathway Membership", "25" },
            { "Burton Flexi Corporate", "46" },
            { "MX - Student", "28" },
            { "Junior 8-11", "32" },
            { "Junior 12-18", "33" },
            { "SOCL - Social Members", "30" },
            { "MSH - Gent Social Honorary", "27" },
            { "FSH - Lady Social Honorary", "23" },
            { "House", "42" },
            { "STAFF - Member of Staff", "31" },
            { "Professional Player", "29" },
            { "3 Month Trial", "47" },
            { "1894", "45" }
        };

        public static Dictionary<string, string> MapToFormData(NewMemberApplicationDto newMember, MemberCDHLookupDto? cdhLookup = null)
        {
            var altTel = UkPhoneNormaliser.Standardise(newMember.AlternativeTelephone ?? "", true);
            var mobTel = UkPhoneNormaliser.Standardise(newMember.Telephone, true);

            var data = new Dictionary<string, string>
            {
                { "gender", MapGender(newMember.Gender) },
                { "title", newMember.Title },
                { "forename", NameCasingHelper.CapitaliseForename(newMember.Forename) },
                { "surname", NameCasingHelper.CapitaliseSurname(newMember.Surname) },
                { "altforename", "" },
                { "dob", newMember.DateOfBirth.ToString("dd/MM/yyyy") },

                { "tel1",  altTel ?? mobTel ?? "" },
                { "tel2", mobTel ?? "" },
                { "tel3", "" },

                { "email", newMember.Email },

                { "address1", newMember.AddressLine1 },
                { "address2", newMember.AddressLine2 },
                { "address3", "" },
                { "town", newMember.Town },
                { "county", newMember.County },
                { "country", "United Kingdom" },
                { "postcode", newMember.Postcode },

                { "cdh_id_option", newMember.HasCdhId ? "existing" : "no" },
                { "cdh_id_lookup", newMember.CdhId ?? "" },

                { "membercategory", MapMembershipCategory(newMember.MembershipCategory, newMember.Gender, newMember.DateOfBirth) },

                { "paymenttype", "6" },
                { "memberstatus", "W" },
                { "new_memberid", "0" },
                { "new_cardswipe", "" },
                { "joindate", "" },
                { "applicationdate", newMember.ApplicationDate.ToString("dd/MM/yyyy") },
                { "till_discount_group_id", "4" },
                { "addcharges", "0" }
            };

            if (newMember.ContactPreferences != null)
            {
                foreach (var pref in newMember.ContactPreferences)
                {
                    data[$"contact_prefs[{pref.Key}]"] = pref.Value ? "1" : "0";
                }
            }

            if (cdhLookup != null)
            {
                data["cdh_id"] = cdhLookup.CdhId ?? "";
                data["handicap_index"] = cdhLookup.HandicapIndex?.ToString(CultureInfo.InvariantCulture) ?? "";
                data["whs_member_uid"] = cdhLookup.WhsMemberUid ?? "";
                data["clubName"] = cdhLookup.ClubName ?? "";
                data["clubUid"] = cdhLookup.ClubUid ?? "";
            }

            return data;
        }

        private static string MapGender(string gender)
        {
            return gender switch
            {
                "M" => "1",
                "F" => "2",
                _ => ""
            };
        }

        private static string MapMembershipCategory(string userSelection, string gender, DateTime dateOfBirth)
        {
            var age = CalculateAge(dateOfBirth);

            string categoryName = userSelection switch
            {
                "7Day" when age >= 22 && age <= 29 => $"Intermediate {age}",
                "7Day" when age >= 19 && age <= 21 => "MX - Student",
                "7Day" => gender == "F" ? "7FN - Lady 7 Day (N)" : "7MN - Gent 7 Day (N)",
                "6Day" => gender == "F" ? "6FN - Lady 6 Day (N)" : "6MN - Gent 6 Day (N)",
                "5Day" => gender == "F" ? "5FN - Lady 5 Day (N)" : "5MN - Gent 5 Day (N)",
                "Intermediate" when age >= 22 && age <= 29 => $"Intermediate {age}",
                "Student" when age >= 19 && age <= 21 => "MX - Student",
                "Junior" when age >= 8 && age <= 11 => "Junior 8-11",
                "Junior" when age >= 12 && age <= 18 => "Junior 12-18",
                "Flexi" => "Burton Flexi 20/40",
                "Clubhouse" => "Clubhouse",
                "Family" => "Family Clubhouse",
                "Social" => "SOCL - Social Members",
                _ => throw new ArgumentException($"Unsupported membership category or invalid age: {userSelection}, Age: {age}")
            };

            if (!MembershipCategoryLookup.TryGetValue(categoryName, out var categoryCode))
            {
                throw new KeyNotFoundException($"Category name '{categoryName}' not found in membership category lookup.");
            }

            return categoryCode;
        }

        private static int CalculateAge(DateTime dateOfBirth)
        {
            var today = DateTime.Today;
            var age = today.Year - dateOfBirth.Year;
            if (dateOfBirth.Date > today.AddYears(-age)) age--;
            return age;
        }
    }
}
