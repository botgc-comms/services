namespace BOTGC.API.Dto
{
    public class MemberBillingCheckDto
    {
        public int MemberId { get; set; }
        public string MemberName { get; set; }
        public decimal Expected { get; set; }
        public decimal Billed { get; set; }
        public decimal Delta { get; set; }

        public MemberBillingCheckDto(int memberId, string memberName, decimal expected, decimal billed, decimal delta)
        {
            MemberId = memberId;
            MemberName = memberName;
            Expected = expected;
            Billed = billed;
            Delta = delta;
        }
    }
    public class MemberBillingAuditDto
    {
        public int MemberId { get; set; }
        public decimal ExpectedFY { get; set; }  
        public decimal BilledFY { get; set; }    
        public decimal Delta { get; set; }      

        public MemberBillingAuditDto() { }

        public MemberBillingAuditDto(int memberId, decimal expectedFY, decimal billedFY)
        {
            MemberId = memberId;
            ExpectedFY = expectedFY;
            BilledFY = billedFY;
            Delta = billedFY - expectedFY;
        }
    }
}

