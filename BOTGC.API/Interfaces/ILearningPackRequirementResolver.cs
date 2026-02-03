namespace BOTGC.API.Interfaces;

public interface ILearningPackRequirementResolver
{
    Task<IReadOnlyList<string>> GetMandatoryPackIdsForChildCategoryAsync(string childCategory, CancellationToken cancellationToken);
}