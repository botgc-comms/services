using BOTGC.MemberPortal.Models;

namespace BOTGC.MemberPortal.Interfaces;

public interface IWhatsNextProvider
{
    Task<IReadOnlyList<WhatsNextItemViewModel>> GetItemsAsync(MemberContext member, CancellationToken cancellationToken);
}
