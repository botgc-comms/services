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
                { "gender", newMember.Gender.ToString() },
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

                { "cdh_id_option", newMember.HasCdhId ? "yes" : "no" },
                { "cdh_id_lookup", newMember.CdhId ?? "" },

                { "membercategory", newMember.MembershipCategory },
                { "paymenttype", null }, // Need to work out what we do here

                { "memberstatus", "Pending" },
                { "new_memberid", "0" },
                { "new_cardswipe", "" },

                { "joindate", DateTime.UtcNow.ToString("dd/MM/yyyy") },
                { "applicationdate", newMember.ApplicationDate.ToString("dd/MM/yyyy") },

                { "till_discount_group_id", "0" }
            };

            if (cdhLookup != null)
            {
                data["cdh_id"] = cdhLookup.CdhId;
                data["handicap_index"] = cdhLookup.HandicapIndex?.ToString(CultureInfo.InvariantCulture) ?? "";
                data["whs_member_uid"] = cdhLookup.WhsMemberUid ?? "";
                data["clubName"] = cdhLookup.ClubName ?? "";
                data["clubUid"] = cdhLookup.ClubUid ?? "";
            }

            return data;
        }

    }
}
