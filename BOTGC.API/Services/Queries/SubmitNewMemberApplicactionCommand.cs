using BOTGC.API.Dto;

namespace BOTGC.API.Services.Queries
{
    public record SubmitNewMemberApplicactionCommand : QueryBase<NewMemberApplicationResultDto?>
    {
        public required NewMemberApplicationDto Application { get; init; }
    }
}
