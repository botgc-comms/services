using System;
using System.Collections.Generic;

namespace BOTGC.POS.Models
{
    public sealed record Operator(Guid Id, string DisplayName);

    public sealed record Product(Guid Id, string Name, string Category, bool IsComposite);

    public sealed record Reason(Guid Id, string Name, decimal? DefaultQuantity);

    public sealed record WasteEntry(
        Guid Id,
        DateTimeOffset At,
        Guid OperatorId,
        Guid ProductId,
        string ProductName,
        string Reason,
        decimal Quantity
    );

    public sealed class WasteSheet
    {
        public DateTime Date { get; set; } = DateTime.UtcNow.Date;
        public List<WasteEntry> Entries { get; set; } = new();
        public bool Submitted { get; set; }
    }

    public sealed class WastageViewModel
    {
        public WasteSheet Sheet { get; set; } = new();
        public IReadOnlyList<Product> TopProducts { get; set; } = Array.Empty<Product>();
        public IReadOnlyList<Reason> Reasons { get; set; } = Array.Empty<Reason>();
        public IReadOnlyList<Operator> Operators { get; set; } = Array.Empty<Operator>();
    }
}
