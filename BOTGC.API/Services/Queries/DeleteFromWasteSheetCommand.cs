using BOTGC.API.Models;
using MediatR;

namespace BOTGC.API.Services.Queries
{
    public record DeleteFromWasteSheetCommand(DateTime Date, Guid EntryId) : QueryBase<DeleteResultDto>;

}
