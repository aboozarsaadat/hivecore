using HiveCore.Data;
using HiveCore.Models.Entities;
using HiveCore.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HiveCore.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Constants.Roles.Admin + "," + Constants.Roles.Moderator)]
[Route("Admin/[controller]/[action]")]
public class PostsController : Controller
{
    private readonly HiveCoreDbContext _context;
    private readonly ILogger<PostsController> _logger;

    public PostsController(HiveCoreDbContext context, ILogger<PostsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ============================================================
    // GET: /Admin/Posts
    // ============================================================
    [HttpGet]
    [HttpGet("/Admin/Posts")]
    public async Task<IActionResult> Index(
        string? q = null,
        string? filter = null,
        int page = 1,
        int pageSize = 25,
        CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 25 : pageSize;

        var query = _context.Posts
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(p => p.Author)
            .AsQueryable();

        // Filters
        switch (filter?.ToLowerInvariant())
        {
            case "deleted":
                query = query.Where(p => p.IsDeleted);
                break;
            case "hidden":
                query = query.Where(p => p.IsHidden);
                break;
            case "sensitive":
                query = query.Where(p => p.IsSensitive);
                break;
            case "reposts":
                query = query.Where(p => p.IsRepost || p.IsQuote);
                break;
            case "trending":
                query = query.Where(p => p.CreatedAt >= DateTime.UtcNow.AddDays(-1))
                    .OrderByDescending(p => p.LikesCount + p.CommentsCount);
                break;
            case "reported":
                // Placeholder: filter could use a separate Reports table in future
                break;
            case "live":
            default:
                query = query.Where(p => !p.IsDeleted);
                break;
        }

        // Search
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(p =>
                EF.Functions.Like(p.Content, $"%{term}%") ||
                EF.Functions.Like(p.Author.UserName!, $"%{term}%"));
        }

        var total = await query.CountAsync(ct);

        var posts = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new AdminPostRowVm
            {
                Id = p.Id,
                AuthorId = p.AuthorId,
                AuthorUsername = p.Author.UserName ?? "unknown",
                AuthorDisplayName = p.Author.DisplayName,
                AuthorAvatarUrl = p.Author.AvatarUrl,
                Content = p.Content,
                IsDeleted = p.IsDeleted,
                IsHidden = p.IsHidden,
                IsSensitive = p.IsSensitive,
                IsRepost = p.IsRepost,
                IsQuote = p.IsQuote,
                LikesCount = p.LikesCount,
                CommentsCount = p.CommentsCount,
                RepostsCount = p.RepostsCount,
                ViewsCount = p.ViewsCount,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync(ct);

        var vm = new AdminPostsIndexViewModel
        {
            Posts = posts,
            SearchQuery = q,
            Filter = filter,
            CurrentPage = page,
            PageSize = pageSize,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize),
            Stats = new AdminPostStatsVm
            {
                Total = await _context.Posts.IgnoreQueryFilters().CountAsync(ct),
                Live = await _context.Posts.CountAsync(p => !p.IsDeleted, ct),
                Deleted = await _context.Posts.IgnoreQueryFilters().CountAsync(p => p.IsDeleted, ct),
                Hidden = await _context.Posts.IgnoreQueryFilters().CountAsync(p => p.IsHidden, ct),
                Sensitive = await _context.Posts.IgnoreQueryFilters().CountAsync(p => p.IsSensitive, ct)
            }
        };

        return View(vm);
    }

    // ============================================================
    // GET: /Admin/Posts/Details/{id}
    // ============================================================
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var post = await _context.Posts
            .IgnoreQueryFilters()
            .Include(p => p.Author)
            .Include(p => p.Media)
            .Include(p => p.ParentPost)
                .ThenInclude(pp => pp!.Author)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (post == null) return NotFound();

        var commentsCount = await _context.Comments
            .IgnoreQueryFilters()
            .CountAsync(c => c.PostId == id, ct);

        var vm = new AdminPostDetailsViewModel
        {
            Id = post.Id,
            AuthorId = post.AuthorId,
            AuthorUsername = post.Author.UserName ?? "unknown",
            AuthorDisplayName = post.Author.DisplayName,
            AuthorAvatarUrl = post.Author.AvatarUrl,
            Content = post.Content,
            Visibility = post.Visibility.ToString(),
            IsDeleted = post.IsDeleted,
            IsHidden = post.IsHidden,
            IsSensitive = post.IsSensitive,
            IsRepost = post.IsRepost,
            IsQuote = post.IsQuote,
            LikesCount = post.LikesCount,
            CommentsCount = post.CommentsCount,
            RepostsCount = post.RepostsCount,
            BookmarksCount = post.BookmarksCount,
            ViewsCount = post.ViewsCount,
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt,
            DeletedAt = post.DeletedAt,
            ModerationNote = post.ModerationNote,
            ParentPostId = post.ParentPostId,
            ParentAuthorUsername = post.ParentPost?.Author?.UserName,
            ParentContent = post.ParentPost?.Content,
            MediaUrls = post.Media?.Where(m => !m.IsDeleted).Select(m => m.Url).ToList() ?? new(),
            TotalComments = commentsCount
        };

