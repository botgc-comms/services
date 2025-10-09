using BOTGC.POS.Models;
using BOTGC.POS.Services;
using Microsoft.AspNetCore.Mvc;

namespace BOTGC.POS.Controllers;

[Route("stocktake")]
public sealed class StockTakeController : Controller
{
    private const string OperatorCookie = "wastage_operator_id";

    private readonly IOperatorService _operators;
    private readonly IStockTakeService _stockTakes;

    public StockTakeController(IOperatorService operators, IStockTakeService stockTakes)
    {
        _operators = operators ?? throw new ArgumentNullException(nameof(operators));
        _stockTakes = stockTakes ?? throw new ArgumentNullException(nameof(stockTakes));
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var ops = await _operators.GetAllAsync();
        var plan = await _stockTakes.GetPlannedAsync();
        var isDue = plan.Any(d => d.Products?.Count > 0);

        var vm = new StockTakeViewModel
        {
            Operators = ops,
            IsDue = isDue
        };

        return View(vm);
    }

    [HttpGet("products")]
    public async Task<IActionResult> Products()
    {
        var plan = await _stockTakes.GetPlannedAsync();
        return Ok(plan);
    }

    [HttpPost("select-operator")]
    public async Task<IActionResult> SelectOperator([FromForm] Guid id)
    {
        var op = await _operators.GetAsync(id);
        if (op is null) return BadRequest();

        Response.Cookies.Append(OperatorCookie, op.Id.ToString(), new CookieOptions
        {
            HttpOnly = false,
            IsEssential = true,
            Expires = DateTimeOffset.UtcNow.AddMinutes(10)
        });

        return Ok(new { ok = true, id = op.Id, name = op.DisplayName });
    }

    [HttpPost("commit")]
    public async Task<IActionResult> Commit([FromBody] StockTakeCommitDto payload)
    {
        if (!Request.Cookies.TryGetValue(OperatorCookie, out var opCookie) || !Guid.TryParse(opCookie, out var operatorId))
            return Unauthorized();

        var ok = await _stockTakes.CommitAsync(operatorId, payload.Timestamp, payload.Observations);
        if (!ok) return StatusCode(502);

        return Ok(new { ok = true });
    }
}
