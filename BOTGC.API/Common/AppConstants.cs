namespace BOTGC.API.Common
{
    public static class AppConstants
    {
        public const string MembershipApplicationQueueName = "new-membership-applications";
        public const string NewMemberAddedQueueName = "member-application-added";
        public const string MemberPropertyUpdateQueueName = "update-member-record";
        public const string StockWastageQueueName = "stock-wastage";
        public const string StockTakeProcessQueueName = "stock-take-process";
        public const string StockTakeCompletedQueueName = "stock-takes-completed";

        public const int QueueVisibilityTimeoutMinutes = 3;
        public const int QueueLockExpiryBufferSeconds = 30;
    }
}
