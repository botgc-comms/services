namespace BOTGC.API.Models;

public sealed record MandatoryLearningPacksForChildDto(
    int ChildMemberID,
    string ChildCategory,
    IReadOnlyList<string> MandatoryPackIds
);