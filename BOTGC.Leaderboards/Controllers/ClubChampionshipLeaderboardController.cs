using BOTGC.Leaderboards.Interfaces;
using Microsoft.AspNetCore.Mvc;

public class ClubChampionshipLeaderboardController : Controller
{
    private readonly ILeaderboardService _leaderboardService;

    public ClubChampionshipLeaderboardController(ILeaderboardService leaderboardService)
    {
        _leaderboardService = leaderboardService;
    }

    public async Task<IActionResult> Index(int competitionId)
    {
        var vm = await _leaderboardService.GetClubChampionshipLeaderboardPlayersAsync(competitionId);

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> GetLeaderboardData(int competitionId)
    {
        var vm = await _leaderboardService.GetClubChampionshipLeaderboardPlayersAsync(competitionId);
        return Json(vm.Players);
    }
}
