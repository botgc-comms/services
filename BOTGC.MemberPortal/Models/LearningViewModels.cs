// Models/LearningViewModels.cs
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Html;

namespace BOTGC.MemberPortal.Models;

public sealed class LearningIndexViewModel
{
    public string DisplayName { get; set; } = string.Empty;

    public List<LearningPackListItemViewModel> Packs { get; set; } = new();
}

public sealed class LearningPackListItemViewModel
{
    public string PackId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public int EstimatedMinutes { get; set; }

    public bool IsMandatory { get; set; }

    public bool IsVisited { get; set; }

    public bool IsCompleted { get; set; }

    public DateTimeOffset? LastViewedAtUtc { get; set; }

    public DateTimeOffset? CompletedAtUtc { get; set; }
}

public sealed class LearningPackViewModel
{
    public string PackId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public int EstimatedMinutes { get; set; }

    public bool IsMandatory { get; set; }

    public bool IsCompleted { get; set; }

    public DateTimeOffset? CompletedAtUtc { get; set; }

    public string? ContinueHref { get; set; }

    public List<LearningPackPageListItemViewModel> Pages { get; set; } = new();
}

public sealed class LearningPackPageListItemViewModel
{
    public string PageId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;

    public int Index { get; set; }

    public bool IsRead { get; set; }

    public bool IsCurrent { get; set; }
}

public sealed class LearningPackPageViewModel
{
    public string PackId { get; set; } = string.Empty;

    public string PackTitle { get; set; } = string.Empty;

    public string PageId { get; set; } = string.Empty;

    public string PageTitle { get; set; } = string.Empty;

    public int PageNumber { get; set; }

    public int TotalPages { get; set; }

    public IHtmlContent Html { get; set; } = HtmlString.Empty;

    public string? PreviousPageId { get; set; }

    public string? NextPageId { get; set; }

    public bool IsCompleted { get; set; }
}
