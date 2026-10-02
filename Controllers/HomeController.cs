using System.Diagnostics;
using HiveCore.Models.ViewModels;
using HiveCore.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HiveCore.Controllers;

public class HomeController : Controller
{
    private readonly IFeedService _feedService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IFeedService feedService, ILogger<HomeController> logger)
    {
        _feedService = feedService;
        _logger = logger;
    }

    [HttpGet("/")]
    public async Task<IActionResult> Index(
        string feedType = "for-you",
        int page = 1,
        CancellationToken ct = default)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            // Show landing/guest page
            return View("Landing");
        }

        var userId = GetCurrentUserId();
        if (userId == null) return Challenge();

        var model = await _feedService.GetHomeFeedAsync(userId.Value, feedType, page, 25, ct);
        return View(model);
    }

    [HttpGet("/about")]
    public IActionResult About() => View();

    [HttpGet("/privacy")]
    public IActionResult Privacy() => View();

    [HttpGet("/terms")]
    public IActionResult Terms() => View();

    [HttpGet("/contact")]
    public IActionResult Contact() => View();

    [HttpGet("/error")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}

public class ErrorViewModel
{
    public string? RequestId { get; set; }
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}