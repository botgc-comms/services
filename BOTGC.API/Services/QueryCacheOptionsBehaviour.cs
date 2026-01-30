using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using MediatR;

namespace BOTGC.API.Services;

public sealed class QueryCacheOptionsBehaviour<TRequest, TResponse>(
    IQueryCacheOptionsAccessor accessor)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IQueryCacheOptionsAccessor _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is QueryBase<TResponse> qb)
        {
            var prior = _accessor.Current;
            _accessor.Current = qb.Cache;

            try
            {
                return await next();
            }
            finally
            {
                _accessor.Current = prior;
            }
        }

        return await next();
    }
}