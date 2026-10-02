using HiveCore.Models.DTOs;
using HiveCore.Models.Entities;
using HiveCore.Models.ViewModels;

namespace HiveCore.Services.Interfaces;

public interface IPostService
{
    Task<PostDto?> GetByIdAsync(Guid id, Guid? currentUserId = null, CancellationToken ct = default);

    Task<PostDetailsViewModel?> GetDetailsAsync(
        Guid id, Guid? currentUserId = null, CancellationToken ct = default);

    Task<(IReadOnlyList<PostDto> Posts, int Total)> GetUserPostsAsync(
        Guid userId, Guid? currentUserId, int page, int pageSize, CancellationToken ct = default);

    Task<(IReadOnlyList<PostDto> Posts, int Total)> GetUserMediaPostsAsync(
        Guid userId, Guid? currentUserId, int page, int pageSize, CancellationToken ct = default);

    Task<(IReadOnlyList<PostDto> Posts, int Total)> GetLikedPostsAsync(
        Guid userId, Guid? currentUserId, int page, int pageSize, CancellationToken ct = default);

    Task<(IReadOnlyList<PostDto> Posts, int Total)> GetBookmarkedPostsAsync(
        Guid userId, Guid? currentUserId, int page, int pageSize, CancellationToken ct = default);

    Task<(IReadOnlyList<PostDto> Posts, int Total)> GetRepliesAsync(
        Guid parentPostId, Guid? currentUserId, int page, int pageSize, CancellationToken ct = default);

    Task<(IReadOnlyList<PostDto> Posts, int Total)> SearchAsync(
        string query, Guid? currentUserId, int page, int pageSize, CancellationToken ct = default);

    Task<IReadOnlyList<PostDto>> GetTrendingAsync(
        int count, int withinHours, Guid? currentUserId = null, CancellationToken ct = default);

    /// <summary>Bulk-map loaded Post entities to PostDto with viewer flags.</summary>
    Task<IReadOnlyList<PostDto>> MapManyAsync(
        IReadOnlyList<Post> posts, Guid? currentUserId, CancellationToken ct = default);

    Task<PostDto> CreateAsync(
        PostViewModel model, Guid authorId, CancellationToken ct = default);

    /// <summary>
    /// Updates a post's content and media. Removes media whose Ids are listed
    /// in <see cref="PostViewModel.MediaIdsToDelete"/>, then uploads new files
    /// from <see cref="PostViewModel.MediaFiles"/>.
    /// </summary>
    Task<PostDto?> UpdateAsync(
        Guid postId, Guid userId, PostViewModel model, CancellationToken ct = default);

    Task<bool> DeleteAsync(Guid postId, Guid userId, CancellationToken ct = default);

    Task<PostDto?> RepostAsync(
        Guid parentPostId, Guid userId, string? additionalComment, CancellationToken ct = default);

    Task<PostDto?> QuoteAsync(
        Guid parentPostId, Guid userId, string comment, CancellationToken ct = default);

    Task<bool> LikeAsync(Guid postId, Guid userId, CancellationToken ct = default);

    Task<bool> UnlikeAsync(Guid postId, Guid userId, CancellationToken ct = default);

    Task<bool> BookmarkAsync(Guid postId, Guid userId, CancellationToken ct = default);

    Task<bool> UnbookmarkAsync(Guid postId, Guid userId, CancellationToken ct = default);

    Task<bool> IncrementViewAsync(Guid postId, CancellationToken ct = default);
}