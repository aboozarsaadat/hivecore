using HiveCore.Models.Entities;

namespace HiveCore.Repositories.Interfaces;

public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<User?> GetWithProfileAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<User?> GetWithThemeAndMusicAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> SearchAsync(
        string query,
        int count,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> GetSuggestedAsync(
        Guid currentUserId,
        int count,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> GetFollowersAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> GetFollowingAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> IsUsernameTakenAsync(
        string username,
        CancellationToken cancellationToken = default);

    Task<bool> IsEmailTakenAsync(
        string email,
        CancellationToken cancellationToken = default);
}