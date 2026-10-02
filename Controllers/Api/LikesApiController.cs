using HiveCore.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HiveCore.Controllers.Api;

[ApiController]
[Route("api/likes")]
[Authorize]
[Produces("application/json")]
public class LikesApiController : ControllerBase
{
    private readonly IPostService _postService;
    private readonly ICommentService _commentService;
    private readonly ILogger<LikesApiController> _logger;

    public LikesApiController(
        IPostService postService,
        ICommentService commentService,
        ILogger<LikesApiController> logger)
    {
        _postService = postService;
        _commentService = commentService;
        _logger = logger;
    }

    // ============================================================
    // POST api/likes/post/{postId}
    // ============================================================
    [HttpPost("post/{postId:guid}")]
    public async Task<IActionResult> LikePost(Guid postId, CancellationToken ct)
    {
        var userId = GetCurrentUserId()!.Value;
        var ok = await _postService.LikeAsync(postId, userId, ct);
        if (!ok) return NotFound(new { error = "Post not found." });

        return Ok(new { success = true, liked = true });
    }

    // ============================================================
    // DELETE api/likes/post/{postId}
    // ============================================================
    [HttpDelete("post/{postId:guid}")]
    public async Task<IActionResult> UnlikePost(Guid postId, CancellationToken ct)
    {
        var userId = GetCurrentUserId()!.Value;
        var ok = await _postService.UnlikeAsync(postId, userId, ct);
        if (!ok) return NotFound(new { error = "Post not found." });

        return Ok(new { success = true, liked = false });
    }

    // ============================================================
    // POST api/likes/comment/{commentId}
    // ============================================================
    [HttpPost("comment/{commentId:guid}")]
    public async Task<IActionResult> LikeComment(Guid commentId, CancellationToken ct)
    {
        var userId = GetCurrentUserId()!.Value;
        var ok = await _commentService.LikeAsync(commentId, userId, ct);
        if (!ok) return NotFound(new { error = "Comment not found." });

        return Ok(new { success = true, liked = true });
    }

    // ============================================================
    // DELETE api/likes/comment/{commentId}
    // ============================================================
    [HttpDelete("comment/{commentId:guid}")]
    public async Task<IActionResult> UnlikeComment(Guid commentId, CancellationToken ct)
    {
        var userId = GetCurrentUserId()!.Value;
        var ok = await _commentService.UnlikeAsync(commentId, userId, ct);
        if (!ok) return NotFound(new { error = "Comment not found." });

        return Ok(new { success = true, liked = false });
    }

    private Guid? GetCurrentUserId()
    {
        if (User.Identity?.IsAuthenticated != true) return null;
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}