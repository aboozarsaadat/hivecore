using HiveCore.Models.Entities;

namespace HiveCore.Repositories.Interfaces;

public interface IPostRepository : IGenericRepository<Post>
{
    Task<Post?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Post> Items, int TotalCount)> GetHomeFeedAsync(
        Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Post> Items, int TotalCount)> GetLatestFeedAsync(
        int page, int pageSize, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Post> Items, int TotalCount)> GetUserPostsAsync(
        Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Returns only posts that contain at least one non-deleted media item.</summary>
    Task<(IReadOnlyList<Post> Items, int TotalCount)> GetUserMediaPostsAsync(
        Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Post> Items, int TotalCount)> GetLikedPostsAsync(
        Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Post> Items, int TotalCount)> GetBookmarkedPostsAsync(
        Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Post>> GetTrendingAsync(
        int count, int withinHours, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Post> Items, int TotalCount)> GetRepliesAsync(
        Guid parentPostId, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Post> Items, int TotalCount)> SearchAsync(
        string query, int page, int pageSize, CancellationToken cancellationToken = default);
}