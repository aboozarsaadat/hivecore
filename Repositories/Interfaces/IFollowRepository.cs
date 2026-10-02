using HiveCore.Models.Entities;

namespace HiveCore.Repositories.Interfaces;

public interface IFollowRepository : IGenericRepository<Follow>
{
    Task<Follow?> GetFollowAsync(
        Guid followerId,
        Guid followingId,
        CancellationToken cancellationToken = default);

    Task<bool> IsFollowingAsync(
        Guid followerId,
        Guid followingId,
        CancellationToken cancellationToken = default);

    Task<bool> HasPendingRequestAsync(
        Guid followerId,
        Guid followingId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Follow>> GetPendingRequestsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<int> GetFollowersCountAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<int> GetFollowingCountAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}