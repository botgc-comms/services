using BOTGC.API.Dto;

namespace BOTGC.API.Interfaces
{
    public interface ITaskBoardService
    {
        Task<string> AttachFile(string itemId, byte[] fileBytes, string fileName);
        Task<string> CreateMemberApplicationAsync(NewMemberApplicationDto dto);
    }
}