using BOTGC.API.Services.Queries;

namespace BOTGC.API.Services.QueryHandlers
{
    public abstract class QueryHandlerBase<TRequest, TResponse> : MediatR.IRequestHandler<TRequest, TResponse>
        where TRequest : QueryBase<TResponse>
    {
        public abstract Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
    }
}
