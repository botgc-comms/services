using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

namespace BOTGC.POS.Controllers;

[ApiController]
public sealed class VoucherController : ControllerBase
{
    private readonly IHttpClientFactory _factory;

    public VoucherController(IHttpClientFactory factory)
    {
        _factory = factory;
    }

    [HttpGet("/voucher/redeem")]
    public async Task<IActionResult> Redeem([FromQuery] string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return BadRequest(new { ok = false, message = "Missing payload." });
        }

        var client = _factory.CreateClient("Api");

        var res = await client.PostAsJsonAsync("/api/epos/vouchers/redeem", new { Code = payload });

        if (!res.IsSuccessStatusCode)
        {
            return StatusCode((int)res.StatusCode);
        }

        var dto = await res.Content.ReadFromJsonAsync<VoucherRedemptionResult>();
        if (dto is null)
        {
            return StatusCode(500, new { ok = false, message = "Unexpected response from API." });
        }

        return Ok(dto);
    }

    [HttpPost("/voucher/callback")]
    public IActionResult Callback([FromBody] QrCallbackDto callback)
    {
        // For now just acknowledge receipt; can hook SignalR/UI here later.
        return Ok(new { ok = true });
    }

    public sealed record QrCallbackDto(
        bool success,
        string? reason,
        int memberId,
        Guid productId,
        Guid? voucherId,
        string? status
    );

    private sealed record RedeemRequest(string Payload);

    private sealed record VoucherRedemptionResult(
        Guid VoucherId,
        int AccountId,
        Guid ProductId,
        string ProductCode,
        string ProductName,
        decimal RedemptionValue,
        decimal AllowanceCharge,
        decimal RemainingAllowanceAfter,
        bool IsBonus,
        string? AwardReason,
        DateTime RedeemedAt,
        string Status
    );
}