        return View(vm);
    }

    // ============================================================
    // POST: /Admin/Posts/Hide/{id}
    // ============================================================
    [HttpPost("{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Hide(Guid id, CancellationToken ct)
    {
        var post = await _context.Posts
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (post == null) return NotFound();

        post.IsHidden = !post.IsHidden;
        post.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        _logger.LogWarning("Admin {AdminId} toggled hide on post {PostId} → {State}",
            GetCurrentUserId(), id, post.IsHidden);

        TempData["Success"] = post.IsHidden ? "Post hidden." : "Post unhidden.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // ============================================================
    // POST: /Admin/Posts/MarkSensitive/{id}
    // ============================================================
    [HttpPost("{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkSensitive(Guid id, CancellationToken ct)
    {
        var post = await _context.Posts
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (post == null) return NotFound();

        post.IsSensitive = !post.IsSensitive;
        post.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        TempData["Success"] = post.IsSensitive
            ? "Post marked sensitive."
            : "Sensitive flag removed.";

        return RedirectToAction(nameof(Details), new { id });
    }

    // ============================================================
    // POST: /Admin/Posts/SoftDelete/{id}
    // ============================================================
    [HttpPost("{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SoftDelete(Guid id, CancellationToken ct)
    {
        var post = await _context.Posts
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (post == null) return NotFound();

        if (post.IsDeleted)
        {
            post.IsDeleted = false;
            post.DeletedAt = null;
            TempData["Success"] = "Post restored.";
        }
        else
        {
            post.IsDeleted = true;
            post.DeletedAt = DateTime.UtcNow;
            TempData["Success"] = "Post soft-deleted.";
        }

        post.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        _logger.LogWarning("Admin {AdminId} toggled soft-delete on post {PostId} → {State}",
            GetCurrentUserId(), id, post.IsDeleted);

        return RedirectToAction(nameof(Details), new { id });
    }

    // ============================================================
    // POST: /Admin/Posts/AddNote/{id}
    // ============================================================
    [HttpPost("{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddNote(Guid id, string note, CancellationToken ct)
    {
        var post = await _context.Posts
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (post == null) return NotFound();

        post.ModerationNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        post.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        TempData["Success"] = "Moderation note saved.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private Guid GetCurrentUserId()
        => Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
}

// ============================================================
// ViewModels
// ============================================================
public class AdminPostsIndexViewModel
{
    public List<AdminPostRowVm> Posts { get; set; } = new();
    public string? SearchQuery { get; set; }
    public string? Filter { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public AdminPostStatsVm Stats { get; set; } = new();
}

public class AdminPostStatsVm
{
    public int Total { get; set; }
    public int Live { get; set; }
    public int Deleted { get; set; }
    public int Hidden { get; set; }
    public int Sensitive { get; set; }
}

public class AdminPostRowVm
{
    public Guid Id { get; set; }
    public Guid AuthorId { get; set; }
    public string AuthorUsername { get; set; } = string.Empty;
    public string AuthorDisplayName { get; set; } = string.Empty;
    public string? AuthorAvatarUrl { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public bool IsHidden { get; set; }
    public bool IsSensitive { get; set; }
    public bool IsRepost { get; set; }
    public bool IsQuote { get; set; }
    public int LikesCount { get; set; }
    public int CommentsCount { get; set; }
    public int RepostsCount { get; set; }
    public int ViewsCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdminPostDetailsViewModel
{
    public Guid Id { get; set; }
    public Guid AuthorId { get; set; }
    public string AuthorUsername { get; set; } = string.Empty;
    public string AuthorDisplayName { get; set; } = string.Empty;
    public string? AuthorAvatarUrl { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Visibility { get; set; } = "Public";
    public bool IsDeleted { get; set; }
    public bool IsHidden { get; set; }
    public bool IsSensitive { get; set; }
    public bool IsRepost { get; set; }
    public bool IsQuote { get; set; }
    public int LikesCount { get; set; }
    public int CommentsCount { get; set; }
    public int RepostsCount { get; set; }
    public int BookmarksCount { get; set; }
    public int ViewsCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? ModerationNote { get; set; }
    public Guid? ParentPostId { get; set; }
    public string? ParentAuthorUsername { get; set; }
    public string? ParentContent { get; set; }
    public List<string> MediaUrls { get; set; } = new();
    public int TotalComments { get; set; }
}