using HiveCore.Models.ViewModels;
using HiveCore.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HiveCore.Controllers;

[Authorize]
[Route("Post")]
public class PostController : Controller
{
    private readonly IPostService _postService;
    private readonly ICommentService _commentService;
    private readonly ILogger<PostController> _logger;

    public PostController(
        IPostService postService,
        ICommentService commentService,
        ILogger<PostController> logger)
    {
        _postService = postService;
        _commentService = commentService;
        _logger = logger;
    }

    // ============================================================
    // Create
    // ============================================================
    [HttpGet("Create")]
    public IActionResult Create(Guid? parentPostId = null)
    {
        return View(new PostViewModel { ParentPostId = parentPostId });
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(150 * 1024 * 1024)]
    public async Task<IActionResult> Create(PostViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return BadRequest(ModelState);

            return View(model);
        }

        try
        {
            var userId = GetCurrentUserId();
            var dto = await _postService.CreateAsync(model, userId, ct);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = true, post = dto });

            TempData["Success"] = "Post published.";
            return RedirectToAction(nameof(Details), new { id = dto.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create post");
            ModelState.AddModelError(string.Empty, ex.Message);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return BadRequest(new { error = ex.Message });

            return View(model);
        }
    }

    // ============================================================
    // Details
    // ============================================================
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> Details(Guid id, int page = 1, CancellationToken ct = default)
    {
        var userId = User.Identity?.IsAuthenticated == true ? GetCurrentUserId() : (Guid?)null;

        var vm = await _postService.GetDetailsAsync(id, userId, ct);
        if (vm == null) return NotFound();

        var (comments, total) = await _commentService.GetForPostAsync(id, userId, page, 30, ct);
        vm.Comments = comments.ToList();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(total / 30.0);

        await _postService.IncrementViewAsync(id, ct);

        return View(vm);
    }

    // ============================================================
    // Edit — GET
    // ============================================================
    [HttpGet("Edit/{id:guid}")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var post = await _postService.GetByIdAsync(id, userId, ct);

        if (post == null) return NotFound();
        if (!post.CanEdit) return Forbid();

        var model = new PostViewModel
        {
            Id = post.Id,
            Content = post.Content,
            IsSensitive = post.IsSensitive,
            Visibility = post.Visibility,
            ExistingMedia = post.Media ?? new List<HiveCore.Models.DTOs.MediaDto>()
        };

        return View(model);
    }

    // ============================================================
    // Edit — POST (content + media)
    // ============================================================
    [HttpPost("Edit/{id:guid}")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(150 * 1024 * 1024)]
    public async Task<IActionResult> Edit(Guid id, PostViewModel model, CancellationToken ct)
    {
        // The Id comes from the route, so re-set it on the model
        model.Id = id;

        if (!ModelState.IsValid)
        {
            // Re-hydrate existing media so the form can render it again
            var existing = await _postService.GetByIdAsync(id, GetCurrentUserId(), ct);
            if (existing != null)
                model.ExistingMedia = existing.Media ?? new List<HiveCore.Models.DTOs.MediaDto>();

            return View(model);
        }

        try
        {
            var userId = GetCurrentUserId();
            var updated = await _postService.UpdateAsync(id, userId, model, ct);
            if (updated == null) return NotFound();

            TempData["Success"] = "Post updated.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);

            var existing = await _postService.GetByIdAsync(id, GetCurrentUserId(), ct);
            if (existing != null)
                model.ExistingMedia = existing.Media ?? new List<HiveCore.Models.DTOs.MediaDto>();

            return View(model);
        }
    }

    // ============================================================
    // Delete
    // ============================================================
    [HttpPost("Delete/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            var userId = GetCurrentUserId();
            var ok = await _postService.DeleteAsync(id, userId, ct);
            if (!ok) return NotFound();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = true });

            TempData["Success"] = "Post deleted.";
            return RedirectToAction("Index", "Home");
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    // ============================================================
    // Repost / Quote
    // ============================================================
    [HttpPost("Repost/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Repost(Guid id, string? comment, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var dto = await _postService.RepostAsync(id, userId, comment, ct);
        if (dto == null) return NotFound();

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return Json(new { success = true, post = dto });

        return RedirectToAction("Index", "Home");
    }

    private Guid GetCurrentUserId()
        => Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
}