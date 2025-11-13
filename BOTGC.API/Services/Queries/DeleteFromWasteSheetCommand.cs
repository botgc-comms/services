using BOTGC.API.Dto;

namespace BOTGC.API.Services.Queries;

public record DeleteFromWasteSheetCommand(DateTime Date, Guid EntryId) : QueryBase<DeleteResultDto>;

