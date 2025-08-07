namespace BOTGC.API.Services.Queries
{
    public abstract record QueryBase<TResponse> : MediatR.IRequest<TResponse>;
}
