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
public class UsersController : Controller
{
    private readonly HiveCoreDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        HiveCoreDbContext context,
        UserManager<User> userManager,
        ILogger<UsersController> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    // ============================================================
    // GET: /Admin/Users
    // ============================================================
    [HttpGet]
    [HttpGet("/Admin/Users")]
    public async Task<IActionResult> Index(
        string? q = null,
        string? filter = null,
        int page = 1,
        int pageSize = 25,
        CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 25 : pageSize;

        var query = _context.Users.AsNoTracking().AsQueryable();

        // Filters
        switch (filter?.ToLowerInvariant())
        {
            case "verified":
                query = query.Where(u => u.IsVerified);
                break;
            case "suspended":
                query = query.Where(u => u.IsSuspended);
                break;
            case "private":
                query = query.Where(u => u.IsPrivate);
                break;
            case "deleted":
                query = query.IgnoreQueryFilters().Where(u => u.IsDeleted);
                break;
            case "new":
                query = query.Where(u => u.CreatedAt >= DateTime.UtcNow.AddDays(-7));
                break;
            case "active":
                query = query.Where(u => u.LastLoginAt >= DateTime.UtcNow.AddHours(-24));
                break;
        }

        // Search
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(u =>
                EF.Functions.Like(u.UserName!, $"%{term}%") ||
                EF.Functions.Like(u.DisplayName, $"%{term}%") ||
                EF.Functions.Like(u.Email!, $"%{term}%"));
        }

