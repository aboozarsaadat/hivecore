using HiveCore.Data;
using HiveCore.Models.Entities;
using HiveCore.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HiveCore.Repositories.Implementations;

public class FollowRepository : GenericRepository<Follow>, IFollowRepository
{
    public FollowRepository(HiveCoreDbContext context) : base(context) { }

    public async Task<Follow?> GetFollowAsync(
        Guid followerId,
        Guid followingId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Follows
            .FirstOrDefaultAsync(
                f => f.FollowerId == followerId && f.FollowingId == followingId,
                cancellationToken);
    }

    public async Task<bool> IsFollowingAsync(
        Guid followerId,
        Guid followingId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Follows
            .AnyAsync(
                f => f.FollowerId == followerId
                    && f.FollowingId == followingId
                    && !f.IsPending,
                cancellationToken);
    }

    public async Task<bool> HasPendingRequestAsync(
        Guid followerId,
        Guid followingId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Follows
            .AnyAsync(
                f => f.FollowerId == followerId
                    && f.FollowingId == followingId
                    && f.IsPending,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Follow>> GetPendingRequestsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Follows
            .AsNoTracking()
            .Where(f => f.FollowingId == userId && f.IsPending)
            .Include(f => f.Follower)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetFollowersCountAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Follows
            .CountAsync(
                f => f.FollowingId == userId && !f.IsPending,
                cancellationToken);
    }

    public async Task<int> GetFollowingCountAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Follows
            .CountAsync(
                f => f.FollowerId == userId && !f.IsPending,
                cancellationToken);
    }
}