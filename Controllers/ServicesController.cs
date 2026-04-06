using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelBookingSystem.Models;

namespace HotelBookingSystem.Controllers;

public class ServicesController : Controller
{
    private readonly HotelDbContext _context;

    public ServicesController(HotelDbContext context) => _context = context;

    public async Task<IActionResult> Index(string status = "active")
    {
        ViewData["CurrentStatus"] = status;
        var query = _context.Services.AsQueryable();

        if (status == "archived")
        {
            query = query.Where(s => s.IsAvailable == false);
        }
        else
        {
            query = query.Where(s => s.IsAvailable == true);
        }

        var services = await query.OrderBy(s => s.Name).ToListAsync();
        return View(services);
    }

    [HttpPost]
    public async Task<IActionResult> Archive(int id)
    {
        var service = await _context.Services.FindAsync(id);
        if (service is not null)
        {
            service.IsAvailable = false;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Restore(int id)
    {
        var service = await _context.Services.FindAsync(id);
        if (service is not null)
        {
            service.IsAvailable = true;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Service service)
    {
        if (ModelState.IsValid)
        {
            service.IsAvailable = true;
            _context.Add(service);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(service);
    }


    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null) return NotFound();

        var service = await _context.Services.FindAsync(id);
        if (service is null) return NotFound();

        return View(service);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Serviceid,Name,Description,Price,IsAvailable")] Service service)
    {
        if (id != service.Serviceid) return NotFound();

        if (ModelState.IsValid)
        {
            _context.Update(service);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(service);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var service = await _context.Services.FindAsync(id);
        if (service is not null)
        {
            service.IsAvailable = false;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}