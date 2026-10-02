using HiveCore.Models.ViewModels;
using HiveCore.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HiveCore.Controllers;

[Authorize]
[Route("Comment")]
public class CommentController : Controller
{
    private readonly ICommentService _commentService;
    private readonly ILogger<CommentController> _logger;

    public CommentController(ICommentService commentService, ILogger<CommentController> logger)
    {
        _commentService = commentService;
        _logger = logger;
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CommentViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return BadRequest(ModelState);
            return RedirectToAction("Details", "Post", new { id = model.PostId });
        }

        try
        {
            var userId = GetCurrentUserId();
            var dto = await _commentService.CreateAsync(model, userId, ct);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = true, comment = dto });

            return RedirectToAction("Details", "Post", new { id = model.PostId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create comment");
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return BadRequest(new { error = ex.Message });
            return RedirectToAction("Details", "Post", new { id = model.PostId });
        }
    }

    [HttpPost("Edit/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, string content, CancellationToken ct)
    {
        try
        {
            var userId = GetCurrentUserId();
            var dto = await _commentService.UpdateAsync(id, userId, content, ct);
            if (dto == null) return NotFound();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = true, comment = dto });

            return RedirectToAction("Details", "Post", new { id = dto.PostId });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost("Delete/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            var userId = GetCurrentUserId();
            var ok = await _commentService.DeleteAsync(id, userId, ct);
            if (!ok) return NotFound();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return Json(new { success = true });

            return Redirect(Request.Headers.Referer.ToString() ?? "/");
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    private Guid GetCurrentUserId()
        => Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
}