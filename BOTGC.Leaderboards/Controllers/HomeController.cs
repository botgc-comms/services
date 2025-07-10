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

    private async Task<List<CompetitionDetailsViewModel>> GetCompetitions()
    {
        var competitions = await _leaderboardService.GetCompetitionsAsync();
        var fromDate = DateTime.Now.Date;
        var toDate = fromDate.AddDays(5);
        return competitions.Where(c => c.Date >= fromDate && c.Date <= toDate).ToList();
    }

    public async Task<IActionResult> Index()
    {
        var upcomingCompetitions = await GetCompetitions();

        if (upcomingCompetitions.Count > 1)
        {
            return RedirectToAction("CompetitionSelection");
        }
        else if (upcomingCompetitions.Count == 1)
        {
            return RedirectToAction("Index", "Leaderboard", new { competitionId = upcomingCompetitions[0].Id });
        }
        else
        {
            ViewBag.ErrorMessage = "No upcoming competitions found.";
            return View();
        }
    }

    public async Task<IActionResult> CompetitionSelection()
    {
        var upcomingCompetitions = await GetCompetitions();
        return View("CompetitionSelection", upcomingCompetitions);
    }

    public async Task<IActionResult> CompetitionSelected(string sessionId, int competitionId)
    {
        await _hubContext.Clients.Group(sessionId).SendAsync("CompetitionSelected", competitionId);
        return RedirectToAction("Index", "Leaderboard", new { competitionId = competitionId });
    }
}
