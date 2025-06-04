using BOTGC.API.Common;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace BOTGC.API.Services
{
    public class QuestPDFMemberApplicationFormGenerator : IMemberApplicationFormPdfGeneratorService
    {
        private readonly ILogger<QuestPDFMemberApplicationFormGenerator> _logger;
        private readonly AppSettings _settings;

        public QuestPDFMemberApplicationFormGenerator(
            ILogger<QuestPDFMemberApplicationFormGenerator> logger,
            IOptions<AppSettings> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));

            QuestPDF.Settings.License = LicenseType.Community;
        }

        public byte[] GeneratePdf(NewMemberApplicationDto application)
        {
            if (application == null) throw new ArgumentNullException(nameof(application));

            _logger.LogInformation("Starting PDF generation for ApplicationId {ApplicationId} ({Forename} {Surname}).",
                application.ApplicationId, application.Forename, application.Surname);

            var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "img", "logo.png");

            var assembly = typeof(QuestPDFMemberApplicationFormGenerator).Assembly;
            using var logoStream = assembly.GetManifestResourceStream("BOTGC.API.assets.img.logo.png");

            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);
                    page.Size(PageSizes.A4);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Content().Column(column =>
                    {
                        column.Spacing(20);

                        column.Item().Column(headerCol =>
                        {
                            if (logoStream != null)
                            {
                                headerCol.Item().AlignCenter().MaxWidth(100).Image(logoStream).FitWidth();
                            }
                            headerCol.Item().AlignCenter().Text("Burton on Trent Golf Club").Bold().FontSize(16);
                            headerCol.Item().AlignCenter().Text("43 Ashby Road East, Burton on Trent, DE15 0PS");
                            headerCol.Item().AlignCenter().Text("Tel: 01283 544551 Email: clubsecretary@botgc.co.uk");
                        });

                        column.Item().Text("Membership Application Form").FontSize(20).Bold().FontColor(Colors.Blue.Medium).AlignCenter();

                        column.Item().Text("Personal Details").Bold();
                        column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        column.Item().Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Spacing(5);
                                col.Item().Text($"Title: {application.Title}");
                                col.Item().Text($"Forename: {NameCasingHelper.CapitaliseForename(application.Forename)}");
                                col.Item().Text($"Date of Birth: {application.DateOfBirth:dd/MM/yyyy}");
                                col.Item().Text($"Alternative Phone: {application.AlternativeTelephone}");
                            });

                            row.RelativeItem().Column(col =>
                            {
                                col.Spacing(5);
                                col.Item().Text($"Gender: {(application.Gender == "M" ? "Male" : application.Gender == "F" ? "Female" : "Other")}");
                                col.Item().Text($"Surname: {NameCasingHelper.CapitaliseSurname(application.Surname)}");
                                col.Item().Text($"Telephone: {application.Telephone}");
                                col.Item().Text($"Email: {application.Email}");
                            });
                        });

                        column.Item().Text("Address").Bold();
                        column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        column.Item().Text($"{application.AddressLine1}, {application.AddressLine2}, {application.Town}, {application.County}, {application.Postcode}");

                        column.Item().Text("Membership Details").Bold();
                        column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        column.Item().Text($"Membership Category: {application.MembershipCategory}");
                        column.Item().Text($"Arrange Finance: {application.ArrangeFinance}");
                        if (application.HasCdhId && !string.IsNullOrWhiteSpace(application.CdhId))
                            column.Item().Text($"CDH ID: {application.CdhId}");

                        column.Item().Text("Preferences & Declarations").Bold();
                        column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        column.Item().Text($"Agreed to Club Rules: {(application.AgreeToClubRules ? "Yes" : "No")}");
                        column.Item().Text($"Application Date: {application.ApplicationDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)}");

                        column.Item().PaddingTop(30).Text($"Application ID: {application.ApplicationId}").FontSize(10).AlignCenter();
                    });
                });
            }).GeneratePdf();

            _logger.LogInformation("Successfully generated PDF for ApplicationId {ApplicationId}.", application.ApplicationId);

            return pdfBytes;
        }
    }
}
