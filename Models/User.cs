using Microsoft.AspNetCore.Identity;

namespace HotelBookingSystem.Models
{
    public class User : IdentityUser
    {
        public string? FullName { get; set; }
        public int Year { get; set; }
    }
}