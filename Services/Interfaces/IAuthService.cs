using HiveCore.Models.Entities;
using HiveCore.Models.ViewModels;

namespace HiveCore.Services.Interfaces;

public interface IAuthService
{
    Task<(bool Success, string? Error, User? User)> RegisterAsync(
        RegisterViewModel model, CancellationToken ct = default);

    Task<(bool Success, string? Error, User? User)> LoginAsync(
        LoginViewModel model, CancellationToken ct = default);

    Task LogoutAsync(CancellationToken ct = default);

    Task<(bool Success, string? Error)> SendPasswordResetAsync(
        string email, string resetBaseUrl, CancellationToken ct = default);

    Task<(bool Success, string? Error)> ResetPasswordAsync(
        ResetPasswordViewModel model, CancellationToken ct = default);

    Task<(bool Success, string? Error)> ConfirmEmailAsync(
        Guid userId, string token, CancellationToken ct = default);

    Task<string?> GenerateEmailConfirmationTokenAsync(Guid userId, CancellationToken ct = default);

    Task<(bool Success, string? Error)> ChangePasswordAsync(
        Guid userId, string currentPassword, string newPassword, CancellationToken ct = default);

    Task<(bool Success, string? Error)> EnableTwoFactorAsync(
        Guid userId, CancellationToken ct = default);

    Task<(bool Success, string? Error)> DisableTwoFactorAsync(
        Guid userId, CancellationToken ct = default);
}