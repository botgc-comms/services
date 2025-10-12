using BOTGC.POS.Hubs;
using BOTGC.POS.Models;
using BOTGC.POS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace BOTGC.POS.Controllers;

[Route("stocktake")]
public sealed class StockTakeController : Controller
{
    private const string OperatorCookie = "wastage_operator_id";

    private readonly AppSettings _settings;
    private readonly IOperatorService _operators;
    private readonly IStockTakeService _stockTakes;
    private readonly IHubContext<StockTakeHub> _hub;

    public StockTakeController(
        IOperatorService operators,
        IStockTakeService stockTakes,
        IHubContext<StockTakeHub> hub,
        IOptions<AppSettings> opts
        )
    {
        _operators = operators;
        _stockTakes = stockTakes;
        _hub = hub;
        _settings = opts?.Value ?? throw new ArgumentNullException(nameof(opts));
    }

    [HttpGet("config")]
    public IActionResult GetConfig()
       => Ok(new
       {
           showEstimatedInDialog = _settings.StockTake.ShowEstimatedInDialog
       });

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var vm = new StockTakeViewModel
        {
            Operators = await _operators.GetAllAsync(),
            IsDue = (await _stockTakes.GetPlannedAsync()).Any(d => d.Products?.Count > 0)
        };
        return View(vm);
    }

    [HttpGet("products")]
    public async Task<IActionResult> Products()
        => Ok(await _stockTakes.GetPlannedAsync());

    [HttpGet("draft")]
    public async Task<IActionResult> Draft([FromQuery] string division)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var sheet = await _stockTakes.GetSheetAsync(today, division);
        return Ok(sheet);
    }

    [HttpPost("observe")]
    public async Task<IActionResult> Observe([FromBody] StockTakeDraftEntry entry)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        // Attach operator from cookie (UI concern)
        if (!Request.Cookies.TryGetValue(OperatorCookie, out var opCookie) || !Guid.TryParse(opCookie, out var opId))
            return Unauthorized();

        entry.OperatorId = opId;
        if (string.IsNullOrWhiteSpace(entry.OperatorName))
        {
            var op = await _operators.GetAsync(opId);
            entry.OperatorName = op?.DisplayName ?? "Unknown";
        }
        if (entry.At == default) entry.At = DateTimeOffset.UtcNow;

        await _stockTakes.UpsertEntryAsync(today, entry);

        // Broadcast row so other devices update
        await _hub.Clients.All.SendAsync("ObservationUpserted", entry);

        return Ok(new { ok = true });
    }

    [HttpDelete("observe/{stockItemId:int}")]
    public async Task<IActionResult> Remove([FromRoute] int stockItemId, [FromQuery] string division)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        await _stockTakes.RemoveEntryAsync(today, division, stockItemId);

        await _hub.Clients.All.SendAsync("ObservationRemoved", new { stockItemId, division });

        return Ok(new { ok = true });
    }

    // Operator selection (UI-only; cookie lifespan 15 minutes)
    [HttpPost("select-operator")]
    public async Task<IActionResult> SelectOperator([FromForm] Guid id)
    {
        var op = await _operators.GetAsync(id);
        if (op is null) return BadRequest();

        Response.Cookies.Append(OperatorCookie, op.Id.ToString(), new CookieOptions
        {
            HttpOnly = false,
            IsEssential = true,
            Expires = DateTimeOffset.UtcNow.AddMinutes(15)
        });

        await _hub.Clients.All.SendAsync("OperatorSelected", new
        {
            id = op.Id,
            name = op.DisplayName,
            colorHex = op.ColorHex
        });

        return Ok(new { ok = true, id = op.Id, name = op.DisplayName, colorHex = op.ColorHex });
    }
}
