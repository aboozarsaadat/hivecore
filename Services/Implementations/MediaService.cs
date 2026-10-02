using HiveCore.Models.DTOs;
using HiveCore.Models.Entities;
using HiveCore.Repositories.Interfaces;
using HiveCore.Services.Interfaces;
using HiveCore.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace HiveCore.Services.Implementations;

public class MediaService : IMediaService
{
    private readonly IGenericRepository<Media> _mediaRepo;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<MediaService> _logger;

    public MediaService(
        IGenericRepository<Media> mediaRepo,
        IWebHostEnvironment env,
        ILogger<MediaService> logger)
    {
        _mediaRepo = mediaRepo;
        _env = env;
        _logger = logger;
    }

    public async Task<MediaDto> UploadAsync(
        IFormFile file,
        Guid uploadedById,
        MediaUploadKind kind,
        Guid? postId = null,
        string? altText = null,
        CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
            throw new InvalidOperationException("Uploaded file is empty.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

        var mediaType = kind switch
        {
            MediaUploadKind.PostVideo => MediaType.Video,
            MediaUploadKind.Music => MediaType.Audio,
            _ => MediaType.Image
        };

        if (mediaType == MediaType.Image && !IsValidImage(file))
            throw new InvalidOperationException($"Unsupported image type: {ext}");

        if (mediaType == MediaType.Video && !IsValidVideo(file))
            throw new InvalidOperationException($"Unsupported video type: {ext}");

        var maxBytes = kind switch
        {
            MediaUploadKind.Avatar => Constants.Media.MaxAvatarSizeBytes,
            MediaUploadKind.Cover => Constants.Media.MaxCoverSizeBytes,
            MediaUploadKind.PostVideo => Constants.Media.MaxPostVideoSizeBytes,
            _ => Constants.Media.MaxPostImageSizeBytes
        };

        if (!IsValidSize(file, maxBytes))
            throw new InvalidOperationException(
                $"File exceeds maximum allowed size of {maxBytes / (1024 * 1024)} MB.");

        // Determine target folder
        var folder = kind switch
        {
            MediaUploadKind.Avatar => Constants.UploadPaths.Avatars,
            MediaUploadKind.Cover => Constants.UploadPaths.Covers,
            MediaUploadKind.Music => Constants.UploadPaths.Music,
            _ => Constants.UploadPaths.Posts
        };

        var subDir = Path.Combine(DateTime.UtcNow.ToString("yyyy"), DateTime.UtcNow.ToString("MM"));
        var physicalDir = Path.Combine(_env.WebRootPath, "uploads", folder, subDir);
        Directory.CreateDirectory(physicalDir);

        var uniqueName = $"{Guid.NewGuid():N}{ext}";
        var physicalPath = Path.Combine(physicalDir, uniqueName);

        await using (var stream = new FileStream(physicalPath, FileMode.CreateNew))
        {
            await file.CopyToAsync(stream, ct);
        }

        var relativeUrl = $"/uploads/{folder}/{subDir.Replace(Path.DirectorySeparatorChar, '/')}/{uniqueName}";

        var media = new Media
        {
            Id = Guid.NewGuid(),
            UploadedById = uploadedById,
            PostId = postId,
            Url = relativeUrl,
            ThumbnailUrl = null,
            FileName = uniqueName,
            OriginalFileName = file.FileName,
            ContentType = file.ContentType,
            Extension = ext,
            SizeBytes = file.Length,
            Type = mediaType,
            AltText = altText,
            CreatedAt = DateTime.UtcNow
        };

        await _mediaRepo.AddAsync(media, ct);
        await _mediaRepo.SaveChangesAsync(ct);

        _logger.LogInformation("Media {Id} uploaded at {Url} ({Bytes} bytes)",
            media.Id, media.Url, media.SizeBytes);

        return MapToDto(media);
    }

    public async Task<IReadOnlyList<MediaDto>> UploadManyAsync(
        IEnumerable<IFormFile> files,
        Guid uploadedById,
        MediaUploadKind kind,
        Guid? postId = null,
        CancellationToken ct = default)
    {
        var result = new List<MediaDto>();

        foreach (var file in files.Take(Constants.Limits.MaxMediaPerPost))
        {
            result.Add(await UploadAsync(file, uploadedById, kind, postId, ct: ct));
        }

        return result;
    }

    public async Task<bool> DeleteAsync(Guid mediaId, Guid userId, CancellationToken ct = default)
    {
        var media = await _mediaRepo.GetByIdAsync(mediaId, ct);
        if (media == null) return false;
        if (media.UploadedById != userId) return false;

        media.IsDeleted = true;
        _mediaRepo.Update(media);
        await _mediaRepo.SaveChangesAsync(ct);

        // Try physical delete (best effort)
        try
        {
            var relative = media.Url.TrimStart('/');
            var physical = Path.Combine(_env.WebRootPath, relative);
            if (File.Exists(physical)) File.Delete(physical);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete physical media file {Url}", media.Url);
        }

        return true;
    }

    public async Task<string> SaveAvatarAsync(Guid userId, IFormFile file, CancellationToken ct = default)
    {
        var dto = await UploadAsync(file, userId, MediaUploadKind.Avatar, ct: ct);
        return dto.Url;
    }

    public async Task<string> SaveCoverAsync(Guid userId, IFormFile file, CancellationToken ct = default)
    {
        var dto = await UploadAsync(file, userId, MediaUploadKind.Cover, ct: ct);
        return dto.Url;
    }

    public bool IsValidImage(IFormFile file)
    {
        if (file == null) return false;
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        return Constants.Media.AllowedImageExtensions.Contains(ext)
            && Constants.Media.AllowedImageContentTypes.Contains(file.ContentType);
    }

    public bool IsValidVideo(IFormFile file)
    {
        if (file == null) return false;
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        return Constants.Media.AllowedVideoExtensions.Contains(ext)
            && Constants.Media.AllowedVideoContentTypes.Contains(file.ContentType);
    }

    public bool IsValidSize(IFormFile file, long maxBytes)
        => file != null && file.Length > 0 && file.Length <= maxBytes;

    private static MediaDto MapToDto(Media m) => new()
    {
        Id = m.Id,
        Url = m.Url,
        ThumbnailUrl = m.ThumbnailUrl,
        ContentType = m.ContentType,
        Type = m.Type,
        Width = m.Width,
        Height = m.Height,
        Duration = m.Duration,
        AltText = m.AltText
    };
}