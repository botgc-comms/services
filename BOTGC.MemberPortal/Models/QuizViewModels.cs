// Models/QuizViewModels.cs
using System;
using System.Collections.Generic;
using BOTGC.MemberPortal.Services;

namespace BOTGC.MemberPortal.Models;

public sealed class QuizGatekeeperViewModel
{
    public string DisplayName { get; set; } = string.Empty;

    public bool HasInProgress { get; set; }
    public string? AttemptId { get; set; }

    public int TotalQuestions { get; set; }
    public int CorrectCount { get; set; }
    public int PassMark { get; set; }
    public int? NextQuestionNumber { get; set; }

    public List<QuizAttemptListItemViewModel> Completed { get; set; } = new List<QuizAttemptListItemViewModel>();
}

public sealed class QuizAttemptListItemViewModel
{
    public string AttemptId { get; set; } = string.Empty;
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset FinishedAtUtc { get; set; }
    public int TotalQuestions { get; set; }
    public int CorrectCount { get; set; }
    public int PassMark { get; set; }

    public bool Passed => CorrectCount >= PassMark;
}

public sealed class QuizCompletedListViewModel
{
    public string DisplayName { get; set; } = string.Empty;
    public List<QuizAttemptListItemViewModel> Attempts { get; set; } = new List<QuizAttemptListItemViewModel>();
}

public sealed class QuizAttemptSummaryViewModel
{
    public string AttemptId { get; set; } = string.Empty;
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset? FinishedAtUtc { get; set; }
    public int TotalQuestions { get; set; }
    public int CorrectCount { get; set; }
    public int PassMark { get; set; }
    public bool Passed { get; set; }

    public List<QuizAnswerSummaryViewModel> Answers { get; set; } = new List<QuizAnswerSummaryViewModel>();
}

public sealed class QuizAnswerSummaryViewModel
{
    public string QuestionId { get; set; } = string.Empty;
    public string SelectedAnswerId { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public DateTimeOffset AnsweredAtUtc { get; set; }
}

public sealed class QuizQuestionViewModel
{
    public string AttemptId { get; set; } = string.Empty;
    public string QuestionId { get; set; } = string.Empty;

    public int QuestionNumber { get; set; }
    public int TotalQuestions { get; set; }

    public string QuestionText { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }
    public string? ImageAlt { get; set; }

    public List<QuizChoiceViewModel> Choices { get; set; } = new List<QuizChoiceViewModel>();
}

public sealed class QuizChoiceViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public sealed class QuizStartForm
{
    public int QuestionCount { get; set; } = 10;
    public int PassMark { get; set; } = 7;
    public List<QuizDifficulty>? AllowedDifficulties { get; set; }
}

public sealed class QuizAnswerForm
{
    public string QuestionId { get; set; } = string.Empty;
    public string SelectedAnswerId { get; set; } = string.Empty;
}
