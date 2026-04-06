using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelBookingSystem.Models;

namespace HotelBookingSystem.Controllers;

public class RoomsController : Controller
{
    private readonly HotelDbContext _context;

    public RoomsController(HotelDbContext context) => _context = context;

    public async Task<IActionResult> Index(string status = "active")
    {
        ViewData["CurrentStatus"] = status;
        var query = _context.Rooms.AsQueryable();

        if (status == "archived")
        {
            query = query.Where(r => r.Status == "archived");
        }
        else
        {
            query = query.Where(r => r.Status != "archived");
        }

        var rooms = await query.OrderBy(r => r.Roomnumber).ToListAsync();
        return View(rooms);
    }

    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Room room)
    {
        bool exists = await _context.Rooms.AnyAsync(r => r.Roomnumber == room.Roomnumber);

        if (exists)
        {
            ModelState.AddModelError("Roomnumber", "Кімната з таким номером уже існує.");
        }

        if (ModelState.IsValid)
        {
            _context.Add(room);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(room);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null) return NotFound();
        var room = await _context.Rooms.FindAsync(id);
        if (room is null) return NotFound();
        return View(room);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Room room)
    {
        if (id != room.Roomid) return NotFound();

        bool exists = await _context.Rooms.AnyAsync(r => r.Roomnumber == room.Roomnumber && r.Roomid != id);
        if (exists)
        {
            ModelState.AddModelError("Roomnumber", "Такий номер уже використовується.");
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(room);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (await _context.Rooms.FindAsync(id) is null) return NotFound();
                else throw;
            }
        }
        return View(room);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(int id)
    {
        var room = await _context.Rooms.FindAsync(id);
        if (room is null) return NotFound();
        room.Status = "archived";
        _context.Update(room);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(int id)
    {
        var room = await _context.Rooms.FindAsync(id);
        if (room is null) return NotFound();
        room.Status = "available";
        _context.Update(room);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}