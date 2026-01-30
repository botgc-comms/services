// Controllers/QuizController.cs
using BOTGC.MemberPortal.Interfaces;
using BOTGC.MemberPortal.Models;
using BOTGC.MemberPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BOTGC.MemberPortal.Controllers;

[Authorize]
[Route("quiz")]
public sealed class QuizController : Controller
{
    private readonly ICurrentUserService _currentUserService;
    private readonly QuizService _quizService;

    public QuizController(
        ICurrentUserService currentUserService,
        QuizService quizService)
    {
        _currentUserService = currentUserService;
        _quizService = quizService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var userId = _currentUserService.UserId.Value.ToString();

        var inProgress = await _quizService.GetInProgressAttemptAsync(userId, cancellationToken);

        // Controllers/QuizController.cs (inside Index)
        if (inProgress is not null)
        {
            var nextQuestion = await _quizService.GetNextQuestionAsync(userId, inProgress.AttemptId, cancellationToken);

            int? nextNumber = null;

            if (nextQuestion is not null)
            {
                nextNumber = GetNextQuestionNumber(inProgress, nextQuestion.Id);
            }

            var completedQuestions = Math.Max(0, (nextNumber ?? 1) - 1);

            var vm = new QuizGatekeeperViewModel
            {
                DisplayName = _currentUserService.DisplayName ?? "Junior member",
                HasInProgress = true,
                AttemptId = inProgress.AttemptId,
                TotalQuestions = inProgress.TotalQuestions,
                PassMark = inProgress.PassMark,
                NextQuestionNumber = nextNumber,
                CompletedQuestions = completedQuestions,
                Difficulty = inProgress.Difficulty,
                StartedLabel = $"Started {FormatRelative(inProgress.StartedAtUtc)}",
            };

            return View("Index", vm);
        }


        var completed = await _quizService.ListCompletedAttemptsAsync(userId, take: 20, cancellationToken);

        var listVm = new QuizGatekeeperViewModel
        {
            DisplayName = _currentUserService.DisplayName ?? "Junior member",
            HasInProgress = false,
            Completed = completed
                .Select(a => new QuizAttemptListItemViewModel
                {
                    AttemptId = a.AttemptId,
                    StartedAtUtc = a.StartedAtUtc,
                    FinishedAtUtc = a.FinishedAtUtc,
                    TotalQuestions = a.TotalQuestions,
                    CorrectCount = a.CorrectCount,
                    PassMark = a.PassMark,
                })
                .ToList(),
        };

        return View("Index", listVm);
    }

