using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelBookingSystem.Models;
using Microsoft.AspNetCore.Authorization;

namespace HotelBookingSystem.Controllers;

[Authorize]
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
        var roomIds = rooms.Select(r => (int?)r.Roomid).ToList();
        ViewBag.Photos = await _context.Media
            .Where(m => m.Entitytype == "Room" && roomIds.Contains(m.Entityid))
            .ToListAsync();

        return View(rooms);
    }

    [Authorize(Roles = "admin")]
    public IActionResult Create() => View();

    [HttpPost]
    [Authorize(Roles = "admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Room room)
    {
        bool exists = await _context.Rooms.AnyAsync(r => r.Roomnumber == room.Roomnumber);
        if (exists) ModelState.AddModelError("Roomnumber", "Кімната з таким номером уже існує.");

        if (ModelState.IsValid)
        {
            _context.Add(room);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(room);
    }

    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null) return NotFound();
        var room = await _context.Rooms.FindAsync(id);
        if (room is null) return NotFound();
        return View(room);
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Room room)
    {
        if (id != room.Roomid) return NotFound();

        bool exists = await _context.Rooms.AnyAsync(r => r.Roomnumber == room.Roomnumber && r.Roomid != id);
        if (exists) ModelState.AddModelError("Roomnumber", "Такий номер уже використовується.");

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
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> UploadPhoto(int roomId, IFormFile photo)
    {
        if (photo is null || photo.Length == 0) return RedirectToAction(nameof(Index));

        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(photo.FileName);
        string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/rooms", fileName);
        var directoryName = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directoryName))
        {
            Directory.CreateDirectory(directoryName);
        }

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await photo.CopyToAsync(stream);
        }

        var existingMedia = await _context.Media
            .FirstOrDefaultAsync(m => m.Entityid == roomId && m.Entitytype == "Room");

        if (existingMedia is not null)
        {
            var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingMedia.Url?.TrimStart('/') ?? string.Empty);
            if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
            existingMedia.Url = "/images/rooms/" + fileName;
            existingMedia.Updatedat = DateTime.Now;
        }
        else
        {
            _context.Media.Add(new Medium
            {
                Url = "/images/rooms/" + fileName,
                Entitytype = "Room",
                Entityid = roomId,
                Filetype = Path.GetExtension(photo.FileName),
                Isprimary = true,
                Createdat = DateTime.Now,
                Updatedat = DateTime.Now
            });
        }
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> DeletePhoto(int roomId)
    {
        var media = await _context.Media
            .FirstOrDefaultAsync(m => m.Entityid == roomId && m.Entitytype == "Room");

        if (media is not null)
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", media.Url?.TrimStart('/') ?? string.Empty);
            if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath);
            _context.Media.Remove(media);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}