
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

        public static string ResolveCategoryGroup(string? categoryCode)
        {
            if (string.IsNullOrWhiteSpace(categoryCode)) return "Unknown";

            var code = categoryCode.Trim().ToUpperInvariant();

            if (code.StartsWith("7")) return "Full Membership";
            if (code.StartsWith("6")) return "6 Day Membership";
            if (code.StartsWith("5")) return "5 Day Membership";
            if (code.Contains("INTERMEDIATE")) return "Affordable Membership";
            if (code.Contains("STUDENT")) return "Student Membership";
            if (code.Contains("FLEXI")) return "Off Peak Playing Membership";
            if (code.Contains("JUNIOR")) return "Junior Membership";
            if (code.Contains("SOCIAL")) return "Social Membership";
            if (code.Contains("HOUSE")) return "Clubhouse Only";
            if (code.Contains("FAMILY")) return "Family";
            if (code.Contains("CORP")) return "Corporate";

            return "Other";
        }

        public static MembershipPrimaryCategories GetPrimaryCategory(MemberDto member, DateTime? date = null)
        {
            if (member == null) return MembershipPrimaryCategories.None;
            return GetPrimaryCategory(member!.MembershipStatus ?? "", member!.MembershipCategory ?? "", member.LeaveDate, member.JoinDate, date);
        }

        public static MembershipPrimaryCategories GetPrimaryCategory(string membershipStatus, string membershipCategory, DateTime? leftMembershipOn, DateTime? joinedOn, DateTime? date = null)
        {
            var checkDate = date ?? DateTime.UtcNow.Date;

            // Suspended members are excluded from all categories
            if (membershipStatus.Equals("S", StringComparison.OrdinalIgnoreCase))
                return MembershipPrimaryCategories.None;

            // Determine if LeaveDate should be considered
            var leaveDate = (leftMembershipOn.HasValue && leftMembershipOn.Value >= joinedOn)
                ? leftMembershipOn.Value
                : DateTime.MaxValue;

            // Determine if the member is active based on the refined LeaveDate logic
            bool isActive = membershipStatus.Equals("R", StringComparison.OrdinalIgnoreCase) &&
                            joinedOn <= checkDate &&
                            (leaveDate == DateTime.MinValue || leaveDate > checkDate || leaveDate < joinedOn);

            if (!isActive) return MembershipPrimaryCategories.None;

            // Determine if the member is playing or non-playing
            bool isPlaying = _playingMembersExpression!.IsMatch(membershipCategory);
            bool isNonPlaying = _nonPlayingMembersExpression!.IsMatch(membershipCategory);

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
            member.MembershipCategoryGroup = ResolveCategoryGroup(member.MembershipCategory);

            return member;
        }
    }
}
