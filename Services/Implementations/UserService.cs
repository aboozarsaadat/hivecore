using HiveCore.Models.DTOs;
using HiveCore.Models.Entities;
using HiveCore.Models.ViewModels;
using HiveCore.Repositories.Interfaces;
using HiveCore.Services.Interfaces;
using HiveCore.Utilities;
using Microsoft.Extensions.Logging;

namespace HiveCore.Services.Implementations;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepo;
    private readonly IFollowRepository _followRepo;
    private readonly IPostService _postService;
    private readonly IMediaService _mediaService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepo,
        IFollowRepository followRepo,
        IPostService postService,
        IMediaService mediaService,
        INotificationService notificationService,
        ILogger<UserService> logger)
    {
        _userRepo = userRepo;
        _followRepo = followRepo;
        _postService = postService;
        _mediaService = mediaService;
        _notificationService = notificationService;
        _logger = logger;
    }

    // ============================================================
    // Read
    // ============================================================
    public async Task<UserDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _userRepo.GetWithThemeAndMusicAsync(id, ct);
        return user == null ? null : MapToDto(user);
    }

    public async Task<UserDto?> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByUsernameAsync(username, ct);
        if (user == null) return null;

        var full = await _userRepo.GetWithThemeAndMusicAsync(user.Id, ct);
        return full == null ? null : MapToDto(full);
    }

    public async Task<ProfileViewModel?> GetProfileAsync(
    string username,
    Guid? currentUserId,
    string tab,
    int page,
    int pageSize,
    CancellationToken ct = default)
    {
        var user = await _userRepo.GetByUsernameAsync(username, ct);
        if (user == null) return null;

        var full = await _userRepo.GetWithThemeAndMusicAsync(user.Id, ct);
        if (full == null) return null;

        var isOwnProfile = currentUserId.HasValue && currentUserId.Value == user.Id;

        var isFollowing = false;
        var isPending = false;

        if (currentUserId.HasValue && !isOwnProfile)
        {
            isFollowing = await _followRepo.IsFollowingAsync(currentUserId.Value, user.Id, ct);
            isPending = await _followRepo.HasPendingRequestAsync(currentUserId.Value, user.Id, ct);
        }

        var canView = !user.IsPrivate || isOwnProfile || isFollowing;

        // Normalize tab
        tab = string.IsNullOrWhiteSpace(tab) ? "posts" : tab.Trim().ToLowerInvariant();
        if (tab != "posts" && tab != "media" && tab != "likes")
            tab = "posts";

        var vm = new ProfileViewModel
        {
            User = MapToDto(full),
            IsOwnProfile = isOwnProfile,
            IsFollowing = isFollowing,
            IsFollowPending = isPending,
            FollowersCount = user.FollowersCount,
            FollowingCount = user.FollowingCount,
            PostsCount = user.PostsCount,
            CanViewContent = canView,
            Tab = tab
        };

        if (!canView)
            return vm;

        // Guard rails
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        IReadOnlyList<HiveCore.Models.DTOs.PostDto> posts;
        int total;

        switch (tab)
        {
            case "media":
                (posts, total) = await _postService.GetUserMediaPostsAsync(
                    user.Id, currentUserId, page, pageSize, ct);
                break;

            case "likes":
                (posts, total) = await _postService.GetLikedPostsAsync(
                    user.Id, currentUserId, page, pageSize, ct);
                break;

            case "posts":
            default:
                (posts, total) = await _postService.GetUserPostsAsync(
                    user.Id, currentUserId, page, pageSize, ct);
                break;
        }

        vm.Posts = posts.ToList();
        vm.CurrentPage = page;
        vm.TotalPages = (int)Math.Ceiling(total / (double)pageSize);

        return vm;
    }

    public async Task<(IReadOnlyList<UserSummaryDto> Users, int Total)> SearchAsync(
        string query, int page, int pageSize, CancellationToken ct = default)
    {
        // For simplicity, we search first N. In production, implement proper paging in repo.
        var users = await _userRepo.SearchAsync(query, pageSize, ct);
        return (users.Select(MapToSummary).ToList(), users.Count);
    }

    public async Task<IReadOnlyList<UserSummaryDto>> GetSuggestedAsync(
        Guid currentUserId, int count, CancellationToken ct = default)
    {
        var users = await _userRepo.GetSuggestedAsync(currentUserId, count, ct);
        return users.Select(MapToSummary).ToList();
    }

    public async Task<IReadOnlyList<UserSummaryDto>> GetFollowersAsync(
        Guid userId, CancellationToken ct = default)
    {
        var users = await _userRepo.GetFollowersAsync(userId, ct);
        return users.Select(MapToSummary).ToList();
    }

    public async Task<IReadOnlyList<UserSummaryDto>> GetFollowingAsync(
        Guid userId, CancellationToken ct = default)
    {
        var users = await _userRepo.GetFollowingAsync(userId, ct);
        return users.Select(MapToSummary).ToList();
    }

    // ============================================================
    // Profile edit
    // ============================================================
    public async Task<EditProfileViewModel?> GetEditProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdAsync(userId, ct);
        if (user == null) return null;

        return new EditProfileViewModel
        {
            DisplayName = user.DisplayName,
            Bio = user.Bio,
            Location = user.Location,
            Website = user.Website,
            Pronouns = user.Pronouns,
            DateOfBirth = user.DateOfBirth,
            IsPrivate = user.IsPrivate,
            ShowOnlineStatus = user.ShowOnlineStatus,
            CurrentAvatarUrl = user.AvatarUrl,
            CurrentCoverUrl = user.CoverUrl
        };
    }

    public async Task<UserDto> UpdateProfileAsync(
        Guid userId, EditProfileViewModel model, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdAsync(userId, ct)
            ?? throw new KeyNotFoundException("User not found.");

        user.DisplayName = model.DisplayName.Trim();
        user.Bio = model.Bio?.Trim();
        user.Location = model.Location?.Trim();
        user.Website = model.Website?.Trim();
        user.Pronouns = model.Pronouns?.Trim();
        user.DateOfBirth = model.DateOfBirth;
        user.IsPrivate = model.IsPrivate;
        user.ShowOnlineStatus = model.ShowOnlineStatus;
        user.UpdatedAt = DateTime.UtcNow;

        if (model.AvatarFile != null)
        {
            user.AvatarUrl = await _mediaService.SaveAvatarAsync(userId, model.AvatarFile, ct);
        }

        if (model.CoverFile != null)
        {
            user.CoverUrl = await _mediaService.SaveCoverAsync(userId, model.CoverFile, ct);
        }

        _userRepo.Update(user);
        await _userRepo.SaveChangesAsync(ct);

        _logger.LogInformation("Profile updated for {UserId}", userId);

        var full = await _userRepo.GetWithThemeAndMusicAsync(userId, ct);
        return MapToDto(full!);
    }

    // ============================================================
    // Settings
    // ============================================================
    public async Task<ProfileSettingsViewModel?> GetSettingsAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdAsync(userId, ct);
        if (user == null) return null;

        return new ProfileSettingsViewModel
        {
            EmailNotificationsEnabled = user.EmailNotificationsEnabled,
            PushNotificationsEnabled = user.PushNotificationsEnabled,
            ShowOnlineStatus = user.ShowOnlineStatus,
            PreferredLanguage = user.PreferredLanguage,
            TimeZone = user.TimeZone,
            IsPrivate = user.IsPrivate
        };
    }

    public async Task UpdateSettingsAsync(
        Guid userId, ProfileSettingsViewModel model, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdAsync(userId, ct)
            ?? throw new KeyNotFoundException("User not found.");

        user.EmailNotificationsEnabled = model.EmailNotificationsEnabled;
        user.PushNotificationsEnabled = model.PushNotificationsEnabled;
        user.ShowOnlineStatus = model.ShowOnlineStatus;
        user.PreferredLanguage = model.PreferredLanguage;
        user.TimeZone = model.TimeZone;
        user.IsPrivate = model.IsPrivate;
        user.UpdatedAt = DateTime.UtcNow;

        _userRepo.Update(user);
        await _userRepo.SaveChangesAsync(ct);
    }

    // ============================================================
    // Follow
    // ============================================================
    public async Task<bool> FollowAsync(Guid followerId, Guid targetUserId, CancellationToken ct = default)
    {
        if (followerId == targetUserId) return false;

        var target = await _userRepo.GetByIdAsync(targetUserId, ct);
        if (target == null) return false;

        var existing = await _followRepo.GetFollowAsync(followerId, targetUserId, ct);
        if (existing != null) return true;

        var follow = new Follow
        {
            Id = Guid.NewGuid(),
            FollowerId = followerId,
            FollowingId = targetUserId,
            IsPending = target.IsPrivate,
            CreatedAt = DateTime.UtcNow,
            AcceptedAt = target.IsPrivate ? null : DateTime.UtcNow
        };

        await _followRepo.AddAsync(follow, ct);

        var follower = await _userRepo.GetByIdAsync(followerId, ct);

        if (!target.IsPrivate)
        {
            target.FollowersCount++;
            if (follower != null) follower.FollowingCount++;
        }

        _userRepo.Update(target);
        if (follower != null) _userRepo.Update(follower);

        await _followRepo.SaveChangesAsync(ct);

        if (target.IsPrivate)
            await _notificationService.NotifyFollowRequestAsync(targetUserId, followerId, follow.Id, ct);
        else
            await _notificationService.NotifyFollowAsync(targetUserId, followerId, follow.Id, ct);

        return true;
    }

    public async Task<bool> UnfollowAsync(Guid followerId, Guid targetUserId, CancellationToken ct = default)
    {
        var follow = await _followRepo.GetFollowAsync(followerId, targetUserId, ct);
        if (follow == null) return true;

        _followRepo.Remove(follow);

        if (!follow.IsPending)
        {
            var target = await _userRepo.GetByIdAsync(targetUserId, ct);
            var follower = await _userRepo.GetByIdAsync(followerId, ct);

            if (target != null && target.FollowersCount > 0) target.FollowersCount--;
            if (follower != null && follower.FollowingCount > 0) follower.FollowingCount--;

            if (target != null) _userRepo.Update(target);
            if (follower != null) _userRepo.Update(follower);
        }

        await _followRepo.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> AcceptFollowRequestAsync(
        Guid userId, Guid requesterId, CancellationToken ct = default)
    {
        var follow = await _followRepo.GetFollowAsync(requesterId, userId, ct);
        if (follow == null || !follow.IsPending) return false;

        follow.IsPending = false;
        follow.AcceptedAt = DateTime.UtcNow;
        _followRepo.Update(follow);

        var target = await _userRepo.GetByIdAsync(userId, ct);
        var requester = await _userRepo.GetByIdAsync(requesterId, ct);

        if (target != null) { target.FollowersCount++; _userRepo.Update(target); }
        if (requester != null) { requester.FollowingCount++; _userRepo.Update(requester); }

        await _followRepo.SaveChangesAsync(ct);

        await _notificationService.NotifyFollowAcceptedAsync(requesterId, userId, follow.Id, ct);

        return true;
    }

    public async Task<bool> RejectFollowRequestAsync(
        Guid userId, Guid requesterId, CancellationToken ct = default)
    {
        var follow = await _followRepo.GetFollowAsync(requesterId, userId, ct);
        if (follow == null || !follow.IsPending) return false;

        _followRepo.Remove(follow);
        await _followRepo.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeactivateAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByIdAsync(userId, ct);
        if (user == null) return false;

        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        _userRepo.Update(user);
        await _userRepo.SaveChangesAsync(ct);

        _logger.LogInformation("User {UserId} deactivated", userId);
        return true;
    }

    // ============================================================
    // Mapping
    // ============================================================
    private static UserSummaryDto MapToSummary(User u) => new()
    {
        Id = u.Id,
        Username = u.UserName ?? string.Empty,
        DisplayName = u.DisplayName,
        AvatarUrl = u.AvatarUrl,
        IsVerified = u.IsVerified
    };

    private static UserDto MapToDto(User u) => new()
    {
        Id = u.Id,
        Username = u.UserName ?? string.Empty,
        DisplayName = u.DisplayName,
        Bio = u.Bio,
        Location = u.Location,
        Website = u.Website,
        Pronouns = u.Pronouns,
        AvatarUrl = u.AvatarUrl,
        CoverUrl = u.CoverUrl,
        DateOfBirth = u.DateOfBirth,
        IsVerified = u.IsVerified,
        IsPrivate = u.IsPrivate,
        IsSuspended = u.IsSuspended,
        IsDeleted = u.IsDeleted,
        FollowersCount = u.FollowersCount,
        FollowingCount = u.FollowingCount,
        PostsCount = u.PostsCount,
        LikesReceivedCount = u.LikesReceivedCount,
        CreatedAt = u.CreatedAt,
        LastLoginAt = u.LastLoginAt,
        Theme = u.ProfileTheme == null ? null : new UserThemeDto
        {
            PrimaryColor = u.ProfileTheme.PrimaryColor,
            SecondaryColor = u.ProfileTheme.SecondaryColor,
            AccentColor = u.ProfileTheme.AccentColor,
            BackgroundColor = u.ProfileTheme.BackgroundColor,
            TextColor = u.ProfileTheme.TextColor,
            BackgroundGradient = u.ProfileTheme.BackgroundGradient,
            HeaderGradient = u.ProfileTheme.HeaderGradient,
            FontFamily = u.ProfileTheme.FontFamily,
            BackgroundImageUrl = u.ProfileTheme.BackgroundImageUrl,
            BackgroundMode = u.ProfileTheme.BackgroundMode,
            LayoutStyle = u.ProfileTheme.LayoutStyle,
            ShowBadges = u.ProfileTheme.ShowBadges,
            ShowMusicPlayer = u.ProfileTheme.ShowMusicPlayer
        },
        MusicTracks = u.MusicTracks?
            .Where(m => !m.IsDeleted)
            .OrderBy(m => m.DisplayOrder)
            .Select(m => new MusicTrackDto
            {
                Id = m.Id,
                Title = m.Title,
                Artist = m.Artist,
                Album = m.Album,
                CoverUrl = m.CoverUrl,
                AudioUrl = m.AudioUrl,
                Provider = m.Provider,
                ExternalUrl = m.ExternalUrl,
                Duration = m.Duration,
                DisplayOrder = m.DisplayOrder
            }).ToList() ?? new()
    };
}