using HiveCore.Data;
using HiveCore.Models.Entities;
using HiveCore.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HiveCore.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = Constants.Roles.Admin + "," + Constants.Roles.Moderator)]
[Route("Admin/[controller]/[action]")]
public class DashboardController : Controller
{
    private readonly HiveCoreDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        HiveCoreDbContext context,
        UserManager<User> userManager,
        ILogger<DashboardController> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    // ============================================================
    // GET: /Admin/Dashboard
    // ============================================================
    [HttpGet]
    [HttpGet("/Admin")]
    [HttpGet("/Admin/Dashboard")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var last24h = now.AddHours(-24);
        var last7d = now.AddDays(-7);
        var last30d = now.AddDays(-30);

        // ============================================================
        // KPI metrics
        // ============================================================
        var totalUsers = await _context.Users.CountAsync(u => !u.IsDeleted, ct);
        var activeUsers24h = await _context.Users.CountAsync(u => u.LastLoginAt >= last24h, ct);
        var newUsers7d = await _context.Users.CountAsync(u => u.CreatedAt >= last7d, ct);
        var suspendedUsers = await _context.Users.CountAsync(u => u.IsSuspended, ct);

        var totalPosts = await _context.Posts.CountAsync(p => !p.IsDeleted, ct);
        var newPosts24h = await _context.Posts.CountAsync(p => p.CreatedAt >= last24h && !p.IsDeleted, ct);
        var newPosts7d = await _context.Posts.CountAsync(p => p.CreatedAt >= last7d && !p.IsDeleted, ct);

        var totalComments = await _context.Comments.CountAsync(c => !c.IsDeleted, ct);
        var newComments24h = await _context.Comments.CountAsync(c => c.CreatedAt >= last24h && !c.IsDeleted, ct);

        var totalLikes = await _context.Likes.CountAsync(ct);
        var totalFollows = await _context.Follows.CountAsync(f => !f.IsPending, ct);

        // ============================================================
        // Growth chart (last 14 days)
        // ============================================================
        var startDate = now.Date.AddDays(-13);

        var usersPerDay = await _context.Users
            .Where(u => u.CreatedAt >= startDate)
            .GroupBy(u => u.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var postsPerDay = await _context.Posts
            .Where(p => p.CreatedAt >= startDate && !p.IsDeleted)
            .GroupBy(p => p.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        // ============================================================
        // Recent activity
        // ============================================================
        var recentUsers = await _context.Users
            .Where(u => !u.IsDeleted)
            .OrderByDescending(u => u.CreatedAt)
            .Take(5)
            .Select(u => new RecentUserVm
            {
                Id = u.Id,
                Username = u.UserName ?? "unknown",
                DisplayName = u.DisplayName,
                AvatarUrl = u.AvatarUrl,
                IsVerified = u.IsVerified,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync(ct);

        var recentPosts = await _context.Posts
            .Where(p => !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .Take(5)
            .Select(p => new RecentPostVm
            {
                Id = p.Id,
                AuthorUsername = p.Author.UserName ?? "unknown",
                Content = p.Content,
                LikesCount = p.LikesCount,
                CommentsCount = p.CommentsCount,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync(ct);

        // Top trending hashtags (last 7 days)
        var recentPostContents = await _context.Posts
            .Where(p => !p.IsDeleted && p.CreatedAt >= last7d)
            .Select(p => p.Content)
            .ToListAsync(ct);

        var trendingHashtags = recentPostContents
            .SelectMany(c => HiveCore.Extensions.StringExtensions.ExtractHashtags(c))
            .GroupBy(h => h)
            .Select(g => new TrendingTagVm { Tag = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToList();

        var model = new AdminDashboardViewModel
        {
            TotalUsers = totalUsers,
            ActiveUsers24h = activeUsers24h,
            NewUsers7d = newUsers7d,
            SuspendedUsers = suspendedUsers,

            TotalPosts = totalPosts,
            NewPosts24h = newPosts24h,
            NewPosts7d = newPosts7d,

            TotalComments = totalComments,
            NewComments24h = newComments24h,

            TotalLikes = totalLikes,
            TotalFollows = totalFollows,

            UsersPerDay = usersPerDay.ToDictionary(x => x.Date.ToString("yyyy-MM-dd"), x => x.Count),
            PostsPerDay = postsPerDay.ToDictionary(x => x.Date.ToString("yyyy-MM-dd"), x => x.Count),

            RecentUsers = recentUsers,
            RecentPosts = recentPosts,
            TrendingHashtags = trendingHashtags,

            ServerTimeUtc = now,
            AppVersion = Constants.AppVersion
        };

        return View(model);
    }

    // ============================================================
    // Health / system info
    // ============================================================
    [HttpGet("/Admin/Dashboard/SystemInfo")]
    [Authorize(Roles = Constants.Roles.Admin)]
    public async Task<IActionResult> SystemInfo(CancellationToken ct)
    {
        var canConnect = await _context.Database.CanConnectAsync(ct);

        var info = new
        {
            database = new
            {
                canConnect,
                provider = _context.Database.ProviderName,
                pendingMigrations = (await _context.Database.GetPendingMigrationsAsync(ct)).ToList(),
                appliedMigrations = (await _context.Database.GetAppliedMigrationsAsync(ct)).Count()
            },
            environment = new
            {
                machineName = Environment.MachineName,
                osVersion = Environment.OSVersion.ToString(),
                dotnetVersion = Environment.Version.ToString(),
                processorCount = Environment.ProcessorCount,
                workingSetMb = Environment.WorkingSet / 1024 / 1024,
                uptimeSeconds = Environment.TickCount64 / 1000
            },
            serverUtc = DateTime.UtcNow
        };

        return Json(info);
    }
}

// ============================================================
// ViewModels (scoped to Admin area)
// ============================================================
public class AdminDashboardViewModel
{
    public int TotalUsers { get; set; }
    public int ActiveUsers24h { get; set; }
    public int NewUsers7d { get; set; }
    public int SuspendedUsers { get; set; }

    public int TotalPosts { get; set; }
    public int NewPosts24h { get; set; }
    public int NewPosts7d { get; set; }

    public int TotalComments { get; set; }
    public int NewComments24h { get; set; }

    public int TotalLikes { get; set; }
    public int TotalFollows { get; set; }

    public Dictionary<string, int> UsersPerDay { get; set; } = new();
    public Dictionary<string, int> PostsPerDay { get; set; } = new();

    public List<RecentUserVm> RecentUsers { get; set; } = new();
    public List<RecentPostVm> RecentPosts { get; set; } = new();
    public List<TrendingTagVm> TrendingHashtags { get; set; } = new();

    public DateTime ServerTimeUtc { get; set; }
    public string AppVersion { get; set; } = "1.0.0";
}

public class RecentUserVm
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public bool IsVerified { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RecentPostVm
{
    public Guid Id { get; set; }
    public string AuthorUsername { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int LikesCount { get; set; }
    public int CommentsCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TrendingTagVm
{
    public string Tag { get; set; } = string.Empty;
    public int Count { get; set; }
}