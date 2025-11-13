using BOTGC.API.Dto;
using BOTGC.API.Models;
using BOTGC.API.Services.Queries;

namespace BOTGC.API.Interfaces
{
    public interface ITaskBoardService
    {
        Task<string> AttachMemberApplicationForm(string itemId, byte[] fileBytes, string fileName);
        Task<string> AttachFinanceInvoiceFileAsync(string itemId, byte[] fileBytes, string fileName);
        Task<string> CreateMemberApplicationAsync(NewMemberApplicationResultDto dto);
        Task<string?> FindExistingApplicationItemIdAsync(string applicationId);
        Task<List<MembershipCategoryGroupDto>> GetMembershipCategories();
        Task<StockBoardSyncResult> SyncStockLevelsAsync(List<StockItemDto> stockItems);
        Task<string> CreateStockTakeAndInvestigationsAsync(StockTakeCompletedCommand msg, string? igLink = null);
        Task<string> CreateFinanceTaskAsync(string groupTitle, string taskName, string? assigneeEmail = null, string? statusLabel = "To do", DateTime? deadline = null);
    }
}