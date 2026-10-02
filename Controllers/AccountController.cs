using HiveCore.Models.ViewModels;
using HiveCore.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HiveCore.Controllers;

[Route("Account")]
public class AccountController : Controller
{
    private readonly IAuthService _authService;
    private readonly IUserService _userService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IAuthService authService,
        IUserService userService,
        ILogger<AccountController> logger)
    {
        _authService = authService;
        _userService = userService;
        _logger = logger;
    }

    // ============================================================
    // Login
    // ============================================================
    [HttpGet("Login")]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost("Login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken ct)
    {
        ViewData["ReturnUrl"] = model.ReturnUrl;

        if (!ModelState.IsValid)
            return View(model);

        var (success, error, user) = await _authService.LoginAsync(model, ct);

        if (!success)
        {
            if (error == "REQUIRES_2FA" && user != null)
            {
                return RedirectToAction(nameof(TwoFactor), new { returnUrl = model.ReturnUrl });
            }

            ModelState.AddModelError(string.Empty, error ?? "Login failed.");
            return View(model);
        }

        _logger.LogInformation("User {UserId} logged in", user!.Id);

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return Redirect(model.ReturnUrl);

        return RedirectToAction("Index", "Home");
    }

    // ============================================================
    // Register
    // ============================================================
    [HttpGet("Register")]
    [AllowAnonymous]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");

        return View(new RegisterViewModel());
    }

    [HttpPost("Register")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(model);

        var (success, error, user) = await _authService.RegisterAsync(model, ct);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Registration failed.");
            return View(model);
        }

        _logger.LogInformation("New user registered: {UserId}", user!.Id);

        // Auto sign-in
        var loginVm = new LoginViewModel
        {
            EmailOrUsername = model.Email,
            Password = model.Password,
            RememberMe = true
        };

        await _authService.LoginAsync(loginVm, ct);

        TempData["Success"] = "Welcome to HiveCore!";
        return RedirectToAction("Index", "Home");
    }

    // ============================================================
    // Logout
    // ============================================================
    [HttpPost("Logout")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        await _authService.LogoutAsync(ct);
        return RedirectToAction("Index", "Home");
    }

    // ============================================================
    // Forgot / Reset password
    // ============================================================
    [HttpGet("ForgotPassword")]
    [AllowAnonymous]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [HttpPost("ForgotPassword")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);

        var baseUrl = Url.Action(nameof(ResetPassword), "Account", null, Request.Scheme)!;
        await _authService.SendPasswordResetAsync(model.Email, baseUrl, ct);

        TempData["Info"] = "If your email is registered, a reset link has been sent.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet("ResetPassword")]
    [AllowAnonymous]
    public IActionResult ResetPassword(string? email = null, string? token = null)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            return RedirectToAction(nameof(Login));

        return View(new ResetPasswordViewModel { Email = email, Token = token });
    }

    [HttpPost("ResetPassword")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(model);

        var (success, error) = await _authService.ResetPasswordAsync(model, ct);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Reset failed.");
            return View(model);
        }

        TempData["Success"] = "Password reset successfully. Please sign in.";
        return RedirectToAction(nameof(Login));
    }

    // ============================================================
    // Two-factor (placeholder)
    // ============================================================
    [HttpGet("TwoFactor")]
    [AllowAnonymous]
    public IActionResult TwoFactor(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new TwoFactorViewModel { ReturnUrl = returnUrl });
    }

    // ============================================================
    // Access denied
    // ============================================================
    [HttpGet("AccessDenied")]
    [AllowAnonymous]
    public IActionResult AccessDenied() => View();
}