using HiveCore.Extensions;
using HiveCore.Hubs;
using HiveCore.Models.DTOs;
using HiveCore.Models.Entities;
using HiveCore.Models.ViewModels;
using HiveCore.Repositories.Interfaces;
using HiveCore.Services.Interfaces;
using HiveCore.Utilities;
using Microsoft.Extensions.Logging;

namespace HiveCore.Services.Implementations;

/// <summary>
/// Application service for posts, including creation, editing, deletion,
/// interactions (like/bookmark/repost), feeds, and real-time notifications.
/// </summary>
public class PostService : IPostService
{
    private readonly IPostRepository _postRepo;
    private readonly IUserRepository _userRepo;
    private readonly IGenericRepository<Like> _likeRepo;
    private readonly IGenericRepository<Bookmark> _bookmarkRepo;
    private readonly IMediaService _mediaService;
    private readonly INotificationService _notificationService;
    private readonly IRealTimeNotifier _realtime;
    private readonly ILogger<PostService> _logger;

    public PostService(
        IPostRepository postRepo,
        IUserRepository userRepo,
        IGenericRepository<Like> likeRepo,
        IGenericRepository<Bookmark> bookmarkRepo,
        IMediaService mediaService,
        INotificationService notificationService,
        IRealTimeNotifier realtime,
        ILogger<PostService> logger)
    {
        _postRepo = postRepo;
        _userRepo = userRepo;
        _likeRepo = likeRepo;
        _bookmarkRepo = bookmarkRepo;
        _mediaService = mediaService;
        _notificationService = notificationService;
        _realtime = realtime;
        _logger = logger;
    }

    // ============================================================
    // Read
    // ============================================================
    public async Task<PostDto?> GetByIdAsync(
        Guid id, Guid? currentUserId = null, CancellationToken ct = default)
    {
        var post = await _postRepo.GetWithDetailsAsync(id, ct);
        if (post == null) return null;

        return await MapSingleAsync(post, currentUserId, ct);
    }

    public async Task<PostDetailsViewModel?> GetDetailsAsync(
        Guid id, Guid? currentUserId = null, CancellationToken ct = default)
    {
        var post = await _postRepo.GetWithDetailsAsync(id, ct);
        if (post == null) return null;

        var dto = await MapSingleAsync(post, currentUserId, ct);

        return new PostDetailsViewModel
        {
            Post = dto,
            IsLikedByCurrentUser = dto.IsLikedByCurrentUser,
            IsBookmarkedByCurrentUser = dto.IsBookmarkedByCurrentUser,
            CanEdit = currentUserId.HasValue && post.AuthorId == currentUserId.Value,
            CanDelete = currentUserId.HasValue && post.AuthorId == currentUserId.Value,
            NewComment = new CommentViewModel { PostId = post.Id }
        };
    }

    public async Task<(IReadOnlyList<PostDto> Posts, int Total)> GetUserPostsAsync(
        Guid userId, Guid? currentUserId, int page, int pageSize, CancellationToken ct = default)
    {
        var (items, total) = await _postRepo.GetUserPostsAsync(userId, page, pageSize, ct);
        return (await MapManyAsync(items, currentUserId, ct), total);
    }

    public async Task<(IReadOnlyList<PostDto> Posts, int Total)> GetUserMediaPostsAsync(
        Guid userId, Guid? currentUserId, int page, int pageSize, CancellationToken ct = default)
    {
        var (items, total) = await _postRepo.GetUserMediaPostsAsync(userId, page, pageSize, ct);
        return (await MapManyAsync(items, currentUserId, ct), total);
    }

    public async Task<(IReadOnlyList<PostDto> Posts, int Total)> GetLikedPostsAsync(
        Guid userId, Guid? currentUserId, int page, int pageSize, CancellationToken ct = default)
    {
        var (items, total) = await _postRepo.GetLikedPostsAsync(userId, page, pageSize, ct);
        return (await MapManyAsync(items, currentUserId, ct), total);
    }

    public async Task<(IReadOnlyList<PostDto> Posts, int Total)> GetBookmarkedPostsAsync(
        Guid userId, Guid? currentUserId, int page, int pageSize, CancellationToken ct = default)
    {
        var (items, total) = await _postRepo.GetBookmarkedPostsAsync(userId, page, pageSize, ct);
        return (await MapManyAsync(items, currentUserId, ct), total);
    }