    [HttpPost("start")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start([FromForm] QuizStartForm form, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var userId = _currentUserService.UserId.Value.ToString();

        var request = new StartQuizRequest(
            UserId: userId,
            QuestionCount: form.QuestionCount <= 0 ? 10 : form.QuestionCount,
            Difficulty: form.Difficulty,
            PassMark: form.PassMark <= 0 ? 7 : form.PassMark,
            AllowedDifficulties: form.AllowedDifficulties?.Count > 0 ? form.AllowedDifficulties : null
        );

        var result = await _quizService.StartQuizAsync(request, cancellationToken);

        return RedirectToAction(nameof(Attempt), new { attemptId = result.Attempt.AttemptId });
    }

    [HttpGet("{attemptId}")]
    public async Task<IActionResult> Attempt([FromRoute] string attemptId, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var userId = _currentUserService.UserId.Value.ToString();

        var attempt = await _quizService.GetInProgressAttemptAsync(userId, cancellationToken);
        if (attempt is null || !string.Equals(attempt.AttemptId, attemptId, StringComparison.Ordinal))
        {
            return RedirectToAction(nameof(Index));
        }

        return RedirectToAction(nameof(Question), new { attemptId });
    }

    [HttpGet("{attemptId}/question")]
    public async Task<IActionResult> Question([FromRoute] string attemptId, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var userId = _currentUserService.UserId.Value.ToString();

        var details = await _quizService.GetAttemptDetailsAsync(userId, attemptId, cancellationToken);
        if (details is null)
        {
            return RedirectToAction(nameof(Index));
        }

        if (!string.Equals(details.Attempt.Status, "InProgress", StringComparison.Ordinal))
        {
            return RedirectToAction(nameof(CompletedAttempt), new { attemptId });
        }

        var next = await _quizService.GetNextQuestionAsync(userId, attemptId, cancellationToken);
        if (next is null)
        {
            return RedirectToAction(nameof(CompletedAttempt), new { attemptId });
        }

        var answered = new HashSet<string>(details.Answers.Select(a => a.QuestionId), StringComparer.Ordinal);
        var questionNumber = details.Attempt.QuestionIdsInOrder
            .Select((id, idx) => (id, idx))
            .Where(x => !answered.Contains(x.id))
            .Select(x => x.idx + 1)
            .DefaultIfEmpty(1)
            .First();

        var vm = new QuizQuestionViewModel
        {
            AttemptId = attemptId,
            QuestionId = next.Id,
            QuestionNumber = questionNumber,
            TotalQuestions = details.Attempt.TotalQuestions,
            QuestionText = next.Question,
            ImageUrl = next.ImageUrl,
            ImageAlt = next.ImageAlt,
            Choices = next.Choices.Select(c => new QuizChoiceViewModel { Id = c.Id, Text = c.Text }).ToList(),
        };

        return View("Question", vm);
    }

    [HttpPost("{attemptId}/answer")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Answer([FromRoute] string attemptId, [FromForm] QuizAnswerForm form, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var userId = _currentUserService.UserId.Value.ToString();

        if (string.IsNullOrWhiteSpace(form.QuestionId) || string.IsNullOrWhiteSpace(form.SelectedAnswerId))
        {
            return RedirectToAction(nameof(Question), new { attemptId });
        }

        var result = await _quizService.AnswerAsync(
            userId: userId,
            attemptId: attemptId,
            questionId: form.QuestionId,
            selectedAnswerId: form.SelectedAnswerId,
            ct: cancellationToken
        );

        if (result is null)
        {
            return RedirectToAction(nameof(Index));
        }

        if (result.IsFinished)
        {
            return RedirectToAction(nameof(CompletedAttempt), new { attemptId });
        }

        return RedirectToAction(nameof(Question), new { attemptId });
    }

    [HttpGet("completed")]
    public async Task<IActionResult> Completed(CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var userId = _currentUserService.UserId.Value.ToString();
        var completed = await _quizService.ListCompletedAttemptsAsync(userId, take: 50, cancellationToken);

        var vm = new QuizCompletedListViewModel
        {
            DisplayName = _currentUserService.DisplayName ?? "Junior member",
            Attempts = completed.Select(a => new QuizAttemptListItemViewModel
            {
                AttemptId = a.AttemptId,
                StartedAtUtc = a.StartedAtUtc,
                FinishedAtUtc = a.FinishedAtUtc,
                TotalQuestions = a.TotalQuestions,
                CorrectCount = a.CorrectCount,
                PassMark = a.PassMark,
            }).ToList(),
        };

        return View("Completed", vm);
    }

    [HttpGet("completed/{attemptId}")]
    public async Task<IActionResult> CompletedAttempt([FromRoute] string attemptId, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var userId = _currentUserService.UserId.Value.ToString();

        var details = await _quizService.GetAttemptDetailsAsync(userId, attemptId, cancellationToken);
        if (details is null)
        {
            return RedirectToAction(nameof(Index));
        }

        var snapshotAttempt = details.Attempt;
        var passed = snapshotAttempt.CorrectCount >= snapshotAttempt.PassMark;

        var vm = new QuizAttemptSummaryViewModel
        {
            AttemptId = snapshotAttempt.AttemptId,
            StartedAtUtc = snapshotAttempt.StartedAtUtc,
            FinishedAtUtc = snapshotAttempt.FinishedAtUtc,
            TotalQuestions = snapshotAttempt.TotalQuestions,
            CorrectCount = snapshotAttempt.CorrectCount,
            PassMark = snapshotAttempt.PassMark,
            Passed = passed,
            Answers = details.Answers
                .Select(a => new QuizAnswerSummaryViewModel
                {
                    QuestionId = a.QuestionId,
                    SelectedAnswerId = a.SelectedAnswerId,
                    IsCorrect = a.IsCorrect,
                    AnsweredAtUtc = a.AnsweredAtUtc,
                })
                .ToList(),
        };

        return View("Summary", vm);
    }

    private static int? GetNextQuestionNumber(QuizAttempt attempt, string nextQuestionId)
    {
        var idx = Array.FindIndex(attempt.QuestionIdsInOrder.ToArray(), x => string.Equals(x, nextQuestionId, StringComparison.Ordinal));
        if (idx < 0)
        {
            return null;
        }

        return idx + 1;
    }

    private static string FormatRelative(DateTimeOffset startedAtUtc)
    {
        var now = DateTimeOffset.UtcNow;
        var span = now - startedAtUtc;

        if (span.TotalDays < 1)
        {
            return "today";
        }

        if (span.TotalDays < 7)
        {
            var d = (int)Math.Floor(span.TotalDays);
            return d == 1 ? "1 day ago" : $"{d} days ago";
        }

        if (span.TotalDays < 30)
        {
            var w = (int)Math.Floor(span.TotalDays / 7);
            return w == 1 ? "1 week ago" : $"{w} weeks ago";
        }

        if (span.TotalDays < 365)
        {
            var m = (int)Math.Floor(span.TotalDays / 30);
            return m == 1 ? "1 month ago" : $"{m} months ago";
        }

        var y = (int)Math.Floor(span.TotalDays / 365);
        return y == 1 ? "1 year ago" : $"{y} years ago";
    }

}
