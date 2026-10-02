using HiveCore.Models.Entities;
using HiveCore.Models.ViewModels;
using HiveCore.Repositories.Interfaces;
using HiveCore.Services.Helpers;
using HiveCore.Services.Interfaces;
using HiveCore.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace HiveCore.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IUserRepository _userRepo;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IUserRepository userRepo,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _userRepo = userRepo;
        _logger = logger;
    }

    public async Task<(bool Success, string? Error, User? User)> RegisterAsync(
        RegisterViewModel model, CancellationToken ct = default)
    {
        if (await _userRepo.IsUsernameTakenAsync(model.Username, ct))
            return (false, "This username is already taken.", null);

        if (await _userRepo.IsEmailTakenAsync(model.Email, ct))
            return (false, "This email is already registered.", null);

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = model.Username.Trim(),
            Email = model.Email.Trim(),
            DisplayName = model.DisplayName.Trim(),
            EmailConfirmed = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsVerified = false
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            var error = string.Join(" ", result.Errors.Select(e => e.Description));
            return (false, error, null);
        }

        await _userManager.AddToRoleAsync(user, Constants.Roles.User);

        _logger.LogInformation("User {UserId} ({Email}) registered", user.Id, user.Email);

        return (true, null, user);
    }

    public async Task<(bool Success, string? Error, User? User)> LoginAsync(
        LoginViewModel model, CancellationToken ct = default)
    {
        User? user = null;

        if (model.EmailOrUsername.Contains('@'))
            user = await _userRepo.GetByEmailAsync(model.EmailOrUsername, ct);
        else
            user = await _userRepo.GetByUsernameAsync(model.EmailOrUsername, ct);

        if (user == null)
            return (false, "Invalid login credentials.", null);

        if (user.IsSuspended)
            return (false, "Your account is suspended. Please contact support.", null);

        if (user.IsDeleted)
            return (false, "This account no longer exists.", null);

        var result = await _signInManager.PasswordSignInAsync(
            user, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("User {UserId} signed in", user.Id);
            return (true, null, user);
        }

        if (result.IsLockedOut)
            return (false, "Account locked due to too many failed attempts.", null);

        if (result.RequiresTwoFactor)
            return (false, "REQUIRES_2FA", user);

        return (false, "Invalid login credentials.", null);
    }

    public async Task LogoutAsync(CancellationToken ct = default)
    {
        await _signInManager.SignOutAsync();
    }

    public async Task<(bool Success, string? Error)> SendPasswordResetAsync(
        string email, string resetBaseUrl, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByEmailAsync(email, ct);
        if (user == null)
        {
            // Prevent account enumeration — return success regardless
            return (true, null);
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encoded = Uri.EscapeDataString(token);
        var resetLink = $"{resetBaseUrl}?email={Uri.EscapeDataString(email)}&token={encoded}";

        // TODO: Send email via IEmailSender (not implemented in this scope)
        _logger.LogInformation("Password reset link for {Email}: {Link}", email, resetLink);

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ResetPasswordAsync(
        ResetPasswordViewModel model, CancellationToken ct = default)
    {
        var user = await _userRepo.GetByEmailAsync(model.Email, ct);
        if (user == null) return (false, "Invalid request.");

        var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
        if (!result.Succeeded)
            return (false, string.Join(" ", result.Errors.Select(e => e.Description)));

        _logger.LogInformation("Password reset for {UserId}", user.Id);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ConfirmEmailAsync(
        Guid userId, string token, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return (false, "User not found.");

        var result = await _userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
            return (false, string.Join(" ", result.Errors.Select(e => e.Description)));

        user.IsVerified = true;
        await _userManager.UpdateAsync(user);

        return (true, null);
    }

    public async Task<string?> GenerateEmailConfirmationTokenAsync(
        Guid userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return null;

        return await _userManager.GenerateEmailConfirmationTokenAsync(user);
    }

    public async Task<(bool Success, string? Error)> ChangePasswordAsync(
        Guid userId, string currentPassword, string newPassword, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return (false, "User not found.");

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!result.Succeeded)
            return (false, string.Join(" ", result.Errors.Select(e => e.Description)));

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> EnableTwoFactorAsync(
        Guid userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return (false, "User not found.");

        await _userManager.SetTwoFactorEnabledAsync(user, true);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DisableTwoFactorAsync(
        Guid userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return (false, "User not found.");

        await _userManager.SetTwoFactorEnabledAsync(user, false);
        return (true, null);
    }
}