using System.ComponentModel.DataAnnotations;

namespace BookNest.Web.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Your name is required."), MaxLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Your email is required."), EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [MaxLength(254)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Your phone number is required.")]
    [RegularExpression("^01[0125][0-9]{8}$", ErrorMessage = "Enter a valid Egyptian mobile number (11 digits).")]
    [Display(Name = "Phone")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "A password is required."), DataType(DataType.Password)]
    [MinLength(8, ErrorMessage = "Use at least 8 characters.")]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "The two passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class LoginViewModel
{
    [Required(ErrorMessage = "Your email is required."), EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "A password is required."), DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public class ForgotPasswordViewModel
{
    [Required, EmailAddress, MaxLength(254)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordViewModel
{
    [Required]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), MinLength(8)]
    [Display(Name = "New password")]
    public string Password { get; set; } = string.Empty;

    [DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "The two passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ProfileViewModel
{
    [Required, MaxLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [RegularExpression("^01[0125][0-9]{8}$", ErrorMessage = "Enter a valid Egyptian mobile number (11 digits).")]
    [Display(Name = "Phone")]
    public string? Phone { get; set; }

    public string Email { get; set; } = string.Empty;
    public IReadOnlyList<Core.Models.AddressDto> Addresses { get; set; } = Array.Empty<Core.Models.AddressDto>();
}

public class AddressFormViewModel
{
    public int? Id { get; set; }

    [Required, MaxLength(100), Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required, RegularExpression("^01[0125][0-9]{8}$", ErrorMessage = "Enter a valid Egyptian mobile number (11 digits).")]
    [Display(Name = "Phone")]
    public string Phone { get; set; } = string.Empty;

    [Required, MaxLength(100), Display(Name = "City / governorate")]
    public string City { get; set; } = string.Empty;

    [Required, MaxLength(500), MinLength(10, ErrorMessage = "The address needs at least 10 characters.")]
    [Display(Name = "Address")]
    public string AddressLine { get; set; } = string.Empty;

    [MaxLength(20), Display(Name = "Postal code")]
    public string? PostalCode { get; set; }

    [Display(Name = "Set as default address")]
    public bool IsDefault { get; set; }
}

public class ChangePasswordViewModel
{
    [Required, DataType(DataType.Password), Display(Name = "Current password")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), MinLength(8), Display(Name = "New password")]
    public string NewPassword { get; set; } = string.Empty;

    [DataType(DataType.Password), Compare(nameof(NewPassword), ErrorMessage = "The two passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
