using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace HotelBookingSystem.ViewModels
{
    public class ChangeRoleViewModel
{
    public string? UserId { get; set; }
    public string? UserEmail { get; set; }
    public List<string> UserRoles { get; set; } = new List<string>();
    public List<Microsoft.AspNetCore.Identity.IdentityRole> AllRoles { get; set; } = new List<Microsoft.AspNetCore.Identity.IdentityRole>();
}
}