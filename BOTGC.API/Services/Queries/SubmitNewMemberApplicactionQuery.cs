using BOTGC.API.Dto;
using MediatR;

namespace BOTGC.API.Services.Queries
{
    public record SubmitNewMemberApplicactionQuery : QueryBase<NewMemberApplicationResultDto?>
    {
        public required NewMemberApplicationDto Application { get; init; }
    }
}
