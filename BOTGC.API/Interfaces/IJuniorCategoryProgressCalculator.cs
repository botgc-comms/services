using BOTGC.API.Models;

namespace BOTGC.API.Interfaces;

public interface IJuniorCategoryProgressCalculator
{
    Task<JuniorCategoryProgressResult> CalculateAsync(int memberId, CancellationToken cancellationToken);
}