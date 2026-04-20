using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelBookingSystem.Models;
using Microsoft.AspNetCore.Authorization;
namespace HotelBookingSystem.Controllers;

[Authorize(Roles = "admin")]
public class EmployeesController : Controller
{
    private readonly HotelDbContext _context;

    public EmployeesController(HotelDbContext context) => _context = context;

    public async Task<IActionResult> Index(string status = "active")
    {
        ViewData["CurrentStatus"] = status;
        var query = _context.Employees.AsQueryable();

        if (status == "fired")
        {
            query = query.Where(e => e.Position != null && e.Position.ToLower() == "fired");
        }
        else
        {
            query = query.Where(e => e.Position == null || e.Position.ToLower() != "fired");
        }

        var employees = await query.OrderByDescending(e => e.Employeeid).ToListAsync();
        return View(employees);
    }

    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Employee employee)
    {
        bool emailExists = await _context.Employees.AnyAsync(e => e.Email == employee.Email);

        if (emailExists)
        {
            ModelState.AddModelError("Email", "Працівник з такою поштою вже зареєстрований у системі.");
        }

        if (ModelState.IsValid)
        {
            _context.Add(employee);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(employee);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null) return NotFound();
        var employee = await _context.Employees.FindAsync(id);
        if (employee is null) return NotFound();
        return View(employee);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Employee employee)
    {
        if (id != employee.Employeeid) return NotFound();
        bool emailExists = await _context.Employees
            .AnyAsync(e => e.Email == employee.Email && e.Employeeid != id);

        if (emailExists)
        {
            ModelState.AddModelError("Email", "Цей Email уже закріплений за іншим працівником.");
        }

        if (ModelState.IsValid)
        {
            try
            {
                employee.Updatedat = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
                _context.Update(employee);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (await _context.Employees.FindAsync(id) is null) return NotFound();
                else throw;
            }
        }
        return View(employee);
    }
    private bool EmployeeExists(int id) => _context.Employees.Any(e => e.Employeeid == id);
}