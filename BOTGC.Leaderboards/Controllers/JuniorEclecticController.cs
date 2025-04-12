using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using BOTGC.Leaderboards.Models;
using BOTGC.Leaderboards.Models.EclecticScorecard;
using BOTGC.Leaderboards.Interfaces;

namespace BOTGC.Leaderboards.Controllers
{
    [Route("[controller]")]
    public class JuniorEclecticController : Controller
    {
        private readonly IJuniorEclecticService _eclecticService;

        public JuniorEclecticController(IJuniorEclecticService eclecticService)
        {
            _eclecticService = eclecticService;
        }

        public async Task<IActionResult> Index()
        {
            var players = await _eclecticService.GetPlayersAsync();

            if (players == null || !players.Any())
            {
                ViewBag.ErrorMessage = "No results available or failed to load data.";
                return View("Error");
            }

            return View(players);
        }
    }

}
