using HiveCore.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HiveCore.Controllers;

[Authorize]
[Route("Like")]
public class LikeController : Controller
{
    private readonly IPostService _postService;
    private readonly ICommentService _commentService;

    public LikeController(IPostService postService, ICommentService commentService)
    {
        _postService = postService;
        _commentService = commentService;
    }

    [HttpPost("Post/{id:guid}/Toggle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePost(Guid id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();

        // Check current state via service
        var post = await _postService.GetByIdAsync(id, userId, ct);
        if (post == null) return NotFound();

        bool nowLiked;
        if (post.IsLikedByCurrentUser)
        {
            await _postService.UnlikeAsync(id, userId, ct);
            nowLiked = false;
        }
        else
        {
            await _postService.LikeAsync(id, userId, ct);
            nowLiked = true;
        }

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return Json(new { success = true, liked = nowLiked });

        return Redirect(Request.Headers.Referer.ToString() ?? "/");
    }

    [HttpPost("Comment/{id:guid}/Toggle")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleComment(Guid id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();

        var comment = await _commentService.GetByIdAsync(id, userId, ct);
        if (comment == null) return NotFound();

        bool nowLiked;
        if (comment.IsLikedByCurrentUser)
        {
            await _commentService.UnlikeAsync(id, userId, ct);
            nowLiked = false;
        }
        else
        {
            await _commentService.LikeAsync(id, userId, ct);
            nowLiked = true;
        }

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return Json(new { success = true, liked = nowLiked });

        return Redirect(Request.Headers.Referer.ToString() ?? "/");
    }

    private Guid GetCurrentUserId()
        => Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
}