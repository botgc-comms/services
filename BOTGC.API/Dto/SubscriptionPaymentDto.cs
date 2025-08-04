namespace BOTGC.API.Dto
{
    /// <summary>
    /// Represents a summary of a golf round, including competition details, score, and metadata.
    /// </summary>
    public class SubscriptionPaymentDto : HateoasResource
    {
        /// <summary>
        /// The members id
        /// </summary>
        public int MemberId { get; set; }

        /// <summary>
        /// The date on which the subscription payment was due
        /// </summary>
        public DateTime DateDue { get; set; }

        /// <summary>
        /// The date on which the subscription payment was made
        /// </summary>
        public decimal BillAmount { get; set; }

        /// <summary>
        /// The amount that has been paid towards the subscription.
        /// </summary>
        public decimal? AmountPaid { get; set; }

        /// <summary>
        /// The date on which the payment was made.
        /// </summary>
        public DateTime? PaymentDate { get; set; }

        /// <summary>
        /// The method of payment used for the subscription.
        /// </summary>
        public string MembershipCategory { get; set; } = string.Empty;
    }
}
