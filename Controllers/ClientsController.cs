using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelBookingSystem.Models;

namespace HotelBookingSystem.Controllers;

public class ClientsController : Controller
{
    private readonly HotelDbContext _context;

    public ClientsController(HotelDbContext context) => _context = context;

    public async Task<IActionResult> Index(string status = "active", string searchString = "")
    {
        ViewData["CurrentStatus"] = status;
        ViewData["CurrentFilter"] = searchString;
        var query = _context.Clients.AsQueryable();

        if (status == "archived")
        {
            query = query.Where(c => c.IsActive == false);
        }
        else
        {
            query = query.Where(c => c.IsActive == true);
        }

        if (!string.IsNullOrWhiteSpace(searchString))
        {
            string s = $"%{searchString.Trim()}%";
            query = query.Where(c => c.Fullname != null && EF.Functions.ILike(c.Fullname, s));
        }
        var clients = await query.OrderByDescending(c => c.Clientid).ToListAsync();
        return View(clients);
    }

    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Client client)
    {
        bool emailExists = await _context.Clients.AnyAsync(c => c.Email == client.Email);
        if (emailExists)
        {
            ModelState.AddModelError("Email", "Клієнт із такою електронною поштою вже зареєстрований.");
        }

        if (ModelState.IsValid)
        {
            client.IsActive = true;
            _context.Add(client);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(client);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null) return NotFound();
        var client = await _context.Clients.FindAsync(id);
        if (client is null) return NotFound();

        return View(client);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Client client)
    {
        if (id != client.Clientid) return NotFound();
        bool emailExists = await _context.Clients
            .AnyAsync(c => c.Email == client.Email && c.Clientid != id);
        if (emailExists)
        {
            ModelState.AddModelError("Email", "Цей Email уже використовується іншим клієнтом.");
        }
        if (ModelState.IsValid)
        {
            try
            {
                client.IsActive = true;
                _context.Update(client);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (await _context.Clients.FindAsync(id) is null) return NotFound();
                else throw;
            }
        }
        return View(client);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var client = await _context.Clients.FindAsync(id);
        if (client is null) return NotFound();

        client.IsActive = false;
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id)
    {
        var client = await _context.Clients.FindAsync(id);
        if (client is not null)
        {
            client.IsActive = true;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index), new { status = "archived" });
    }
}