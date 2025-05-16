using BOTGC.ManagementReports.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace BOTGC.ManagementReports.Controllers;

public class MembershipReportController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public MembershipReportController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IActionResult> Index()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true, 
            IncludeFields = true
        };

        var client = _httpClientFactory.CreateClient("MembershipApi");
        var response = await client.GetAsync("/api/members/report");

        response.EnsureSuccessStatusCode();

        var data = await response.Content.ReadFromJsonAsync<MembershipReportDto>();

        return View(data); // Pass first report to the view
    }
}
