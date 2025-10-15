using BOTGC.API.Dto;
using MediatR;

namespace BOTGC.API.Services.Queries
{
    public record DeleteFromWasteSheetCommand(DateTime Date, Guid EntryId) : QueryBase<DeleteResultDto>;
}
