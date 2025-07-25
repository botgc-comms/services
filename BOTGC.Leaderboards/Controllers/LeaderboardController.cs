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
        var vm = await _leaderboardService.GetLeaderboardPlayersAsync(competitionId);

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> GetLeaderboardData(int competitionId)
    {
        var vm = await _leaderboardService.GetLeaderboardPlayersAsync(competitionId);
        return Json(vm.Players);
    }

}
