using BOTGC.API.Services.Events;

namespace BOTGC.API.Interfaces;

public interface IJuniorProgressEvaluator
{
    Task EvaluateAsync(int memberId, CancellationToken cancellationToken);
}

public interface IJuniorProgressCategoryEvaluator
{
    bool CanEvaluate(string? membershipCategory);

    Task EvaluateAsync(
        int memberId,
        string category,
        MemberCategoryWindow window,
        CancellationToken cancellationToken);
}