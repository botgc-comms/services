using BOTGC.API.Interfaces;
using BOTGC.API.Models;

public sealed class TableStorageLearningPackRequirementResolver(
    ITableStore<LearningPackCatalogueEntity> catalogueTable)
    : ILearningPackRequirementResolver
{
    private readonly ITableStore<LearningPackCatalogueEntity> _catalogueTable = catalogueTable ?? throw new ArgumentNullException(nameof(catalogueTable));

    public async Task<IReadOnlyList<string>> GetMandatoryPackIdsForChildCategoryAsync(string childCategory, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(childCategory))
        {
            return Array.Empty<string>();
        }

        var category = childCategory.Trim();

        var rows = await _catalogueTable.QueryAsync(
            predicate: e => e.PartitionKey == "PACKS",
            take: 2000,
            cancellationToken: cancellationToken);

        var result = new List<string>(64);

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.PackId))
            {
                continue;
            }

            if (!IsMandatoryFor(row.MandatoryForCsv, category))
            {
                continue;
            }

            result.Add(row.PackId.Trim());
        }

        return result
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool IsMandatoryFor(string? mandatoryForCsv, string category)
    {
        if (string.IsNullOrWhiteSpace(mandatoryForCsv))
        {
            return false;
        }

        foreach (var part in mandatoryForCsv.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (string.Equals(part, category, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}