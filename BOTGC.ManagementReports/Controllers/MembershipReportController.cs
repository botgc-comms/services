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
        var client = _httpClientFactory.CreateClient("MembershipApi");

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/members/report");

        if (Request.Query.ContainsKey("no-cache"))
        {
            request.Headers.Add("Cache-Control", "no-cache");
        }

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var data = await response.Content.ReadFromJsonAsync<MembershipReportDto>();

        return View(data);
    }
}
