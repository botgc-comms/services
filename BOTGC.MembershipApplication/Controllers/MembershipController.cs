using BOTGC.MembershipApplication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace BOTGC.MembershipApplication;

public class MembershipController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AppSettings _settings;

    public MembershipController(IHttpClientFactory httpClientFactory, IOptions<AppSettings> settings)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value;
    }

    [HttpGet]
    public IActionResult Apply()
    {
        ViewData["GetAddressApiKey"] = _settings.GetAddressIOSettings.ApiKey;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply(BOTGC.MembershipApplication.Models.MembershipApplication  application)
    {
        if (!ModelState.IsValid)
            return View(application);

        var client = _httpClientFactory.CreateClient("MembershipApi");

        var response = await client.PostAsJsonAsync("application", application);

        if (response.IsSuccessStatusCode)
            return RedirectToAction("Thanks");

        ModelState.AddModelError("", "An error occurred submitting your application. Please try again.");

        return View(application);
    }

    public IActionResult Thanks() => View();
}
