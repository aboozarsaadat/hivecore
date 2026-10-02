using HiveCore.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HiveCore.Controllers;

[Authorize]
[Route("Notification")]
public class NotificationController : Controller
{
    private readonly IFeedService _feedService;
    private readonly INotificationService _notificationService;

    public NotificationController(
        IFeedService feedService,
        INotificationService notificationService)
    {
        _feedService = feedService;
        _notificationService = notificationService;
    }

    [HttpGet("")]
    [HttpGet("/Notifications")]
    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var vm = await _feedService.GetNotificationsAsync(userId, page, 20, ct);
        return View(vm);
    }

    [HttpPost("MarkRead/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        await _notificationService.MarkReadAsync(id, userId, ct);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return Json(new { success = true });

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("MarkAllRead")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        await _notificationService.MarkAllReadAsync(userId, ct);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return Json(new { success = true });

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("UnreadCount")]
    public async Task<IActionResult> UnreadCount(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var count = await _notificationService.GetUnreadCountAsync(userId, ct);
        return Json(new { count });
    }

    private Guid GetCurrentUserId()
        => Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
}