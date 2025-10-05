using BOTGC.POS.Hubs;
using BOTGC.POS.Models;
using BOTGC.POS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace BOTGC.POS.Controllers;

public class WastageController : Controller
{
    private const string OperatorCookie = "wastage_operator_id";

    private readonly IOperatorService _operators;
    private readonly IProductService _products;
    private readonly IReasonService _reasons;
    private readonly IWasteService _waste;
    private readonly IHubContext<WastageHub> _hub;

    public WastageController(
        IOperatorService operators,
        IProductService products,
        IReasonService reasons,
        IWasteService waste,
        IHubContext<WastageHub> hub)
    {
        _operators = operators;
        _products = products;
        _reasons = reasons;
        _waste = waste;
        _hub = hub;
    }

    public async Task<IActionResult> Index()
    {
        var model = new WastageViewModel
        {
            Sheet = await _waste.GetTodayAsync(),
            TopProducts = await _products.GetTop20Async(),
            Reasons = await _reasons.GetAllAsync(),
            Operators = await _operators.GetAllAsync()
        };
        return View(model);
    }

    [HttpGet("/wastage/sheet")]
    public async Task<IActionResult> Sheet()
    {
        var sheet = await _waste.GetTodayAsync();
        return Ok(sheet);
    }

    [HttpGet("/wastage/search")]
    public async Task<IActionResult> Search(string q)
    {
        if (string.IsNullOrWhiteSpace(q)) return Ok(Array.Empty<object>());
        var results = await _products.SearchAsync(q);
        return Ok(results.Select(p => new { p.Id, p.Name, p.Category }));
    }

    [HttpPost("/wastage/select-operator")]
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

    [HttpPost("/wastage/log")]
    public async Task<IActionResult> Log(
        [FromForm] Guid productId,
        [FromForm] long igProductId,
        [FromForm] string unit, 
        [FromForm] string productName,
        [FromForm] Guid? reasonId,
        [FromForm] string? customReason,
        [FromForm] decimal quantity,
        [FromForm] string? clientId)
    {
        if (!Request.Cookies.TryGetValue(OperatorCookie, out var opCookie) || !Guid.TryParse(opCookie, out var operatorId))
            return Unauthorized();

        var reasonText = customReason?.Trim();
        if (reasonId.HasValue)
        {
            var r = await _reasons.GetAsync(reasonId.Value);
            if (r != null) reasonText = r.Name;
        }

        var id = Guid.TryParse(clientId, out var parsed) ? parsed : Guid.NewGuid();

        var entry = new WasteEntry(
            id,
            DateTimeOffset.UtcNow,
            operatorId,
            productId,
            igProductId, 
            unit, 
            productName,
            reasonText ?? "Unspecified",
            quantity
        );

        await _waste.AddAsync(entry);

        // Broadcast to all connected clients
        await _hub.Clients.All.SendAsync("EntryAdded", new
        {
            id = entry.Id,
            atIso = entry.At.ToString("o"),
            operatorId = entry.OperatorId,
            productId = entry.ProductId,
            igProductId = entry.IGProductId, 
            unit = entry.Unit, 
            productName = entry.ProductName,
            reason = entry.Reason,
            quantity = entry.Quantity
        });

        return Ok(new { ok = true });
    }

    [HttpDelete("/wastage/entry/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _waste.DeleteAsync(id);

        if (!result)
        {
            return NotFound(new { ok = false, message = "Entry not found" });
        }

        await _hub.Clients.All.SendAsync("EntryDeleted", new { id });

        return Ok(new { ok = true });
    }


    [HttpPost("/wastage/submit")]
    public async Task<IActionResult> Submit()
    {
        await _waste.SubmitTodayAsync();
        Response.Cookies.Delete(OperatorCookie);
        return RedirectToAction(nameof(Index));
    }
}
