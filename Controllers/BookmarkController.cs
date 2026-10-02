using HiveCore.Models.ViewModels;
using HiveCore.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HiveCore.Controllers;

[Authorize]
[Route("Bookmark")]
public class BookmarkController : Controller
{
    private readonly IPostService _postService;
    private readonly ILogger<BookmarkController> _logger;

    public BookmarkController(IPostService postService, ILogger<BookmarkController> logger)
    {
        _postService = postService;
        _logger = logger;
    }

    [HttpPost("Toggle/{postId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(Guid postId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();

        var post = await _postService.GetByIdAsync(postId, userId, ct);
        if (post == null) return NotFound();

        bool nowBookmarked;
        if (post.IsBookmarkedByCurrentUser)
        {
            await _postService.UnbookmarkAsync(postId, userId, ct);
            nowBookmarked = false;
        }
        else
        {
            await _postService.BookmarkAsync(postId, userId, ct);
            nowBookmarked = true;
        }

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return Json(new { success = true, bookmarked = nowBookmarked });

        return Redirect(Request.Headers.Referer.ToString() ?? "/");
    }

    [HttpGet("/Bookmarks")]
    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        var (posts, total) = await _postService.GetBookmarkedPostsAsync(userId, userId, page, 20, ct);

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(total / 20.0);

        return View("~/Views/Bookmarks/Index.cshtml", posts);
    }

    private Guid GetCurrentUserId()
        => Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
}