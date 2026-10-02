using HiveCore.Models.DTOs;
using HiveCore.Models.ViewModels;

namespace HiveCore.Services.Interfaces;

public interface IUserService
{
    Task<UserDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<UserDto?> GetByUsernameAsync(string username, CancellationToken ct = default);

    Task<ProfileViewModel?> GetProfileAsync(
        string username,
        Guid? currentUserId,
        string tab,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<(IReadOnlyList<UserSummaryDto> Users, int Total)> SearchAsync(
        string query, int page, int pageSize, CancellationToken ct = default);

    Task<IReadOnlyList<UserSummaryDto>> GetSuggestedAsync(
        Guid currentUserId, int count, CancellationToken ct = default);

    Task<IReadOnlyList<UserSummaryDto>> GetFollowersAsync(
        Guid userId, CancellationToken ct = default);

    Task<IReadOnlyList<UserSummaryDto>> GetFollowingAsync(
        Guid userId, CancellationToken ct = default);

    Task<EditProfileViewModel?> GetEditProfileAsync(Guid userId, CancellationToken ct = default);

    Task<UserDto> UpdateProfileAsync(
        Guid userId, EditProfileViewModel model, CancellationToken ct = default);

    Task<ProfileSettingsViewModel?> GetSettingsAsync(Guid userId, CancellationToken ct = default);

    Task UpdateSettingsAsync(
        Guid userId, ProfileSettingsViewModel model, CancellationToken ct = default);

    Task<bool> FollowAsync(Guid followerId, Guid targetUserId, CancellationToken ct = default);

    Task<bool> UnfollowAsync(Guid followerId, Guid targetUserId, CancellationToken ct = default);

    Task<bool> AcceptFollowRequestAsync(
        Guid userId, Guid requesterId, CancellationToken ct = default);

    Task<bool> RejectFollowRequestAsync(
        Guid userId, Guid requesterId, CancellationToken ct = default);

    Task<bool> DeactivateAsync(Guid userId, CancellationToken ct = default);
}