using BOTGC.MembershipApplication.Models;

namespace BOTGC.MembershipApplication.Interfaces
{
    public interface IMembershipCategoryCache
    {
        Task<IReadOnlyList<MembershipCategoryGroup>> GetAll();
        void Update(IEnumerable<MembershipCategoryGroup> categories);
        Task RefreshAsync(CancellationToken cancellationToken = default);
    }

}
