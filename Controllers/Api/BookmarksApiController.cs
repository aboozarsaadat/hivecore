using HiveCore.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HiveCore.Controllers.Api;

[ApiController]
[Route("api/bookmarks")]
[Authorize]
[Produces("application/json")]
public class BookmarksApiController : ControllerBase
{
    private readonly IPostService _postService;
    private readonly ILogger<BookmarksApiController> _logger;

    public BookmarksApiController(
        IPostService postService,
        ILogger<BookmarksApiController> logger)
    {
        _postService = postService;
        _logger = logger;
    }

    // ============================================================
    // GET api/bookmarks
    // ============================================================
    [HttpGet]
    public async Task<IActionResult> GetMine(
        int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var userId = GetCurrentUserId()!.Value;
        var (posts, total) = await _postService.GetBookmarkedPostsAsync(userId, userId, page, pageSize, ct);

        return Ok(new
        {
            items = posts,
            page,
            pageSize,
            total,
            totalPages = (int)Math.Ceiling(total / (double)pageSize)
        });
    }

    // ============================================================
    // POST api/bookmarks/{postId}
    // ============================================================
    [HttpPost("{postId:guid}")]
    public async Task<IActionResult> Add(Guid postId, CancellationToken ct)
    {
        var userId = GetCurrentUserId()!.Value;
        var ok = await _postService.BookmarkAsync(postId, userId, ct);
        if (!ok) return NotFound(new { error = "Post not found." });

        return Ok(new { success = true, bookmarked = true });
    }

    // ============================================================
    // DELETE api/bookmarks/{postId}
    // ============================================================
    [HttpDelete("{postId:guid}")]
    public async Task<IActionResult> Remove(Guid postId, CancellationToken ct)
    {
        var userId = GetCurrentUserId()!.Value;
        var ok = await _postService.UnbookmarkAsync(postId, userId, ct);
        if (!ok) return NotFound(new { error = "Post not found." });

        return Ok(new { success = true, bookmarked = false });
    }

    // ============================================================
    // POST api/bookmarks/{postId}/toggle
    // ============================================================
    [HttpPost("{postId:guid}/toggle")]
    public async Task<IActionResult> Toggle(Guid postId, CancellationToken ct)
    {
        var userId = GetCurrentUserId()!.Value;

        var post = await _postService.GetByIdAsync(postId, userId, ct);
        if (post == null) return NotFound(new { error = "Post not found." });

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

        return Ok(new { success = true, bookmarked = nowBookmarked });
    }

    private Guid? GetCurrentUserId()
    {
        if (User.Identity?.IsAuthenticated != true) return null;
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}