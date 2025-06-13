using Microsoft.Extensions.ObjectPool;
using BOTGC.API.Dto;

namespace BOTGC.API.Dto
{
    public class MembershipCategoryDto: HateoasResource
    {
        public string Name { get; set; } 
        public string Title { get; set; }
        public string Description { get; set; }
        public string Price { get; set; }
        public bool FinanceAvailable { get; set; }
        public bool IsOnWaitingList { get; set; }
        public bool Display { get; set; }
        public bool ReferrerEligable { get; set; }
    }
}
