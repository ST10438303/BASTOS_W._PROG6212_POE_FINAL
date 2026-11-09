using System.ComponentModel.DataAnnotations;
using RaceDay.Web.Services;

namespace RaceDay.Web.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Please enter your email address."), EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Display(Name = "Email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your password.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Please enter your full name."), StringLength(100)]
        [Display(Name = "Full name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your email address."), EmailAddress(ErrorMessage = "Please enter a valid email address."), StringLength(150)]
        [Display(Name = "Email address")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Please enter a valid phone number."), StringLength(20)]
        [Display(Name = "Phone number (optional)")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Please choose a password."), MinLength(8, ErrorMessage = "Your password must be at least 8 characters long.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password.")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "The two passwords do not match.")]
        [Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please choose how you will use RaceDay.")]
        [RegularExpression("^(Organiser|Participant)$", ErrorMessage = "Please choose Participant or Organiser.")]
        [Display(Name = "I am joining as a")]
        public string Role { get; set; } = RoleNames.Participant;
    }

    public class ProfileViewModel
    {
        [Required(ErrorMessage = "Please enter your full name."), StringLength(100)]
        [Display(Name = "Full name")]
        public string FullName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Please enter a valid phone number."), StringLength(20)]
        [Display(Name = "Phone number")]
        public string? PhoneNumber { get; set; }

        // Read-only details shown on the page.
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public DateTime MemberSince { get; set; }
    }
}
