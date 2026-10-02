using HiveCore.Models.ViewModels;
using HiveCore.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HiveCore.Controllers;

[Route("Profile")]
public class ProfileController : Controller
{
    private readonly IUserService _userService;
    private readonly IPostService _postService;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(
        IUserService userService,
        IPostService postService,
        ILogger<ProfileController> logger)
    {
        _userService = userService;
        _postService = postService;
        _logger = logger;
    }

    // ============================================================
    // Public profile
    // ============================================================
    [HttpGet("{username}")]
    [AllowAnonymous]
    public async Task<IActionResult> Index(
        string username,
        string? tab = "posts",
        int page = 1,
        CancellationToken ct = default)
    {
        var userId = User.Identity?.IsAuthenticated == true
            ? GetCurrentUserId()
            : (Guid?)null;

        var vm = await _userService.GetProfileAsync(
            username, userId, tab ?? "posts", page, 20, ct);

        if (vm == null) return NotFound();

        return View(vm);
    }

    // ============================================================
    // Followers / Following
    // ============================================================
    [HttpGet("{username}/followers")]
    [AllowAnonymous]
    public async Task<IActionResult> Followers(string username, CancellationToken ct)
    {
        var user = await _userService.GetByUsernameAsync(username, ct);
        if (user == null) return NotFound();

        var followers = await _userService.GetFollowersAsync(user.Id, ct);
        ViewBag.Username = username;
        ViewBag.Title = "Followers";
        return View(followers);
    }

    [HttpGet("{username}/following")]
    [AllowAnonymous]
    public async Task<IActionResult> Following(string username, CancellationToken ct)
    {
        var user = await _userService.GetByUsernameAsync(username, ct);
        if (user == null) return NotFound();

        var following = await _userService.GetFollowingAsync(user.Id, ct);
        ViewBag.Username = username;
        ViewBag.Title = "Following";
        return View(following);
    }

    // ============================================================
    // Edit
    // ============================================================
    [HttpGet("Edit")]
    [Authorize]
    public async Task<IActionResult> Edit(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var model = await _userService.GetEditProfileAsync(userId, ct);
        if (model == null) return NotFound();

        return View(model);
    }

    [HttpPost("Edit")]
    [Authorize]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(30 * 1024 * 1024)]
    public async Task<IActionResult> Edit(EditProfileViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            var userId = GetCurrentUserId();
            await _userService.UpdateProfileAsync(userId, model, ct);

            TempData["Success"] = "Profile updated.";
            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update profile");
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    // ============================================================
    // Settings
    // ============================================================
    [HttpGet("Settings")]
    [Authorize]
    public async Task<IActionResult> Settings(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var model = await _userService.GetSettingsAsync(userId, ct);
        if (model == null) return NotFound();

        return View(model);
    }

    [HttpPost("Settings")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Settings(ProfileSettingsViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);

        var userId = GetCurrentUserId();
        await _userService.UpdateSettingsAsync(userId, model, ct);

        TempData["Success"] = "Settings saved.";
        return RedirectToAction(nameof(Settings));
    }

    // ============================================================
    // Follow / Unfollow
    // ============================================================
    [HttpPost("Follow/{userId:guid}")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Follow(Guid userId, CancellationToken ct)
    {
        var current = GetCurrentUserId();
        var ok = await _userService.FollowAsync(current, userId, ct);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return Json(new { success = ok });

        return Redirect(Request.Headers.Referer.ToString() ?? "/");
    }

    [HttpPost("Unfollow/{userId:guid}")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unfollow(Guid userId, CancellationToken ct)
    {
        var current = GetCurrentUserId();
        var ok = await _userService.UnfollowAsync(current, userId, ct);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return Json(new { success = ok });

        return Redirect(Request.Headers.Referer.ToString() ?? "/");
    }

    [HttpPost("AcceptRequest/{requesterId:guid}")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AcceptRequest(Guid requesterId, CancellationToken ct)
    {
        var current = GetCurrentUserId();
        var ok = await _userService.AcceptFollowRequestAsync(current, requesterId, ct);

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return Json(new { success = ok });

        return RedirectToAction(nameof(Index), "Notification");
    }

    private Guid GetCurrentUserId()
        => Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
}