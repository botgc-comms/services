using BOTGC.Leaderboards.Interfaces;
using Microsoft.AspNetCore.Mvc;

public class LeaderboardController : Controller
{
    private readonly ILeaderboardService _leaderboardService;

    public LeaderboardController(ILeaderboardService leaderboardService)
    {
        _leaderboardService = leaderboardService;
    }

    public async Task<IActionResult> Index(int competitionId)
    {
        var vm = await _leaderboardService.GetPlayersAsync(competitionId);

        return View(vm);
    }
}
