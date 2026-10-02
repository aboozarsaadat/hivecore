using System.ComponentModel.DataAnnotations;

namespace HiveCore.Models.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Display name is required.")]
    [StringLength(80, MinimumLength = 2, ErrorMessage = "Display name must be between 2 and 80 characters.")]
    [Display(Name = "Display Name")]
    public string DisplayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Username is required.")]
    [StringLength(30, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 30 characters.")]
    [RegularExpression(@"^[a-zA-Z0-9_.]+$", ErrorMessage = "Username can only contain letters, numbers, underscores, and dots.")]
    [Display(Name = "Username")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [MaxLength(256)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Display(Name = "I agree to the Terms & Privacy Policy")]
    [Range(typeof(bool), "true", "true", ErrorMessage = "You must accept the terms to continue.")]
    public bool AcceptTerms { get; set; } = false;
}

public class ForgotPasswordViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 8)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = string.Empty;
}