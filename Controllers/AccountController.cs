using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using HotelBookingSystem.Models;
using HotelBookingSystem.ViewModels;

namespace HotelBookingSystem.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly HotelDbContext _context;

    public AccountController(UserManager<User> userManager,
                            SignInManager<User> signInManager,
                            HotelDbContext context)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
    }

    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (ModelState.IsValid)
        {
            User user = new User
            {
                Email = model.Email,
                UserName = model.Email,
                FullName = model.FullName,
                Year = model.Year,
                PhoneNumber = model.PhoneNumber
            };

            var result = await _userManager.CreateAsync(user, model.Password!);

            if (result.Succeeded)
            {
                string role = (model.Email == "admin@gmail.com") ? "admin" : "user";
                await _userManager.AddToRoleAsync(user, role);

                if (role == "admin")
                {
                    var employee = new Employee
                    {
                        UserId = user.Id,
                        Fullname = model.FullName,
                        Email = model.Email,
                        Phonenumber = model.PhoneNumber,
                        Position = "Адміністратор",
                        Salary = 0
                    };
                    _context.Employees.Add(employee);
                }
                else
                {
                    var client = new Client
                    {
                        UserId = user.Id,
                        Fullname = model.FullName,
                        Email = model.Email,
                        Phonenumber = model.PhoneNumber,
                        IsActive = true
                    };
                    _context.Clients.Add(client);
                }

                await _context.SaveChangesAsync();
                await _signInManager.SignInAsync(user, false);
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
        return View(model);
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (ModelState.IsValid)
        {
            var result = await _signInManager.PasswordSignInAsync(
                model.Email ?? string.Empty, 
                model.Password ?? string.Empty, 
                model.RememberMe, 
                lockoutOnFailure: false
            );

            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Неправильний логін чи (та) пароль");
        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }
}