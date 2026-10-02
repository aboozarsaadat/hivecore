using HiveCore.Models.DTOs;
using HiveCore.Models.Entities;
using HiveCore.Utilities;
using Microsoft.AspNetCore.Http;

namespace HiveCore.Services.Interfaces;

public interface IMediaService
{
    Task<MediaDto> UploadAsync(
        IFormFile file,
        Guid uploadedById,
        MediaUploadKind kind,
        Guid? postId = null,
        string? altText = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<MediaDto>> UploadManyAsync(
        IEnumerable<IFormFile> files,
        Guid uploadedById,
        MediaUploadKind kind,
        Guid? postId = null,
        CancellationToken ct = default);

    Task<bool> DeleteAsync(Guid mediaId, Guid userId, CancellationToken ct = default);

    Task<string> SaveAvatarAsync(Guid userId, IFormFile file, CancellationToken ct = default);

    Task<string> SaveCoverAsync(Guid userId, IFormFile file, CancellationToken ct = default);

    bool IsValidImage(IFormFile file);

    bool IsValidVideo(IFormFile file);

    bool IsValidSize(IFormFile file, long maxBytes);
}