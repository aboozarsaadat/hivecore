using HiveCore.Data;
using HiveCore.Models.Entities;
using HiveCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HiveCore.Repositories.Implementations;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(HiveCoreDbContext context) : base(context) { }

    public async Task<User?> GetByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username)) return null;

        var normalized = username.Trim().ToLowerInvariant();

        return await _context.Users
            .FirstOrDefaultAsync(
                u => u.NormalizedUserName == normalized.ToUpperInvariant(),
                cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;

        var normalized = email.Trim().ToUpperInvariant();

        return await _context.Users
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalized, cancellationToken);
    }

    public async Task<User?> GetWithProfileAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.ProfileTheme)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
    }

    public async Task<User?> GetWithThemeAndMusicAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.ProfileTheme)
            .Include(u => u.MusicTracks.Where(m => !m.IsDeleted)
                .OrderBy(m => m.DisplayOrder))
            .AsSplitQuery()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<User>> SearchAsync(
        string query,
        int count,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<User>();

        var term = query.Trim().ToLowerInvariant();
        count = count < 1 ? 20 : count;

        return await _context.Users
            .AsNoTracking()
            .Where(u => !u.IsDeleted
                && !u.IsSuspended
                && (EF.Functions.Like(u.UserName!, $"%{term}%")
                    || EF.Functions.Like(u.DisplayName.ToLower(), $"%{term}%")))
            .OrderByDescending(u => u.FollowersCount)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetSuggestedAsync(
        Guid currentUserId,
        int count,
        CancellationToken cancellationToken = default)
    {
        count = count < 1 ? 10 : count;

        // Exclude self and users the current user already follows
        var followingIds = await _context.Follows
            .Where(f => f.FollowerId == currentUserId)
            .Select(f => f.FollowingId)
            .ToListAsync(cancellationToken);

        followingIds.Add(currentUserId);

        return await _context.Users
            .AsNoTracking()
            .Where(u => !u.IsDeleted
                && !u.IsSuspended
                && !followingIds.Contains(u.Id))
            .OrderByDescending(u => u.FollowersCount)
            .ThenByDescending(u => u.PostsCount)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetFollowersAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Follows
            .AsNoTracking()
            .Where(f => f.FollowingId == userId && !f.IsPending)
            .Select(f => f.Follower)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetFollowingAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Follows
            .AsNoTracking()
            .Where(f => f.FollowerId == userId && !f.IsPending)
            .Select(f => f.Following)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsUsernameTakenAsync(
        string username,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username)) return false;

        var normalized = username.Trim().ToUpperInvariant();

        return await _context.Users
            .AnyAsync(u => u.NormalizedUserName == normalized, cancellationToken);
    }

    public async Task<bool> IsEmailTakenAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;

        var normalized = email.Trim().ToUpperInvariant();

        return await _context.Users
            .AnyAsync(u => u.NormalizedEmail == normalized, cancellationToken);
    }
}