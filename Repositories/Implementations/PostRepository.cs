using HiveCore.Data;
using HiveCore.Models.Entities;
using HiveCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HiveCore.Repositories.Implementations;

public class PostRepository : GenericRepository<Post>, IPostRepository
{
    public PostRepository(HiveCoreDbContext context) : base(context) { }

    // ============================================================
    // Detail queries
    // ============================================================
    public async Task<Post?> GetWithDetailsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Posts
            .Include(p => p.Author)
            .Include(p => p.Media)
            .Include(p => p.ParentPost)
                .ThenInclude(pp => pp!.Author)
            .Include(p => p.ParentPost)
                .ThenInclude(pp => pp!.Media)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    // ============================================================
    // Feeds
    // ============================================================
    public async Task<(IReadOnlyList<Post> Items, int TotalCount)> GetHomeFeedAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 25 : pageSize;

        // Get list of users this user follows + self
        var followingIds = await _context.Follows
            .Where(f => f.FollowerId == userId && !f.IsPending)
            .Select(f => f.FollowingId)
            .ToListAsync(cancellationToken);

        followingIds.Add(userId);

        var baseQuery = _context.Posts
            .AsNoTracking()
            .Where(p => !p.IsDeleted
                && !p.IsHidden
                && p.Visibility != PostVisibility.Private
                && followingIds.Contains(p.AuthorId));

        var total = await baseQuery.CountAsync(cancellationToken);

        var items = await baseQuery
            .Include(p => p.Author)
            .Include(p => p.Media)
            .Include(p => p.ParentPost)
                .ThenInclude(pp => pp!.Author)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<(IReadOnlyList<Post> Items, int TotalCount)> GetLatestFeedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 25 : pageSize;

        var baseQuery = _context.Posts
            .AsNoTracking()
            .Where(p => !p.IsDeleted
                && !p.IsHidden
                && p.Visibility == PostVisibility.Public);

        var total = await baseQuery.CountAsync(cancellationToken);

        var items = await baseQuery
            .Include(p => p.Author)
            .Include(p => p.Media)
            .Include(p => p.ParentPost)
                .ThenInclude(pp => pp!.Author)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<(IReadOnlyList<Post> Items, int TotalCount)> GetUserPostsAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 25 : pageSize;

        var baseQuery = _context.Posts
            .AsNoTracking()
            .Where(p => p.AuthorId == userId
                && !p.IsDeleted
                && !p.IsHidden);

        var total = await baseQuery.CountAsync(cancellationToken);

        var items = await baseQuery
            .Include(p => p.Author)
            .Include(p => p.Media)
            .Include(p => p.ParentPost)
                .ThenInclude(pp => pp!.Author)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<(IReadOnlyList<Post> Items, int TotalCount)> GetUserMediaPostsAsync(
    Guid userId,
    int page,
    int pageSize,
    CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 25 : pageSize;

        var baseQuery = _context.Posts
            .AsNoTracking()
            .Where(p => p.AuthorId == userId
                     && !p.IsDeleted
                     && !p.IsHidden
                     && p.Media.Any(m => !m.IsDeleted));

        var total = await baseQuery.CountAsync(cancellationToken);

        var items = await baseQuery
            .Include(p => p.Author)
            .Include(p => p.Media)
            .Include(p => p.ParentPost)
                .ThenInclude(pp => pp!.Author)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    // ============================================================
    // FIXED: GetLikedPostsAsync
    // ============================================================
    // Previously: query was `_context.Likes.Select(l => l.Post!)` then Include
    // was applied on the projection → EF Core error.
    //
    // Now: we Include Post on the Bookmark/Like root, then project in memory.
    // ============================================================
    public async Task<(IReadOnlyList<Post> Items, int TotalCount)> GetLikedPostsAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 25 : pageSize;

        // Count base query (Likes belonging to user, on existing posts)
        var baseQuery = _context.Likes
            .AsNoTracking()
            .Where(l => l.UserId == userId
                     && l.PostId != null
                     && !l.Post!.IsDeleted);

        var total = await baseQuery.CountAsync(cancellationToken);

        // Load Likes + Post + Post.Author + Post.Media + Post.ParentPost.Author
        var likes = await baseQuery
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(l => l.Post!).ThenInclude(p => p.Author)
            .Include(l => l.Post!).ThenInclude(p => p.Media)
            .Include(l => l.Post!).ThenInclude(p => p.ParentPost).ThenInclude(pp => pp!.Author)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        // Project to Post entities
        var items = likes
            .Where(l => l.Post != null)
            .Select(l => l.Post!)
            .ToList();

        return (items, total);
    }

    // ============================================================
    // FIXED: GetBookmarkedPostsAsync
    // ============================================================
    public async Task<(IReadOnlyList<Post> Items, int TotalCount)> GetBookmarkedPostsAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 25 : pageSize;

        // Count base query (Bookmarks belonging to user, on existing posts)
        var baseQuery = _context.Bookmarks
            .AsNoTracking()
            .Where(b => b.UserId == userId
                     && !b.Post.IsDeleted);

        var total = await baseQuery.CountAsync(cancellationToken);

        // Load Bookmarks + Post + Post.Author + Post.Media + Post.ParentPost.Author
        var bookmarks = await baseQuery
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(b => b.Post).ThenInclude(p => p.Author)
            .Include(b => b.Post).ThenInclude(p => p.Media)
            .Include(b => b.Post).ThenInclude(p => p.ParentPost).ThenInclude(pp => pp!.Author)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        // Project to Post entities (preserving bookmark order)
        var items = bookmarks
            .Select(b => b.Post)
            .ToList();

        return (items, total);
    }

    // ============================================================
    // Trending / Replies / Search
    // ============================================================
    public async Task<IReadOnlyList<Post>> GetTrendingAsync(
        int count,
        int withinHours,
        CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow.AddHours(-Math.Abs(withinHours));

        return await _context.Posts
            .AsNoTracking()
            .Where(p => !p.IsDeleted
                && !p.IsHidden
                && p.Visibility == PostVisibility.Public
                && p.CreatedAt >= since)
            .Include(p => p.Author)
            .Include(p => p.Media)
            .OrderByDescending(p => (p.LikesCount * 2) + (p.CommentsCount * 3) + p.RepostsCount)
            .ThenByDescending(p => p.CreatedAt)
            .Take(count)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<Post> Items, int TotalCount)> GetRepliesAsync(
        Guid parentPostId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 25 : pageSize;

        var baseQuery = _context.Posts
            .AsNoTracking()
            .Where(p => p.ParentPostId == parentPostId
                && !p.IsDeleted
                && !p.IsHidden);

        var total = await baseQuery.CountAsync(cancellationToken);

        var items = await baseQuery
            .Include(p => p.Author)
            .Include(p => p.Media)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<(IReadOnlyList<Post> Items, int TotalCount)> SearchAsync(
        string query,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return (Array.Empty<Post>(), 0);

        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 25 : pageSize;

        var term = query.Trim();

        var baseQuery = _context.Posts
            .AsNoTracking()
            .Where(p => !p.IsDeleted
                && !p.IsHidden
                && p.Visibility == PostVisibility.Public
                && EF.Functions.Like(p.Content, $"%{term}%"));

        var total = await baseQuery.CountAsync(cancellationToken);

        var items = await baseQuery
            .Include(p => p.Author)
            .Include(p => p.Media)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}