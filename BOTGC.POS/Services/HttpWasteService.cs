using BOTGC.POS.Models;

namespace BOTGC.POS.Services;

public sealed class HttpWasteService : IWasteService
{
    private readonly IHttpClientFactory _factory;

    public HttpWasteService(IHttpClientFactory factory)
    {
        _factory = factory;
    }

    public async Task<WasteSheet> GetTodayAsync()
    {
        var client = _factory.CreateClient("Api");
        var day = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
        var dto = await client.GetFromJsonAsync<WasteSheetDto>($"api/stock/wasteSheet?day={day}");
        var sheet = new WasteSheet { Date = DateTime.UtcNow.Date };
        if (dto == null) return sheet;

        sheet.Submitted = string.Equals(dto.Status, "Submitted", StringComparison.OrdinalIgnoreCase);
        sheet.Entries = dto.Entries
            .Select(e => new WasteEntry(
                e.ClientEntryId,
                e.CreatedAtUtc,
                e.OperatorId,
                e.ProductId, 
                e.IgProductId,
                e.Unit, 
                e.ProductName,
                e.Reason,
                e.Quantity))
            .OrderByDescending(x => x.At)
            .ToList();

        return sheet;
    }
    public async Task<bool> DeleteAsync(Guid id)
    {
        var client = _factory.CreateClient("Api");
        var res = await client.DeleteAsync($"/api/stock/wasteSheet/entry/{id}");
        return res.IsSuccessStatusCode;
    }

    public async Task AddAsync(WasteEntry entry)
    {
        var client = _factory.CreateClient("Api");
        var day = entry.At.UtcDateTime.Date.ToString("yyyy-MM-dd");

        var payload = new AddWasteEntryRequest(
            entry.Id,
            entry.OperatorId,
            entry.ProductId,
            entry.ProductName,
            entry.Reason,
            entry.Quantity,
            null
        );

        var res = await client.PostAsJsonAsync($"/api/stock/wasteSheet/entry?day={day}", payload);
        res.EnsureSuccessStatusCode();
    }

    public Task SubmitTodayAsync()
    {
        return Task.CompletedTask;
    }

    private sealed record AddWasteEntryRequest(
        Guid ClientEntryId,
        Guid OperatorId,
        Guid ProductId,
        string ProductName,
        string Reason,
        decimal Quantity,
        string? DeviceId
    );

    private sealed record WasteEntryDto(
        Guid ClientEntryId,
        DateTimeOffset CreatedAtUtc,
        Guid OperatorId,
        Guid ProductId,
        long IgProductId,
        string Unit, 
        string ProductName,
        string Reason,
        decimal Quantity,
        string? DeviceId
    );

    private sealed record WasteSheetDto(
        DateTime Date,
        string Status,
        List<WasteEntryDto> Entries
    );
}

