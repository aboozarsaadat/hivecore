using HiveCore.Filters;
using HiveCore.Models.ViewModels;
using HiveCore.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HiveCore.Controllers.Api;

[ApiController]
[Route("api/posts")]
[Authorize]
[Produces("application/json")]
public class PostsApiController : ControllerBase
{
    private readonly IPostService _postService;
    private readonly ILogger<PostsApiController> _logger;

    public PostsApiController(IPostService postService, ILogger<PostsApiController> logger)
    {
        _postService = postService;
        _logger = logger;
    }

    // ============================================================
    // GET api/posts/{id}
    // ============================================================
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var dto = await _postService.GetByIdAsync(id, userId, ct);
        if (dto == null) return NotFound(new { error = "Post not found." });
        return Ok(dto);
    }

    // ============================================================
    // GET api/posts/user/{userId}
    // ============================================================
    [HttpGet("user/{userId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByUser(
        Guid userId, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var current = GetCurrentUserId();
        var (posts, total) = await _postService.GetUserPostsAsync(userId, current, page, pageSize, ct);

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
    // GET api/posts/trending
    // ============================================================
    [HttpGet("trending")]
    [AllowAnonymous]
    public async Task<IActionResult> GetTrending(
        int count = 20, int withinHours = 24, CancellationToken ct = default)
    {
        var current = GetCurrentUserId();
        var posts = await _postService.GetTrendingAsync(count, withinHours, current, ct);
        return Ok(posts);
    }

    // ============================================================
    // GET api/posts/search?q=...
    // ============================================================
    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<IActionResult> Search(
        [FromQuery] string q, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { error = "Query is required." });

        var current = GetCurrentUserId();
        var (posts, total) = await _postService.SearchAsync(q, current, page, pageSize, ct);

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
    // POST api/posts
    // ============================================================
    [HttpPost]
    [RequestSizeLimit(150 * 1024 * 1024)]
    public async Task<IActionResult> Create([FromForm] PostViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var userId = GetCurrentUserId()!.Value;
            var dto = await _postService.CreateAsync(model, userId, ct);

            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // ============================================================
    // PUT api/posts/{id}
    // ============================================================
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
    Guid id, [FromBody] UpdatePostRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest(new { error = "Content is required." });

        try
        {
            var userId = GetCurrentUserId()!.Value;

            // Wrap into PostViewModel (no media changes via API — media upload
            // is a multipart concern; use the MVC endpoint for that).
            var model = new PostViewModel
            {
                Id = id,
                Content = request.Content,
                MediaFiles = null,
                MediaIdsToDelete = new List<Guid>()
            };

            var dto = await _postService.UpdateAsync(id, userId, model, ct);
            if (dto == null) return NotFound();
            return Ok(dto);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // ============================================================
    // DELETE api/posts/{id}
    // ============================================================
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            var userId = GetCurrentUserId()!.Value;
            var ok = await _postService.DeleteAsync(id, userId, ct);
            if (!ok) return NotFound();
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    // ============================================================
    // POST api/posts/{id}/repost
    // ============================================================
    [HttpPost("{id:guid}/repost")]
    public async Task<IActionResult> Repost(
        Guid id, [FromBody] RepostRequest? request, CancellationToken ct)
    {
        var userId = GetCurrentUserId()!.Value;
        var dto = await _postService.RepostAsync(id, userId, request?.Comment, ct);
        if (dto == null) return NotFound();

        return Ok(dto);
    }

    private Guid? GetCurrentUserId()
    {
        if (User.Identity?.IsAuthenticated != true) return null;
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}

public class UpdatePostRequest
{
    public string Content { get; set; } = string.Empty;
}

public class RepostRequest
{
    public string? Comment { get; set; }
}