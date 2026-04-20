using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using HotelBookingSystem.Models;
using HotelBookingSystem.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HotelBookingSystem.Controllers
{
    [Authorize(Roles = "admin")]
    public class RolesController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<User> _userManager;
        private readonly HotelDbContext _context;

        public RolesController(RoleManager<IdentityRole> roleManager, UserManager<User> userManager, HotelDbContext context)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> UserList(string searchEmail, string roleFilter)
        {
            ViewData["CurrentEmailFilter"] = searchEmail;
            ViewData["CurrentRoleFilter"] = roleFilter;

            var usersQuery = _userManager.Users.AsQueryable();

            if (!string.IsNullOrEmpty(searchEmail))
            {
                if (!string.IsNullOrEmpty(searchEmail))
                {
                    usersQuery = usersQuery.Where(u => u.Email != null && u.Email.Contains(searchEmail));
                }
            }

            var users = await usersQuery.ToListAsync();
            var filteredUsers = new List<User>();

            foreach (var user in users)
            {
                var isAdmin = await _userManager.IsInRoleAsync(user, "admin");
                bool matchesRole = true;

                if (roleFilter == "admin") matchesRole = isAdmin;
                else if (roleFilter == "user") matchesRole = !isAdmin;

                if (matchesRole)
                {
                    filteredUsers.Add(user);
                }
            }

            return View(filteredUsers);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string userid)
        {
            if (string.IsNullOrEmpty(userid)) return NotFound();

            User? user = await _userManager.FindByIdAsync(userid);
            if (user == null) return NotFound();

            var userRoles = await _userManager.GetRolesAsync(user);
            var allRoles = _roleManager.Roles.ToList();

            ChangeRoleViewModel model = new ChangeRoleViewModel
            {
                UserId = user.Id,
                UserEmail = user.Email,
                UserRoles = userRoles.ToList(),
                AllRoles = allRoles
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string userId, List<string> roles)
        {
            User? user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var userRoles = await _userManager.GetRolesAsync(user);
            roles ??= new List<string>();

            var addedRoles = roles.Except(userRoles);
            var removedRoles = userRoles.Except(roles);

            await _userManager.AddToRolesAsync(user, addedRoles);
            await _userManager.RemoveFromRolesAsync(user, removedRoles);

            if (roles.Contains("admin"))
            {
                var existingEmployee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.UserId == user.Id || e.Email == user.Email);

                if (existingEmployee == null)
                {
                    var clientData = await _context.Clients.FirstOrDefaultAsync(c => c.UserId == user.Id);
                    var employee = new Employee
                    {
                        UserId = user.Id,
                        Fullname = clientData?.Fullname ?? user.Email,
                        Email = user.Email,
                        Phonenumber = clientData?.Phonenumber ?? user.PhoneNumber,
                        Position = "Адміністратор",
                        Salary = 0,
                        IsActive = true
                    };
                    _context.Employees.Add(employee);
                    if (clientData != null) _context.Clients.Remove(clientData);
                }
                else
                {
                    existingEmployee.IsActive = true;
                    existingEmployee.UserId = user.Id;
                    _context.Employees.Update(existingEmployee);
                    var clientData = await _context.Clients.FirstOrDefaultAsync(c => c.UserId == user.Id);
                    if (clientData != null) _context.Clients.Remove(clientData);
                }
            }
            else
            {
                var employeeData = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == user.Id);
                if (employeeData != null && employeeData.IsActive == true)
                {
                    employeeData.IsActive = false;
                    _context.Employees.Update(employeeData);
                    _context.Clients.Add(new Client
                    {
                        UserId = user.Id,
                        Fullname = employeeData.Fullname,
                        Email = employeeData.Email,
                        Phonenumber = employeeData.Phonenumber,
                        IsActive = true
                    });
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("UserList");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null && user.Email != "admin@gmail.com")
            {
                await _userManager.DeleteAsync(user);
            }
            return RedirectToAction("UserList");
        }
    }
}