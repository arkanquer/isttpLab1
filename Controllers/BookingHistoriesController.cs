using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelBookingSystem.Models;

namespace HotelBookingSystem.Controllers;

public class BookingHistoriesController : Controller
{
    private readonly HotelDbContext _context;

    public BookingHistoriesController(HotelDbContext context) => _context = context;

    public async Task<IActionResult> Index(int? bookingId)
    {
        var query = _context.BookingHistories.Include(h => h.Booking).ThenInclude(b => b!.Client).OrderByDescending(h => h.Changeddate);

        if (bookingId.HasValue)
        {
            ViewBag.BookingId = bookingId;
            return View(await query.Where(h => h.Bookingid == bookingId).ToListAsync());
        }
        return View(await query.ToListAsync());
    }
}