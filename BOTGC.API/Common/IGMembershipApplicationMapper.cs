using BOTGC.API.Dto;
using System;
using System.Collections.Generic;

namespace BOTGC.API.Common
{
    public static class IGMembershipApplicationMapper
    {
        public static Dictionary<string, string> MapToFormData(NewMemberApplicationDto dto)
        {
            return new Dictionary<string, string>
            {
                { "gender", dto.Gender.ToString() },
                { "title", dto.Title },
                { "forename", dto.Forename },
                { "surname", dto.Surname },
                { "altforename", "" },
                { "dob", dto.DateOfBirth.ToString("dd/MM/yyyy") },

                { "tel1", dto.Telephone },
                { "tel2", dto.AlternativeTelephone ?? "" },
                { "tel3", "" },

                { "email", dto.Email },

                { "address1", dto.AddressLine1 },
                { "address2", dto.AddressLine2 },
                { "address3", "" },
                { "town", dto.Town },
                { "county", dto.County },
                { "country", "United Kingdom" },
                { "postcode", dto.Postcode },

                { "cdh_id_option", dto.HasCdhId ? "yes" : "no" },
                { "cdh_id_lookup", dto.CdhId ?? "" },

                { "membercategory", dto.MembershipCategory },
                { "paymenttype", dto.PaymentType },

                { "memberstatus", dto.MemberStatus },
                { "new_memberid", "0" },
                { "new_cardswipe", "" },

                { "joindate", DateTime.UtcNow.ToString("dd/MM/yyyy") },
                { "applicationdate", dto.ApplicationDate.ToString("dd/MM/yyyy") },

                { "till_discount_group_id", "0" }
            };
        }
    }
}
