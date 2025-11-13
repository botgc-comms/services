using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace BOTGC.API.Services;

public sealed class QuestPDFCompetitionPrizeInvoiceGenerator : ICompetitionPrizeInvoicePdfGeneratorService
{
    private readonly ILogger<QuestPDFCompetitionPrizeInvoiceGenerator> _logger;
    private readonly AppSettings _settings;

    public QuestPDFCompetitionPrizeInvoiceGenerator(
        ILogger<QuestPDFCompetitionPrizeInvoiceGenerator> logger,
        IOptions<AppSettings> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));

        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateInvoice(CompetitionWinningsSummaryDto summary, string invoiceId)
    {
        if (summary == null)
        {
            throw new ArgumentNullException(nameof(summary));
        }

        if (string.IsNullOrWhiteSpace(invoiceId))
        {
            throw new ArgumentException("InvoiceId is required.", nameof(invoiceId));
        }

        var allPlacings = summary.Divisions.SelectMany(d => d.Placings).ToList();
        if (allPlacings.Count == 0)
        {
            throw new InvalidOperationException("Cannot generate invoice without any prize placings.");
        }

        var culture = CultureInfo.GetCultureInfo("en-GB");
        var totalPayout = allPlacings.Sum(p => p.Amount);

        _logger.LogInformation(
            "Generating competition prize invoice PDF for CompetitionId {CompetitionId}, InvoiceId {InvoiceId}.",
            summary.CompetitionId,
            invoiceId);

        var assembly = typeof(QuestPDFCompetitionPrizeInvoiceGenerator).Assembly;
        using var logoStream = assembly.GetManifestResourceStream("BOTGC.API.assets.img.logo.png");

        var pdfBytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.Size(PageSizes.A4);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Content().Column(column =>
                {
                    column.Spacing(20);

                    column.Item().Column(headerCol =>
                    {
                        if (logoStream != null)
                        {
                            headerCol.Item()
                                .AlignCenter()
                                .MaxWidth(100)
                                .Image(logoStream)
                                .FitWidth();
                        }

                        headerCol.Item()
                            .AlignCenter()
                            .Text("Burton on Trent Golf Club")
                            .Bold()
                            .FontSize(16);

                        headerCol.Item()
                            .AlignCenter()
                            .Text("43 Ashby Road East, Burton on Trent, DE15 0PS");

                        headerCol.Item()
                            .AlignCenter()
                            .Text("Tel: 01283 544551 Email: clubsecretary@botgc.co.uk");
                    });

                    column.Item()
                        .Text("Competition Prize Invoice")
                        .FontSize(20)
                        .Bold()
                        .FontColor(Colors.Blue.Medium)
                        .AlignCenter();

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Spacing(4);
                            left.Item().Text($"Invoice ID: {invoiceId}");
                            left.Item().Text($"Competition: {summary.CompetitionName}");
                            left.Item().Text($"Competition ID: {summary.CompetitionId}");
                            left.Item().Text($"Competition Date: {summary.CompetitionDate.ToString("dd/MM/yyyy", culture)}");
                            left.Item().Text($"Entrants: {summary.Entrants}");
                        });

                        row.RelativeItem().AlignRight().Column(right =>
                        {
                            right.Spacing(4);
                            right.Item().Text($"Invoice Date: {DateTime.UtcNow.ToString("dd/MM/yyyy", culture)}");
                            right.Item().Text("Recipient: Professional Shop");
                            right.Item().Text($"Currency: {summary.Currency}");
                        });
                    });

                    column.Item().PaddingTop(10).Text("Prize Breakdown").Bold();
                    column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2); // Division
                            columns.RelativeColumn(1); // Position
                            columns.RelativeColumn(4); // Player
                            columns.RelativeColumn(2); // Amount
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCellStyle).Text("Division");
                            header.Cell().Element(HeaderCellStyle).Text("Position");
                            header.Cell().Element(HeaderCellStyle).Text("Player");
                            header.Cell().Element(HeaderCellStyle).AlignRight().Text("Prize");
                        });

                        foreach (var (division, placing) in summary.Divisions
                                        .OrderBy(d => d.DivisionNumber)
                                        .SelectMany(d => d.Placings
                                            .OrderBy(p => p.Position)
                                            .ThenBy(p => p.CompetitorName)
                                            .Select(p => (Division: d, Placing: p))))
                        {
                            table.Cell().Element(BodyCellStyle)
                                .Text(string.IsNullOrWhiteSpace(division.DivisionName)
                                    ? $"Division {division.DivisionNumber}"
                                    : division.DivisionName);

                            table.Cell().Element(BodyCellStyle)
                                .Text(placing.Position.ToString(culture));

                            table.Cell().Element(BodyCellStyle)
                                .Text(placing.CompetitorName);

                            table.Cell().Element(BodyCellStyle)
                                .AlignRight()
                                .Text(FormatMoney(placing.Amount, summary.Currency, culture));
                        }
                    });

                    column.Item().PaddingTop(10).AlignRight().Column(totals =>
                    {
                        totals.Spacing(2);

                        totals.Item()
                            .Text($"Total prize payout due to Professional Shop: {FormatMoney(totalPayout, summary.Currency, culture)}")
                            .Bold()
                            .FontSize(12);

                        //if (totalPayout != summary.PrizePot)
                        //{
                        //    totals.Item()
                        //        .Text($"Note: Prize pot {FormatMoney(summary.PrizePot, summary.Currency, culture)} " +
                        //                $"vs itemised payout {FormatMoney(totalPayout, summary.Currency, culture)}.")
                        //        .FontSize(9)
                        //        .FontColor(Colors.Grey.Darken1);
                        //}
                    });

                    column.Item().PaddingTop(30).AlignCenter()
                        .Text("The Professional Shop will credit the above total to the players' prize accounts.")
                        .FontSize(10);
                });
            });
        }).GeneratePdf();

        _logger.LogInformation(
            "Successfully generated competition prize invoice PDF for CompetitionId {CompetitionId}, InvoiceId {InvoiceId}.",
            summary.CompetitionId,
            invoiceId);

        return pdfBytes;
    }

    private static IContainer HeaderCellStyle(IContainer container)
    {
        return container
            .Padding(5)
            .Background(Colors.Grey.Lighten3)
            .DefaultTextStyle(t => t.SemiBold());
    }

    private static IContainer BodyCellStyle(IContainer container)
    {
        return container
            .Padding(4)
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten3);
    }

    private static string FormatMoney(decimal amount, string currency, CultureInfo culture)
    {
        var symbol = currency == "GBP" ? "£" : string.Empty;
        return symbol + amount.ToString("0.00", culture);
    }
}
