using HiveCore.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HiveCore.Controllers;

[Route("Explore")]
public class ExploreController : Controller
{
    private readonly IFeedService _feedService;

    public ExploreController(IFeedService feedService)
    {
        _feedService = feedService;
    }

    [HttpGet("")]
    [AllowAnonymous]
    public async Task<IActionResult> Index(
        string? q = null,
        string? tab = "trending",
        int page = 1,
        CancellationToken ct = default)
    {
        var userId = User.Identity?.IsAuthenticated == true ? GetCurrentUserId() : (Guid?)null;

        var vm = await _feedService.GetExploreAsync(userId, q, tab, page, 20, ct);
        return View(vm);
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}