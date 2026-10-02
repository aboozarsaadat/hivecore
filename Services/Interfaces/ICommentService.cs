using HiveCore.Models.DTOs;
using HiveCore.Models.ViewModels;

namespace HiveCore.Services.Interfaces;

public interface ICommentService
{
    Task<CommentDto?> GetByIdAsync(Guid id, Guid? currentUserId = null, CancellationToken ct = default);

    Task<(IReadOnlyList<CommentDto> Comments, int Total)> GetForPostAsync(
        Guid postId, Guid? currentUserId, int page, int pageSize, CancellationToken ct = default);

    Task<CommentDto> CreateAsync(
        CommentViewModel model, Guid authorId, CancellationToken ct = default);

    Task<CommentDto?> UpdateAsync(
        Guid commentId, Guid userId, string newContent, CancellationToken ct = default);

    Task<bool> DeleteAsync(Guid commentId, Guid userId, CancellationToken ct = default);

    Task<bool> LikeAsync(Guid commentId, Guid userId, CancellationToken ct = default);

    Task<bool> UnlikeAsync(Guid commentId, Guid userId, CancellationToken ct = default);
}