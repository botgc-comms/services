
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

        public static void SetPrimaryCategory(MemberDto member, DateTime? date = null)
        {
            if (member == null) return;
            member.PrimaryCategory = GetPrimaryCategory(member, date);
        }
    }
}
