using HiveCore.Data;
using HiveCore.Extensions;
using HiveCore.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HiveCore.ViewComponents;

/// <summary>
/// Renders the "Trending hashtags" widget in the right rail.
/// Extracts hashtags from posts in the last 7 days.
/// </summary>
public class TrendingHashtagsViewComponent : ViewComponent
{
    private readonly HiveCoreDbContext _context;

    public TrendingHashtagsViewComponent(HiveCoreDbContext context)
    {
        _context = context;
    }

    public async Task<IViewComponentResult> InvokeAsync(
        int count = 10,
        int withinDays = 7,
        CancellationToken ct = default)
    {
        count = Math.Clamp(count, 1, 30);
        withinDays = Math.Clamp(withinDays, 1, 30);

        var since = DateTime.UtcNow.AddDays(-withinDays);

        // Fetch only content of recent posts (lightweight projection)
        var contents = await _context.Posts
            .AsNoTracking()
            .Where(p => !p.IsDeleted
                && !p.IsHidden
                && p.Visibility == HiveCore.Models.Entities.PostVisibility.Public
                && p.CreatedAt >= since)
            .OrderByDescending(p => p.CreatedAt)
            .Take(1000)
            .Select(p => p.Content)
            .ToListAsync(ct);

        var trending = contents
            .SelectMany(c => c.ExtractHashtags())
            .GroupBy(tag => tag)
            .Select(g => new TrendingHashtagViewModel
            {
                Tag = g.Key,
                PostsCount = g.Count()
            })
            .OrderByDescending(x => x.PostsCount)
            .Take(count)
            .ToList();

        return View(trending);
    }
}