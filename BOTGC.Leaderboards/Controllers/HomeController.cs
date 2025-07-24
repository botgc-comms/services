using BOTGC.Leaderboards.Interfaces;
using BOTGC.Leaderboards.Models;
using BOTGC.Leaderboards.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

public class HomeController : Controller
{
    private readonly ILeaderboardService _leaderboardService;
    private readonly IHubContext<CompetitionHub> _hubContext;

    public HomeController(ILeaderboardService leaderboardService, IHubContext<CompetitionHub> hubContext)
    {
        _leaderboardService = leaderboardService;
        _hubContext = hubContext;
    }

    private async Task<List<CompetitionDetailsViewModel>> GetCompetitions(DateTime? date = null)
    {
        var competitions = await _leaderboardService.GetCompetitionsAsync();
        var fromDate = (date ?? DateTime.Now.Date).AddDays(-2);
        var selectedCompetitions = competitions.Where(c => c.Date >= fromDate).OrderBy(c => c.Date).Take(6).ToList();

        return selectedCompetitions;
    }

    [HttpGet]
    public async Task<IActionResult> Index(DateTime? date = null)
    {
        var upcomingCompetitions = await GetCompetitions(date);

        if (upcomingCompetitions.Count == 1 && upcomingCompetitions[0].Date.Date == DateTime.Today)
        {
            return Redirect(upcomingCompetitions[0].LeaderboardUrl);
        }
        else
        {
            return RedirectToAction("CompetitionSelection", new { date });
        }
    }

    public async Task<IActionResult> CompetitionSelection(DateTime? date = null)
    {
        var upcomingCompetitions = await GetCompetitions(date);
        return View("CompetitionSelection", upcomingCompetitions);
    }

    public async Task<IActionResult> CompetitionSelected(string sessionId, int competitionId)
    {
        await _hubContext.Clients.Group(sessionId).SendAsync("CompetitionSelected", competitionId);
        return RedirectToAction("Index", "Leaderboard", new { competitionId = competitionId });
    }
}
