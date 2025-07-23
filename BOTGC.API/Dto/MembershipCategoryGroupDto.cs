using Microsoft.Extensions.ObjectPool;
using BOTGC.API.Dto;

namespace BOTGC.API.Dto
{
    public class MembershipCategoryGroupDto
    {
        public string Name { get; set; } = "";
        public int Order { get; set; }
        public List<MembershipCategoryDto> Categories { get; set; } = new();
    }

}
