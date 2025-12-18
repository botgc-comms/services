using BOTGC.ManagementReports.Models;
using Microsoft.AspNetCore.Mvc;

namespace BOTGC.ManagementReports.Controllers;

public sealed class MembershipReportController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public MembershipReportController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var client = _httpClientFactory.CreateClient("MembershipApi");

        var financialYear = Request.Query.TryGetValue("financialYear", out var fy)
            ? fy.ToString()
            : null;

        var url = string.IsNullOrWhiteSpace(financialYear)
            ? "/api/members/report"
            : $"/api/members/report?financialYear={Uri.EscapeDataString(financialYear)}";

        var request = new HttpRequestMessage(HttpMethod.Get, url);

        if (Request.Query.ContainsKey("no-cache"))
        {
            request.Headers.TryAddWithoutValidation("Cache-Control", "no-cache");
            request.Headers.TryAddWithoutValidation("Pragma", "no-cache");
        }

        var response = await client.SendAsync(request, HttpContext.RequestAborted);
        response.EnsureSuccessStatusCode();

        var report = await response.Content.ReadFromJsonAsync<MembershipReportDto>(cancellationToken: HttpContext.RequestAborted);

        if (report == null)
        {
            return StatusCode(StatusCodes.Status502BadGateway, "Membership API returned an empty response.");
        }

        var pageLinks = BuildPageLinks(report.Links);

        var vm = new MembershipReportPageViewModel(report, pageLinks);

        return View(vm);
    }

    private IReadOnlyList<HateoasLink> BuildPageLinks(List<HateoasLink>? apiLinks)
    {
        if (apiLinks == null || apiLinks.Count == 0)
        {
            return Array.Empty<HateoasLink>();
        }

        var result = new List<HateoasLink>(apiLinks.Count);

        foreach (var link in apiLinks)
        {
            if (link == null)
            {
                continue;
            }

            var financialYear = TryExtractFinancialYearFromHref(link.Href);

            var pageHref = Url.ActionLink(
                action: nameof(Index),
                controller: "MembershipReport",
                values: string.IsNullOrWhiteSpace(financialYear) ? null : new { financialYear });

            result.Add(new HateoasLink
            {
                Rel = link.Rel ?? string.Empty,
                Href = pageHref ?? string.Empty,
                Method = string.IsNullOrWhiteSpace(link.Method) ? "GET" : link.Method
            });
        }

        return result;
    }

    private static string? TryExtractFinancialYearFromHref(string? href)
    {
        if (string.IsNullOrWhiteSpace(href))
        {
            return null;
        }

        if (!Uri.TryCreate(href, UriKind.RelativeOrAbsolute, out var uri))
        {
            return null;
        }

        if (!uri.IsAbsoluteUri)
        {
            uri = new Uri(new Uri("https://localhost/"), href);
        }

        var q = uri.Query;
        if (string.IsNullOrWhiteSpace(q))
        {
            return null;
        }

        var parts = q.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var p in parts)
        {
            var kv = p.Split('=', 2, StringSplitOptions.TrimEntries);
            if (kv.Length != 2)
            {
                continue;
            }

            if (!string.Equals(kv[0], "financialYear", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var value = Uri.UnescapeDataString(kv[1] ?? string.Empty);
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        return null;
    }
}
