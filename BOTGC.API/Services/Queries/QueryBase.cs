namespace BOTGC.API.Services.Queries;

public abstract record QueryBase<TResponse> : MediatR.IRequest<TResponse>
{
    public CacheOptions Cache { get; init; } = new(false, true);
}

public readonly record struct CacheOptions(
    bool NoCache = false,
    bool WriteThrough = true);

