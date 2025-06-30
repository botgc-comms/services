
using BOTGC.API.Dto;
using System.Text.RegularExpressions;

namespace BOTGC.API.Common
{
    public static class MembershipHelper
    {
        private static Regex? _playingMembersExpression = null;
        private static Regex? _nonPlayingMembersExpression = null;

        public static void Configure(AppSettings settings)
        {
            _playingMembersExpression = new Regex(settings.PlayingMemberExpression, RegexOptions.Compiled);
            _nonPlayingMembersExpression = new Regex(settings.NonPlayingMemberExpression, RegexOptions.Compiled);
        }

        public static MembershipPrimaryCategories GetPrimaryCategory(MemberDto member, DateTime? date = null)
        {
            if (member == null) return MembershipPrimaryCategories.None;

            var checkDate = date ?? DateTime.UtcNow.Date;

            // Suspended members are excluded from all categories
            if (member.MembershipStatus.Equals("S", StringComparison.OrdinalIgnoreCase))
                return MembershipPrimaryCategories.None;

            // Determine if LeaveDate should be considered
            var leaveDate = (member.LeaveDate.HasValue && member.LeaveDate.Value >= member.JoinDate)
                ? member.LeaveDate.Value
                : DateTime.MaxValue;

            // Determine if the member is active based on the refined LeaveDate logic
            bool isActive = member.MembershipStatus.Equals("R", StringComparison.OrdinalIgnoreCase) &&
                            member.JoinDate <= checkDate &&
                            (leaveDate == DateTime.MinValue || leaveDate > checkDate || leaveDate < member.JoinDate);

            if (!isActive) return MembershipPrimaryCategories.None;

            // Determine if the member is playing or non-playing
            bool isPlaying = _playingMembersExpression!.IsMatch(member.MembershipCategory);
            bool isNonPlaying = _nonPlayingMembersExpression!.IsMatch(member.MembershipCategory);

            return isPlaying ? MembershipPrimaryCategories.PlayingMember : isNonPlaying ? MembershipPrimaryCategories.NonPlayingMember : MembershipPrimaryCategories.None;

        }

        public static MemberDto SetPrimaryCategory(this MemberDto member, DateTime? date = null)
        {
            if (member == null) return member;
            member.PrimaryCategory = GetPrimaryCategory(member, date);
            member.IsActive = member.PrimaryCategory != MembershipPrimaryCategories.None;

            return member;
        }

        public static MemberDto SetCategoryGroup(this MemberDto member)
        {
            var categoryCode = member.MembershipCategory;

            if (string.IsNullOrWhiteSpace(categoryCode))
                member.MembershipCategoryGroup = "Unknown";

            var code = categoryCode.Trim().ToUpperInvariant();

            if (code.StartsWith("7")) member.MembershipCategoryGroup = "Full Membership";
            if (code.StartsWith("6")) member.MembershipCategoryGroup = "6 Day Membership";
            if (code.StartsWith("5")) member.MembershipCategoryGroup = "5 Day Membership";
            if (code.Contains("INTERMEDIATE")) member.MembershipCategoryGroup = "Affordable Membership";
            if (code.Contains("STUDENT")) member.MembershipCategoryGroup = "Student Membership";
            if (code.Contains("FLEXI")) member.MembershipCategoryGroup = "Off Peak Playing Membership";
            if (code.Contains("JUNIOR")) member.MembershipCategoryGroup = "Junior Membership";
            if (code.Contains("SOCIAL")) member.MembershipCategoryGroup = "Social Membership";
            if (code.Contains("CLUBHOUSE")) member.MembershipCategoryGroup = "Clubhouse Only";
            if (code.Contains("FAMILY")) member.MembershipCategoryGroup = "Family";
            if (code.Contains("CORP")) member.MembershipCategoryGroup = "Corporate";

            return member;
        }
    }
}
