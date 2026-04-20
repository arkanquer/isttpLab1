using System.ComponentModel.DataAnnotations;

namespace HotelBookingSystem.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        [EmailAddress]
        public string? Email { get; set; }

        [Required]
        [Display(Name = "ПІБ")]
        public string? FullName { get; set; }

        [Required]
        [Display(Name = "Телефон")]
        public string? PhoneNumber { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [Compare("Password", ErrorMessage = "Паролі не збігаються")]
        public string? ConfirmPassword { get; set; }

        public int Year { get; set; }
    }
}