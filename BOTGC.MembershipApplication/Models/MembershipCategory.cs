namespace BOTGC.MembershipApplication.Models
{
    public class MembershipCategory
    {
        public string Name { get; set; } 
        public string Title { get; set; }
        public string Description { get; set; }
        public bool FinanceAvailable { get; set; }
        public bool IsOnWaitingList { get; set; }
        public bool Display { get; set; }
        public bool ReferrerEligable { get; set; }
    }

    public class MembershipCategoryGroup
    {
        public string Name { get; set; } = "";
        public int Order { get; set; }
        public List<MembershipCategory> Categories { get; set; } = new();
    }
}
