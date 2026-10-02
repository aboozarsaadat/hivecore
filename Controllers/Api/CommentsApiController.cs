using HiveCore.Models.ViewModels;
using HiveCore.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HiveCore.Controllers.Api;

[ApiController]
[Route("api/comments")]
[Authorize]
[Produces("application/json")]
public class CommentsApiController : ControllerBase
{
    private readonly ICommentService _commentService;
    private readonly ILogger<CommentsApiController> _logger;

    public CommentsApiController(
        ICommentService commentService,
        ILogger<CommentsApiController> logger)
    {
        _commentService = commentService;
        _logger = logger;
    }

    // ============================================================
    // GET api/comments/{id}
    // ============================================================
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var current = GetCurrentUserId();
        var dto = await _commentService.GetByIdAsync(id, current, ct);
        if (dto == null) return NotFound(new { error = "Comment not found." });
        return Ok(dto);
    }

    // ============================================================
    // GET api/comments/post/{postId}
    // ============================================================
    [HttpGet("post/{postId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetForPost(
        Guid postId, int page = 1, int pageSize = 30, CancellationToken ct = default)
    {
        var current = GetCurrentUserId();
        var (comments, total) = await _commentService.GetForPostAsync(postId, current, page, pageSize, ct);

        return Ok(new
        {
            items = comments,
            page,
            pageSize,
            total,
            totalPages = (int)Math.Ceiling(total / (double)pageSize)
        });
    }

    // ============================================================
    // POST api/comments
    // ============================================================
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CommentViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var userId = GetCurrentUserId()!.Value;
            var dto = await _commentService.CreateAsync(model, userId, ct);
            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // ============================================================
    // PUT api/comments/{id}
    // ============================================================
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateCommentRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest(new { error = "Content is required." });

        try
        {
            var userId = GetCurrentUserId()!.Value;
            var dto = await _commentService.UpdateAsync(id, userId, request.Content, ct);
            if (dto == null) return NotFound();
            return Ok(dto);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    // ============================================================
    // DELETE api/comments/{id}
    // ============================================================
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            var userId = GetCurrentUserId()!.Value;
            var ok = await _commentService.DeleteAsync(id, userId, ct);
            if (!ok) return NotFound();
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    private Guid? GetCurrentUserId()
    {
        if (User.Identity?.IsAuthenticated != true) return null;
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}

public class UpdateCommentRequest
{
    public string Content { get; set; } = string.Empty;
}