    public async Task<(IReadOnlyList<PostDto> Posts, int Total)> GetRepliesAsync(
        Guid parentPostId, Guid? currentUserId, int page, int pageSize, CancellationToken ct = default)
    {
        var (items, total) = await _postRepo.GetRepliesAsync(parentPostId, page, pageSize, ct);
        return (await MapManyAsync(items, currentUserId, ct), total);
    }

    public async Task<(IReadOnlyList<PostDto> Posts, int Total)> SearchAsync(
        string query, Guid? currentUserId, int page, int pageSize, CancellationToken ct = default)
    {
        var (items, total) = await _postRepo.SearchAsync(query, page, pageSize, ct);
        return (await MapManyAsync(items, currentUserId, ct), total);
    }

    public async Task<IReadOnlyList<PostDto>> GetTrendingAsync(
        int count, int withinHours, Guid? currentUserId = null, CancellationToken ct = default)
    {
        var items = await _postRepo.GetTrendingAsync(count, withinHours, ct);
        return await MapManyAsync(items, currentUserId, ct);
    }

    // ============================================================
    // Create
    // ============================================================
    public async Task<PostDto> CreateAsync(
        PostViewModel model, Guid authorId, CancellationToken ct = default)
    {
        var hasContent = !string.IsNullOrWhiteSpace(model.Content);
        var hasMedia = model.MediaFiles?.Any(f => f.Length > 0) == true;

        if (!hasContent && !hasMedia)
            throw new InvalidOperationException("Post must contain text or media.");

        if (hasContent && model.Content.Length > Constants.Limits.MaxPostLength)
            throw new InvalidOperationException(
                $"Post content cannot exceed {Constants.Limits.MaxPostLength} characters.");

        if (hasContent)
        {
            var hashtags = model.Content.ExtractHashtags().Distinct().Count();
            if (hashtags > Constants.Limits.MaxHashtagsPerPost)
                throw new InvalidOperationException(
                    $"A post can contain at most {Constants.Limits.MaxHashtagsPerPost} hashtags.");

            var mentions = model.Content.ExtractMentions().Distinct().Count();
            if (mentions > Constants.Limits.MaxMentionsPerPost)
                throw new InvalidOperationException(
                    $"A post can contain at most {Constants.Limits.MaxMentionsPerPost} mentions.");
        }

        Post? parent = null;
        if (model.ParentPostId.HasValue)
        {
            parent = await _postRepo.GetByIdAsync(model.ParentPostId.Value, ct);
            if (parent == null || parent.IsDeleted)
                throw new KeyNotFoundException("Parent post not found.");
        }

        var post = new Post
        {
            Id = Guid.NewGuid(),
            Content = (model.Content ?? string.Empty).Trim(),
            AuthorId = authorId,
            ParentPostId = model.ParentPostId,
            IsQuote = model.IsQuote,
            IsRepost = model.ParentPostId.HasValue && !model.IsQuote,
            Visibility = model.Visibility,
            IsSensitive = model.IsSensitive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _postRepo.AddAsync(post, ct);

        if (hasMedia)
        {
            await _mediaService.UploadManyAsync(
                model.MediaFiles!.Where(f => f.Length > 0),
                authorId,
                MediaUploadKind.PostImage,
                post.Id,
                ct);
        }

        var author = await _userRepo.GetByIdAsync(authorId, ct);
        if (author != null)
        {
            author.PostsCount++;
            author.UpdatedAt = DateTime.UtcNow;
            _userRepo.Update(author);
        }

        if (parent != null)
        {
            if (post.IsRepost) parent.RepostsCount++;
            else parent.CommentsCount++;

            parent.UpdatedAt = DateTime.UtcNow;
            _postRepo.Update(parent);
        }

        await _postRepo.SaveChangesAsync(ct);

        // Real-time + notifications (best-effort)
        try
        {
            var fresh = await _postRepo.GetWithDetailsAsync(post.Id, ct);
            if (fresh != null)
            {
                var dto = await MapSingleAsync(fresh, authorId, ct);
                await _realtime.NotifyPostCreatedAsync(authorId, dto, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Real-time NotifyPostCreated failed for post {PostId}", post.Id);
        }

        if (parent != null && parent.AuthorId != authorId)
        {
            try
            {
                if (post.IsRepost)
                    await _notificationService.NotifyRepostAsync(parent.AuthorId, authorId, post.Id, ct);
                else
                    await _notificationService.NotifyReplyAsync(
                        parent.AuthorId, authorId, parent.Id, post.Id, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to notify parent post author {AuthorId}", parent.AuthorId);
            }
        }

        if (hasContent)
        {
            var mentionedUsers = new HashSet<Guid>();
            foreach (var mention in post.Content.ExtractMentions().Distinct())
            {
                try
                {
                    var mentioned = await _userRepo.GetByUsernameAsync(mention, ct);
                    if (mentioned != null
                        && mentioned.Id != authorId
                        && mentionedUsers.Add(mentioned.Id))
                    {
                        await _notificationService.NotifyMentionAsync(
                            mentioned.Id, authorId, post.Id, ct);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to notify mention @{Mention}", mention);
                }
            }
        }

        _logger.LogInformation("Post {PostId} created by {AuthorId}", post.Id, authorId);

        var finalPost = await _postRepo.GetWithDetailsAsync(post.Id, ct);
        return await MapSingleAsync(finalPost!, authorId, ct);
    }

    // ============================================================
    // Update (content + media)
    // ============================================================
    public async Task<PostDto?> UpdateAsync(
        Guid postId, Guid userId, PostViewModel model, CancellationToken ct = default)
    {
        var newContent = (model.Content ?? string.Empty).Trim();

        var post = await _postRepo.GetWithDetailsAsync(postId, ct);
        if (post == null) return null;

        if (post.AuthorId != userId)
            throw new UnauthorizedAccessException("You can only edit your own posts.");

        if (newContent.Length > Constants.Limits.MaxPostLength)
            throw new InvalidOperationException(
                $"Content cannot exceed {Constants.Limits.MaxPostLength} characters.");

        // ============================================================
        // Compute remaining media count after deletion + uploads
        // ============================================================
        var currentMediaIds = post.Media?
            .Where(m => !m.IsDeleted)
            .Select(m => m.Id)
            .ToList() ?? new List<Guid>();

        var toDelete = model.MediaIdsToDelete?
            .Where(id => currentMediaIds.Contains(id))
            .Distinct()
            .ToList() ?? new List<Guid>();

        var remainingExisting = currentMediaIds.Count - toDelete.Count;
        var newFiles = model.MediaFiles?
            .Where(f => f != null && f.Length > 0)
            .ToList() ?? new List<IFormFile>();

        var totalAfterUpdate = remainingExisting + newFiles.Count;

        // ============================================================
        // Post must have either content or at least one media
        // ============================================================
        if (string.IsNullOrWhiteSpace(newContent) && totalAfterUpdate == 0)
            throw new InvalidOperationException("Post must contain text or media.");

        // Enforce max media per post
        if (totalAfterUpdate > Constants.Limits.MaxMediaPerPost)
            throw new InvalidOperationException(
                $"A post can have at most {Constants.Limits.MaxMediaPerPost} media items.");

        // ============================================================
        // Validate hashtags/mentions if content is present
        // ============================================================
        if (!string.IsNullOrWhiteSpace(newContent))
        {
            var hashtags = newContent.ExtractHashtags().Distinct().Count();
            if (hashtags > Constants.Limits.MaxHashtagsPerPost)
                throw new InvalidOperationException(
                    $"A post can contain at most {Constants.Limits.MaxHashtagsPerPost} hashtags.");

            var mentions = newContent.ExtractMentions().Distinct().Count();
            if (mentions > Constants.Limits.MaxMentionsPerPost)
                throw new InvalidOperationException(
                    $"A post can contain at most {Constants.Limits.MaxMentionsPerPost} mentions.");
        }

        // ============================================================
        // Apply content change
        // ============================================================
        post.Content = newContent;
        post.UpdatedAt = DateTime.UtcNow;
        _postRepo.Update(post);

        // ============================================================
        // Soft-delete marked media (own transaction inside MediaService)
        // ============================================================
        foreach (var mediaId in toDelete)
        {
            try
            {
                await _mediaService.DeleteAsync(mediaId, userId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete media {MediaId} from post {PostId}",
                    mediaId, postId);
            }
        }

        // ============================================================
        // Upload new media
        // ============================================================
        if (newFiles.Count > 0)
        {
            try
            {
                await _mediaService.UploadManyAsync(
                    newFiles,
                    userId,
                    MediaUploadKind.PostImage,
                    post.Id,
                    ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload media while editing post {PostId}", postId);
                throw new InvalidOperationException(
                    "Failed to upload media: " + ex.Message);
            }
        }

        await _postRepo.SaveChangesAsync(ct);

        // ============================================================
        // Reload with fresh media list and map to DTO
        // ============================================================
        var refreshed = await _postRepo.GetWithDetailsAsync(postId, ct);
        if (refreshed == null) return null;

        var dto = await MapSingleAsync(refreshed, userId, ct);

        // ============================================================
        // Real-time broadcast (best-effort)
        // ============================================================
        try
        {
            await _realtime.NotifyPostUpdatedAsync(post.Id, dto, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Real-time NotifyPostUpdated failed for {PostId}", post.Id);
        }

        _logger.LogInformation(
            "Post {PostId} edited by {UserId} (removed {Removed} media, added {Added})",
            postId, userId, toDelete.Count, newFiles.Count);

        return dto;
    }

    // ============================================================
    // Delete
    // ============================================================
    public async Task<bool> DeleteAsync(Guid postId, Guid userId, CancellationToken ct = default)
    {
        var post = await _postRepo.GetByIdAsync(postId, ct);
        if (post == null) return false;

        if (post.AuthorId != userId)
            throw new UnauthorizedAccessException("You can only delete your own posts.");

        post.IsDeleted = true;
        post.DeletedAt = DateTime.UtcNow;
        post.UpdatedAt = DateTime.UtcNow;
        _postRepo.Update(post);

        var author = await _userRepo.GetByIdAsync(userId, ct);
        if (author != null && author.PostsCount > 0)
        {
            author.PostsCount--;
            author.UpdatedAt = DateTime.UtcNow;
            _userRepo.Update(author);
        }

        if (post.ParentPostId.HasValue)
        {
            var parent = await _postRepo.GetByIdAsync(post.ParentPostId.Value, ct);
            if (parent != null)
            {
                if (post.IsRepost && parent.RepostsCount > 0) parent.RepostsCount--;
                else if (!post.IsRepost && parent.CommentsCount > 0) parent.CommentsCount--;
                _postRepo.Update(parent);
            }
        }

        await _postRepo.SaveChangesAsync(ct);

        try
        {
            await _realtime.NotifyPostDeletedAsync(postId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Real-time NotifyPostDeleted failed for {PostId}", postId);
        }

        _logger.LogInformation("Post {PostId} soft-deleted by {UserId}", postId, userId);
        return true;
    }

    // ============================================================
    // Repost / Quote
    // ============================================================
    public async Task<PostDto?> RepostAsync(
        Guid parentPostId, Guid userId, string? additionalComment, CancellationToken ct = default)
    {
        var parent = await _postRepo.GetByIdAsync(parentPostId, ct);
        if (parent == null || parent.IsDeleted) return null;

        var isQuote = !string.IsNullOrWhiteSpace(additionalComment);

        if (!isQuote)
        {
            var alreadyReposted = await _postRepo.AnyAsync(p =>
                p.AuthorId == userId
                && p.ParentPostId == parentPostId
                && p.IsRepost
                && !p.IsDeleted, ct);

            if (alreadyReposted)
                throw new InvalidOperationException("You have already reposted this post.");
        }

        if (isQuote && additionalComment!.Length > Constants.Limits.MaxPostLength)
            throw new InvalidOperationException(
                $"Quote comment cannot exceed {Constants.Limits.MaxPostLength} characters.");

        var repost = new Post
        {
            Id = Guid.NewGuid(),
            Content = isQuote ? additionalComment!.Trim() : string.Empty,
            AuthorId = userId,
            ParentPostId = parentPostId,
            IsRepost = !isQuote,
            IsQuote = isQuote,
            Visibility = PostVisibility.Public,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _postRepo.AddAsync(repost, ct);

        parent.RepostsCount++;
        parent.UpdatedAt = DateTime.UtcNow;
        _postRepo.Update(parent);

        var author = await _userRepo.GetByIdAsync(userId, ct);
        if (author != null)
        {
            author.PostsCount++;
            _userRepo.Update(author);
        }

        await _postRepo.SaveChangesAsync(ct);

        if (parent.AuthorId != userId)
        {
            try
            {
                await _notificationService.NotifyRepostAsync(parent.AuthorId, userId, repost.Id, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to notify repost to {AuthorId}", parent.AuthorId);
            }
        }

        try
        {
            var fresh = await _postRepo.GetWithDetailsAsync(repost.Id, ct);
            if (fresh != null)
            {
                var dto = await MapSingleAsync(fresh, userId, ct);
                await _realtime.NotifyPostCreatedAsync(userId, dto, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Real-time NotifyPostCreated failed for repost {PostId}", repost.Id);
        }

        var finalPost = await _postRepo.GetWithDetailsAsync(repost.Id, ct);
        return await MapSingleAsync(finalPost!, userId, ct);
    }

    public Task<PostDto?> QuoteAsync(
        Guid parentPostId, Guid userId, string comment, CancellationToken ct = default)
        => RepostAsync(parentPostId, userId, comment, ct);

    // ============================================================
    // Interactions
    // ============================================================
    public async Task<bool> LikeAsync(Guid postId, Guid userId, CancellationToken ct = default)
    {
        var post = await _postRepo.GetByIdAsync(postId, ct);
        if (post == null || post.IsDeleted) return false;

        var existing = await _likeRepo.FirstOrDefaultAsync(
            l => l.UserId == userId && l.PostId == postId, ct);

        if (existing != null) return true;

        await _likeRepo.AddAsync(new Like
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PostId = postId,
            CreatedAt = DateTime.UtcNow
        }, ct);

        post.LikesCount++;
        post.UpdatedAt = DateTime.UtcNow;
        _postRepo.Update(post);

        var author = await _userRepo.GetByIdAsync(post.AuthorId, ct);
        if (author != null)
        {
            author.LikesReceivedCount++;
            _userRepo.Update(author);
        }

        await _postRepo.SaveChangesAsync(ct);

        if (post.AuthorId != userId)
        {
            try
            {
                await _notificationService.NotifyLikeAsync(post.AuthorId, userId, postId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to notify like to {AuthorId}", post.AuthorId);
            }
        }

        await SafeCountsUpdateAsync(post, ct);
        return true;
    }

    public async Task<bool> UnlikeAsync(Guid postId, Guid userId, CancellationToken ct = default)
    {
        var post = await _postRepo.GetByIdAsync(postId, ct);
        if (post == null) return false;

        var existing = await _likeRepo.FirstOrDefaultAsync(
            l => l.UserId == userId && l.PostId == postId, ct);

        if (existing == null) return true;

        _likeRepo.Remove(existing);

        if (post.LikesCount > 0) post.LikesCount--;
        post.UpdatedAt = DateTime.UtcNow;
        _postRepo.Update(post);

        var author = await _userRepo.GetByIdAsync(post.AuthorId, ct);
        if (author != null && author.LikesReceivedCount > 0)
        {
            author.LikesReceivedCount--;
            _userRepo.Update(author);
        }

        await _postRepo.SaveChangesAsync(ct);

        await SafeCountsUpdateAsync(post, ct);
        return true;
    }

    public async Task<bool> BookmarkAsync(Guid postId, Guid userId, CancellationToken ct = default)
    {
        var post = await _postRepo.GetByIdAsync(postId, ct);
        if (post == null || post.IsDeleted) return false;

        var existing = await _bookmarkRepo.FirstOrDefaultAsync(
            b => b.UserId == userId && b.PostId == postId, ct);

        if (existing != null) return true;

        await _bookmarkRepo.AddAsync(new Bookmark
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PostId = postId,
            CreatedAt = DateTime.UtcNow
        }, ct);

        post.BookmarksCount++;
        post.UpdatedAt = DateTime.UtcNow;
        _postRepo.Update(post);

        await _postRepo.SaveChangesAsync(ct);

        await SafeCountsUpdateAsync(post, ct);
        return true;
    }

    public async Task<bool> UnbookmarkAsync(Guid postId, Guid userId, CancellationToken ct = default)
    {
        var post = await _postRepo.GetByIdAsync(postId, ct);
        if (post == null) return false;

        var existing = await _bookmarkRepo.FirstOrDefaultAsync(
            b => b.UserId == userId && b.PostId == postId, ct);

        if (existing == null) return true;

        _bookmarkRepo.Remove(existing);

        if (post.BookmarksCount > 0) post.BookmarksCount--;
        post.UpdatedAt = DateTime.UtcNow;
        _postRepo.Update(post);

        await _postRepo.SaveChangesAsync(ct);

        await SafeCountsUpdateAsync(post, ct);
        return true;
    }

    public async Task<bool> IncrementViewAsync(Guid postId, CancellationToken ct = default)
    {
        var post = await _postRepo.GetByIdAsync(postId, ct);
        if (post == null) return false;

        post.ViewsCount++;
        _postRepo.Update(post);
        await _postRepo.SaveChangesAsync(ct);
        return true;
    }

    // ============================================================
    // Mapping — PUBLIC (used by other services like FeedService)
    // ============================================================
    /// <summary>
    /// Bulk-maps already-loaded Post entities to PostDto with viewer context
    /// (liked/bookmarked flags). Uses at most 3 extra queries total,
    /// regardless of the number of posts.
    /// </summary>
    public async Task<IReadOnlyList<PostDto>> MapManyAsync(
        IReadOnlyList<Post> posts, Guid? currentUserId, CancellationToken ct = default)
    {
        if (posts.Count == 0) return Array.Empty<PostDto>();

        var postIds = posts.Select(p => p.Id).ToList();

        var likedIds = new HashSet<Guid>();
        var bookmarkedIds = new HashSet<Guid>();

        if (currentUserId.HasValue)
        {
            var likes = await _likeRepo.FindAsync(
                l => l.UserId == currentUserId.Value
                     && l.PostId != null
                     && postIds.Contains(l.PostId!.Value), ct);
            likedIds = likes.Select(l => l.PostId!.Value).ToHashSet();

            var bookmarks = await _bookmarkRepo.FindAsync(
                b => b.UserId == currentUserId.Value
                     && postIds.Contains(b.PostId), ct);
            bookmarkedIds = bookmarks.Select(b => b.PostId).ToHashSet();
        }

        var result = new List<PostDto>(posts.Count);
        foreach (var p in posts)
        {
            result.Add(MapToDto(
                p,
                currentUserId,
                likedIds.Contains(p.Id),
                bookmarkedIds.Contains(p.Id),
                p.IsRepost && currentUserId.HasValue && p.AuthorId == currentUserId.Value));
        }

        // ============================================================
        // Bulk-load parent viewer state (for reposts/quotes)
        // ============================================================
        var parentIds = posts
            .Where(p => p.ParentPostId.HasValue)
            .Select(p => p.ParentPostId!.Value)
            .Distinct()
            .ToList();

        if (parentIds.Count > 0 && currentUserId.HasValue)
        {
            var parentLikes = await _likeRepo.FindAsync(
                l => l.UserId == currentUserId.Value
                     && l.PostId != null
                     && parentIds.Contains(l.PostId!.Value), ct);
            var parentLikedIds = parentLikes.Select(l => l.PostId!.Value).ToHashSet();

            var parentBookmarks = await _bookmarkRepo.FindAsync(
                b => b.UserId == currentUserId.Value
                     && parentIds.Contains(b.PostId), ct);
            var parentBookmarkedIds = parentBookmarks.Select(b => b.PostId).ToHashSet();

            var userReposts = await _postRepo.FindAsync(
                p => p.AuthorId == currentUserId.Value
                     && p.IsRepost
                     && !p.IsDeleted
                     && p.ParentPostId != null
                     && parentIds.Contains(p.ParentPostId!.Value), ct);
            var repostedParentIds = userReposts
                .Select(p => p.ParentPostId!.Value)
                .ToHashSet();

            foreach (var dto in result)
            {
                if (dto.ParentPost == null) continue;

                dto.ParentPost.IsLikedByCurrentUser = parentLikedIds.Contains(dto.ParentPost.Id);
                dto.ParentPost.IsBookmarkedByCurrentUser = parentBookmarkedIds.Contains(dto.ParentPost.Id);
                dto.ParentPost.IsRepostedByCurrentUser = repostedParentIds.Contains(dto.ParentPost.Id);
            }
        }

        return result;
    }

    // ============================================================
    // Mapping — PRIVATE helpers
    // ============================================================
    private async Task<PostDto> MapSingleAsync(
        Post post, Guid? currentUserId, CancellationToken ct)
    {
        var liked = false;
        var bookmarked = false;
        var reposted = false;

        if (currentUserId.HasValue)
        {
            liked = await _likeRepo.AnyAsync(
                l => l.UserId == currentUserId.Value && l.PostId == post.Id, ct);

            bookmarked = await _bookmarkRepo.AnyAsync(
                b => b.UserId == currentUserId.Value && b.PostId == post.Id, ct);

            if (post.ParentPostId.HasValue && post.AuthorId == currentUserId.Value)
                reposted = post.IsRepost;
        }

        var dto = MapToDto(post, currentUserId, liked, bookmarked, reposted);

        // Load parent post viewer state
        if (dto.ParentPost != null && currentUserId.HasValue)
        {
            dto.ParentPost.IsLikedByCurrentUser = await _likeRepo.AnyAsync(
                l => l.UserId == currentUserId.Value && l.PostId == dto.ParentPost.Id, ct);

            dto.ParentPost.IsBookmarkedByCurrentUser = await _bookmarkRepo.AnyAsync(
                b => b.UserId == currentUserId.Value && b.PostId == dto.ParentPost.Id, ct);

            var alreadyReposted = await _postRepo.AnyAsync(p =>
                p.AuthorId == currentUserId.Value
                && p.ParentPostId == dto.ParentPost.Id
                && p.IsRepost
                && !p.IsDeleted, ct);

            dto.ParentPost.IsRepostedByCurrentUser = alreadyReposted;
        }

        return dto;
    }

    private static PostDto MapToDto(
        Post post,
        Guid? currentUserId,
        bool isLiked,
        bool isBookmarked,
        bool isReposted)
    {
        return new PostDto
        {
            Id = post.Id,
            Content = post.Content,
            Author = new UserSummaryDto
            {
                Id = post.Author.Id,
                Username = post.Author.UserName ?? string.Empty,
                DisplayName = post.Author.DisplayName,
                AvatarUrl = post.Author.AvatarUrl,
                IsVerified = post.Author.IsVerified
            },
            ParentPostId = post.ParentPostId,
            ParentPost = post.ParentPost != null
                ? MapNested(post.ParentPost)
                : null,
            IsRepost = post.IsRepost,
            IsQuote = post.IsQuote,
            IsSensitive = post.IsSensitive,
            IsDeleted = post.IsDeleted,
            Visibility = post.Visibility,
            LikesCount = post.LikesCount,
            CommentsCount = post.CommentsCount,
            RepostsCount = post.RepostsCount,
            BookmarksCount = post.BookmarksCount,
            ViewsCount = post.ViewsCount,
            Media = post.Media?
                .Where(m => !m.IsDeleted)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new MediaDto
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
                }).ToList() ?? new(),
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt,
            RelativeTime = DateHelper.ToRelative(post.CreatedAt),
            IsLikedByCurrentUser = isLiked,
            IsBookmarkedByCurrentUser = isBookmarked,
            IsRepostedByCurrentUser = isReposted,
            CanEdit = currentUserId.HasValue && post.AuthorId == currentUserId.Value,
            CanDelete = currentUserId.HasValue && post.AuthorId == currentUserId.Value
        };
    }

    /// <summary>
    /// Shallow nested mapping for parent posts. Viewer flags are filled later
    /// by the caller (MapSingleAsync or MapManyAsync).
    /// </summary>
    private static PostDto MapNested(Post post)
    {
        return new PostDto
        {
            Id = post.Id,
            Content = post.Content,
            Author = new UserSummaryDto
            {
                Id = post.Author.Id,
                Username = post.Author.UserName ?? string.Empty,
                DisplayName = post.Author.DisplayName,
                AvatarUrl = post.Author.AvatarUrl,
                IsVerified = post.Author.IsVerified
            },
            ParentPostId = post.ParentPostId,
            IsRepost = post.IsRepost,
            IsQuote = post.IsQuote,
            IsDeleted = post.IsDeleted,
            Visibility = post.Visibility,
            LikesCount = post.LikesCount,
            CommentsCount = post.CommentsCount,
            RepostsCount = post.RepostsCount,
            BookmarksCount = post.BookmarksCount,
            ViewsCount = post.ViewsCount,
            Media = post.Media?
                .Where(m => !m.IsDeleted)
                .Select(m => new MediaDto
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
                }).ToList() ?? new(),
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt,
            RelativeTime = DateHelper.ToRelative(post.CreatedAt),
            CanEdit = false,
            CanDelete = false
        };
    }

    private async Task SafeCountsUpdateAsync(Post post, CancellationToken ct)
    {
        try
        {
            await _realtime.NotifyCountsUpdatedAsync(
                post.Id,
                post.LikesCount,
                post.CommentsCount,
                post.RepostsCount,
                post.BookmarksCount,
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Real-time counts update failed for post {PostId}", post.Id);
        }
    }
}