        var total = await query.CountAsync(ct);

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new AdminUserRowVm
            {
                Id = u.Id,
                Username = u.UserName ?? string.Empty,
                DisplayName = u.DisplayName,
                Email = u.Email ?? string.Empty,
                AvatarUrl = u.AvatarUrl,
                IsVerified = u.IsVerified,
                IsPrivate = u.IsPrivate,
                IsSuspended = u.IsSuspended,
                IsDeleted = u.IsDeleted,
                FollowersCount = u.FollowersCount,
                FollowingCount = u.FollowingCount,
                PostsCount = u.PostsCount,
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt
            })
            .ToListAsync(ct);

        var vm = new AdminUsersIndexViewModel
        {
            Users = users,
            SearchQuery = q,
            Filter = filter,
            CurrentPage = page,
            PageSize = pageSize,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)pageSize),
            Stats = new AdminUserStatsVm
            {
                Total = await _context.Users.CountAsync(ct),
                Verified = await _context.Users.CountAsync(u => u.IsVerified, ct),
                Suspended = await _context.Users.CountAsync(u => u.IsSuspended, ct),
                Private = await _context.Users.CountAsync(u => u.IsPrivate, ct),
                Deleted = await _context.Users.IgnoreQueryFilters().CountAsync(u => u.IsDeleted, ct)
            }
        };

        return View(vm);
    }

    // ============================================================
    // GET: /Admin/Users/Details/{id}
    // ============================================================
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var user = await _context.Users
            .IgnoreQueryFilters()
            .Include(u => u.ProfileTheme)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);

        var vm = new AdminUserDetailsViewModel
        {
            Id = user.Id,
            Username = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            DisplayName = user.DisplayName,
            Bio = user.Bio,
            AvatarUrl = user.AvatarUrl,
            CoverUrl = user.CoverUrl,
            Location = user.Location,
            Website = user.Website,
            IsVerified = user.IsVerified,
            IsPrivate = user.IsPrivate,
            IsSuspended = user.IsSuspended,
            IsDeleted = user.IsDeleted,
            EmailConfirmed = user.EmailConfirmed,
            TwoFactorEnabled = user.TwoFactorEnabled,
            LockoutEnabled = user.LockoutEnabled,
            AccessFailedCount = user.AccessFailedCount,
            LockoutEnd = user.LockoutEnd,
            FollowersCount = user.FollowersCount,
            FollowingCount = user.FollowingCount,
            PostsCount = user.PostsCount,
            LikesReceivedCount = user.LikesReceivedCount,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            LastLoginAt = user.LastLoginAt,
            LastLoginIp = user.LastLoginIp,
            Roles = roles.ToList()
        };

        return View(vm);
    }

    // ============================================================
    // POST: /Admin/Users/Verify/{id}
    // ============================================================
    [HttpPost("{id:guid}")]
    [Authorize(Roles = Constants.Roles.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Verify(Guid id, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound();

        user.IsVerified = !user.IsVerified;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("Admin {AdminId} toggled verify on user {UserId} → {State}",
            GetCurrentUserId(), id, user.IsVerified);

        TempData["Success"] = user.IsVerified
            ? "User marked as verified."
            : "Verification removed.";

        return RedirectToAction(nameof(Details), new { id });
    }

    // ============================================================
    // POST: /Admin/Users/Suspend/{id}
    // ============================================================
    [HttpPost("{id:guid}")]
    [Authorize(Roles = Constants.Roles.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Suspend(Guid id, string? reason, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound();

        user.IsSuspended = !user.IsSuspended;
        user.SuspensionReason = user.IsSuspended ? (reason ?? "Suspended by admin") : null;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        _logger.LogWarning("Admin {AdminId} toggled suspend on user {UserId} → {State} (reason: {Reason})",
            GetCurrentUserId(), id, user.IsSuspended, reason);

        TempData["Success"] = user.IsSuspended ? "User suspended." : "Suspension lifted.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // ============================================================
    // POST: /Admin/Users/SoftDelete/{id}
    // ============================================================
    [HttpPost("{id:guid}")]
    [Authorize(Roles = Constants.Roles.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SoftDelete(Guid id, CancellationToken ct)
    {
        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == id, ct);

        if (user == null) return NotFound();

        if (user.IsDeleted)
        {
            user.IsDeleted = false;
            user.DeletedAt = null;
            TempData["Success"] = "User restored.";
        }
        else
        {
            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;
            TempData["Success"] = "User soft-deleted.";
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        _logger.LogWarning("Admin {AdminId} toggled soft-delete on user {UserId} → {State}",
            GetCurrentUserId(), id, user.IsDeleted);

        return RedirectToAction(nameof(Details), new { id });
    }

    // ============================================================
    // POST: /Admin/Users/Unlock/{id}
    // ============================================================
    [HttpPost("{id:guid}")]
    [Authorize(Roles = Constants.Roles.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(Guid id, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound();

        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);

        TempData["Success"] = "User unlocked.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // ============================================================
    // POST: /Admin/Users/AddRole
    // ============================================================
    [HttpPost]
    [Authorize(Roles = Constants.Roles.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddRole(Guid id, string role, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound();

        if (!await _userManager.IsInRoleAsync(user, role))
            await _userManager.AddToRoleAsync(user, role);

        TempData["Success"] = $"Role '{role}' added.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // ============================================================
    // POST: /Admin/Users/RemoveRole
    // ============================================================
    [HttpPost]
    [Authorize(Roles = Constants.Roles.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveRole(Guid id, string role, CancellationToken ct)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound();

        if (await _userManager.IsInRoleAsync(user, role))
            await _userManager.RemoveFromRoleAsync(user, role);

        TempData["Success"] = $"Role '{role}' removed.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private Guid GetCurrentUserId()
        => Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
}

// ============================================================
// ViewModels
// ============================================================
public class AdminUsersIndexViewModel
{
    public List<AdminUserRowVm> Users { get; set; } = new();
    public string? SearchQuery { get; set; }
    public string? Filter { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public AdminUserStatsVm Stats { get; set; } = new();
}

public class AdminUserStatsVm
{
    public int Total { get; set; }
    public int Verified { get; set; }
    public int Suspended { get; set; }
    public int Private { get; set; }
    public int Deleted { get; set; }
}

public class AdminUserRowVm
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public bool IsVerified { get; set; }
    public bool IsPrivate { get; set; }
    public bool IsSuspended { get; set; }
    public bool IsDeleted { get; set; }
    public int FollowersCount { get; set; }
    public int FollowingCount { get; set; }
    public int PostsCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class AdminUserDetailsViewModel
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public string? CoverUrl { get; set; }
    public string? Location { get; set; }
    public string? Website { get; set; }
    public bool IsVerified { get; set; }
    public bool IsPrivate { get; set; }
    public bool IsSuspended { get; set; }
    public bool IsDeleted { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public bool LockoutEnabled { get; set; }
    public int AccessFailedCount { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public int FollowersCount { get; set; }
    public int FollowingCount { get; set; }
    public int PostsCount { get; set; }
    public int LikesReceivedCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? LastLoginIp { get; set; }
    public List<string> Roles { get; set; } = new();
}