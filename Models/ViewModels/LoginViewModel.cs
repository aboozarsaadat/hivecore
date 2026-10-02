using System.ComponentModel.DataAnnotations;

namespace HiveCore.Models.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Email or username is required.")]
    [Display(Name = "Email or Username")]
    [MaxLength(256)]
    public string EmailOrUsername { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; } = false;

    [MaxLength(500)]
    public string? ReturnUrl { get; set; }
}

public class TwoFactorViewModel
{
    [Required]
    [Display(Name = "Verification Code")]
    [StringLength(7, MinimumLength = 6)]
    public string Code { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}