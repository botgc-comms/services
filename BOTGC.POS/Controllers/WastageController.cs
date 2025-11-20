using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using BOTGC.POS.Hubs;
using BOTGC.POS.Models;
using BOTGC.POS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace BOTGC.POS.Controllers;

public class WastageController(

    IOptions<AppSettings> settings,
    IStockTakeScheduleService stockTakeSchedule,
    IOperatorService operators,
    IProductService products,
    IReasonService reasons,
    IWasteService waste,
    IHubContext<WastageHub> hub) : Controller
{
    private const string OperatorCookie = "wastage_operator_id";

    private readonly IOperatorService _operators = operators;
    private readonly IProductService _products = products;
    private readonly IReasonService _reasons = reasons;
    private readonly IWasteService _waste = waste;
    private readonly IHubContext<WastageHub> _hub = hub;
    private readonly IStockTakeScheduleService _stockTakeSchedule = stockTakeSchedule;
    private readonly AppSettings _appSettings = settings.Value;

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
        return Ok(results.Select(p => new { p.Id, p.Name, p.Category, p.igProductId, p.unit }));
    }

    [HttpGet("/wastage/stocktake-status")]
    public IActionResult StockTakeStatus()
    {
        var due = _stockTakeSchedule.IsStockTakeDue(DateTimeOffset.UtcNow);
        var stockCfg = _appSettings.StockTake ?? new StockTakeUiSettings();
        var url = stockCfg.StockTakeUrl ?? string.Empty;

        return Ok(new
        {
            due = due && !string.IsNullOrWhiteSpace(url),
            url,
            chimeEnabled = stockCfg.EnableChime,
            chimeStart = stockCfg.ChimeStartTime,
            chimeEnd = stockCfg.ChimeEndTime,
            chimeIntervalMinutes = stockCfg.ChimeIntervalMinutes
        });
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

        if (quantity <= 0) return BadRequest(new { ok = false, message = "Quantity must be greater than zero." });

        var reasonText = await ResolveReasonAsync(reasonId, customReason);

        var id = Guid.TryParse(clientId, out var parsed) ? parsed : Guid.NewGuid();

        var entry = new WasteEntry(
            id,
            DateTimeOffset.UtcNow,
            operatorId,
            productId,
            igProductId,
            unit,
            productName,
            reasonText,
            quantity
        );

        await _waste.AddAsync(entry);

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
        if (!result) return NotFound(new { ok = false, message = "Entry not found" });

        await _hub.Clients.All.SendAsync("EntryDeleted", new { id });

        return Ok(new { ok = true });
    }

    [HttpGet("/wastage/product/{id:guid}")]
    public async Task<IActionResult> Product(Guid id)
    {
        var details = await _products.GetDetailsAsync(id);
        if (details is null) return NotFound();
        return Ok(details);
    }

    [HttpPost("/wastage/log-product")]
    public async Task<IActionResult> LogProduct(
        [FromForm] Guid productId,
        [FromForm] decimal quantity,
        [FromForm] Guid? reasonId,
        [FromForm] string? customReason,
        [FromForm] string? clientId)
    {
        if (!Request.Cookies.TryGetValue(OperatorCookie, out var opCookie) || !Guid.TryParse(opCookie, out var operatorId))
            return Unauthorized();

        if (quantity <= 0) return BadRequest(new { ok = false, message = "Quantity must be greater than zero." });

        var details = await _products.GetDetailsAsync(productId);
        if (details is null)
        {
            var single = await _products.GetAsync(productId);
            if (single is null) return BadRequest(new { ok = false, message = "Unknown product." });
            details = new ProductDetails(single.Id, single.Name, single.Category, single.igProductId, single.unit, new List<ProductComponent>());
        }

        var reasonText = await ResolveReasonAsync(reasonId, customReason);

        var batchId = Guid.TryParse(clientId, out var parsedBatch) ? parsedBatch : Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var toAdd = new List<WasteEntry>();

        if (details.Components is { Count: > 0 })
        {
            foreach (var comp in details.Components)
            {
                if (comp.Id <= 0) continue;
                var compQty = ((comp.Quantity ?? 1m) * quantity);
                var entryId = DeterministicId($"waste:batch:{batchId}:comp:{comp.Id}:qty:{compQty.ToString("G29", CultureInfo.InvariantCulture)}");

                toAdd.Add(new WasteEntry(
                    entryId,
                    now,
                    operatorId,
                    productId,
                    comp.Id,
                    comp.Unit ?? string.Empty,
                    $"{details.Name} – {comp.Name}",
                    reasonText,
                    compQty
                ));
            }
        }
        else
        {
            var entryId = DeterministicId($"waste:batch:{batchId}:single:{details.igProductId}:qty:{quantity.ToString("G29", CultureInfo.InvariantCulture)}");

            toAdd.Add(new WasteEntry(
                entryId,
                now,
                operatorId,
                productId,
                details.igProductId,
                details.Unit,
                details.Name,
                reasonText,
                quantity
            ));
        }

        var added = new List<WasteEntry>();
        try
        {
            foreach (var e in toAdd)
            {
                await _waste.AddAsync(e);
                added.Add(e);
            }
        }
        catch
        {
            foreach (var e in added)
            {
                try { await _waste.DeleteAsync(e.Id); } catch { }
            }
            return StatusCode(500, new { ok = false, message = "Failed to log waste." });
        }

        var payload = added.Select(e => new
        {
            id = e.Id,
            at = e.At.ToString("o"),
            operatorId = e.OperatorId,
            productId = e.ProductId,
            igProductId = e.IGProductId,
            unit = e.Unit,
            productName = e.ProductName,
            reason = e.Reason,
            quantity = e.Quantity,
            clientBatchId = batchId
        }).ToList();

        await _hub.Clients.All.SendAsync("EntriesAdded", payload);
        foreach (var e in payload)
        {
            await _hub.Clients.All.SendAsync("EntryAdded", e);
        }

        return Ok(new { ok = true, count = added.Count, batchId });
    }

    private async Task<string> ResolveReasonAsync(Guid? reasonId, string? customReason)
    {
        var rt = customReason?.Trim();
        if (reasonId.HasValue)
        {
            var r = await _reasons.GetAsync(reasonId.Value);
            if (r != null) rt = r.Name;
        }
        return string.IsNullOrWhiteSpace(rt) ? "Unspecified" : rt;
    }

    private static Guid DeterministicId(string key)
    {
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(key));
        return new Guid(hash);
    }
}
