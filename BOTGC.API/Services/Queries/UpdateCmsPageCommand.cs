using MediatR;

namespace BOTGC.API.Services.Queries;

public sealed record UpdateCmsPageCommand(int PageId, string Html) : QueryBase<bool>;
