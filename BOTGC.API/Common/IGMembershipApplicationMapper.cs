using BOTGC.API.Dto;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace BOTGC.API.Common
{
    public static class IGMembershipApplicationMapper
    {
        public static Dictionary<string, string> MapToFormData(NewMemberApplicationDto newMember, MemberCDHLookupDto? cdhLookup = null)
        {
            var data = new Dictionary<string, string>
            {
                { "gender", MapGender(newMember.Gender) },
                { "title", newMember.Title },
                { "forename", newMember.Forename },
                { "surname", newMember.Surname },
                { "altforename", "" },
                { "dob", newMember.DateOfBirth.ToString("dd/MM/yyyy") },

                { "tel1", newMember.Telephone },
                { "tel2", newMember.AlternativeTelephone ?? "" },
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

                { "membercategory", newMember.MembershipCategory },

                { "paymenttype", "6" }, // Default to Sage Pay Online

                { "memberstatus", "W" }, // Waiting list

                { "new_memberid", "0" },
                { "new_cardswipe", "" },

                { "joindate", "" }, // Leave blank for waiting list
                { "applicationdate", newMember.ApplicationDate.ToString("dd/MM/yyyy") },

                { "till_discount_group_id", "4" } // Default to Temporary 0% as per form
            };

            // Add contact preferences if present
            if (newMember.ContactPreferences != null)
            {
                foreach (var pref in newMember.ContactPreferences)
                {
                    data[$"contact_prefs[{pref.Key}]"] = pref.Value ? "1" : "0";
                }
            }

            // Add CDH lookup details if provided
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
    }
}
