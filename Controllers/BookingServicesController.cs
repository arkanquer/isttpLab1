using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HotelBookingSystem.Models;

namespace HotelBookingSystem.Controllers;

public class BookingServicesController : Controller
{
    private readonly HotelDbContext _context;
    public BookingServicesController(HotelDbContext context) => _context = context;
    public async Task<IActionResult> Index(int bookingId)
    {
        var booking = await _context.Bookings.Include(b => b.Client).Include(b => b.Room)
            .Include(b => b.BookingServices).ThenInclude(bs => bs.Service)
            .FirstOrDefaultAsync(b => b.Bookingid == bookingId);

        if (booking is null)
        {
            return NotFound();
        }
        ViewBag.ServiceList = new SelectList(await _context.Services
            .Where(s => s.IsAvailable == true)
            .ToListAsync(), "Serviceid", "Name");

        return View(booking);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveService(int bookingId, int serviceId)
    {
        var bs = await _context.BookingServices
            .Include(bs => bs.Booking)
            .FirstOrDefaultAsync(bs => bs.Bookingid == bookingId && bs.Serviceid == serviceId);
        if (bs is not null)
        {
            if (bs.Booking is not null)
            {
                decimal amountToRemove = (bs.Priceatbooking ?? 0) * (bs.Quantity ?? 1);
                bs.Booking.Totalprice = (bs.Booking.Totalprice ?? 0) - amountToRemove;
                if (bs.Booking.Totalprice < 0)
                    bs.Booking.Totalprice = 0;
            }
            _context.BookingServices.Remove(bs);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index), new { bookingId });
    }
    private async Task RecalculateTotal(int bookingId)
    {
        var booking = await _context.Bookings
            .Include(b => b.BookingServices)
            .FirstOrDefaultAsync(b => b.Bookingid == bookingId);

        if (booking is not null)
        {
            var checkIn = booking.Checkindate ?? DateTime.Now;
            var checkOut = booking.Checkoutdate ?? DateTime.Now.AddDays(1);
            var days = (int)(checkOut - checkIn).TotalDays;
            if (days < 1) days = 1;
            decimal stayTotal = (booking.Roompriceatbooking ?? 0) * days;
            decimal servicesTotal = booking.BookingServices.Sum(s => (s.Priceatbooking ?? 0) * (s.Quantity ?? 1));
            booking.Totalprice = stayTotal + servicesTotal;
            await _context.SaveChangesAsync();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddService(int bookingId, int serviceId, int quantity)
    {
        if (quantity <= 0) quantity = 1;
        var booking = await _context.Bookings.FindAsync(bookingId);
        var service = await _context.Services.FindAsync(serviceId);

        if (booking is null ||
            booking.Status?.ToLower() == "cancelled" ||
            booking.Status?.ToLower() == "checkedout")
        {
            return RedirectToAction("Index", "Services");
        }

        if (service is not null)
        {
            var existingBS = await _context.BookingServices
                .FirstOrDefaultAsync(bs => bs.Bookingid == bookingId && bs.Serviceid == serviceId);

            if (existingBS is not null)
            {
                existingBS.Quantity = (existingBS.Quantity ?? 0) + quantity;
            }
            else
            {
                var bs = new BookingService
                {
                    Bookingid = bookingId,
                    Serviceid = serviceId,
                    Quantity = quantity,
                    Priceatbooking = service.Price
                };
                _context.BookingServices.Add(bs);
            }
            decimal serviceSum = (service.Price ?? 0) * quantity;
            booking.Totalprice = (booking.Totalprice ?? 0) + serviceSum;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index), new { bookingId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateQuantity(int bookingId, int serviceId, int delta)
    {
        var bs = await _context.BookingServices
            .Include(x => x.Booking)
            .Include(x => x.Service)
            .FirstOrDefaultAsync(x => x.Bookingid == bookingId && x.Serviceid == serviceId);

        if (bs is not null && bs.Booking is not null)
        {
            if (bs.Booking.Status == "CheckedOut" || bs.Booking.Status == "Cancelled")
            {
                return RedirectToAction(nameof(Index), new { bookingId });
            }

            int newQuantity = (bs.Quantity ?? 1) + delta;
            if (newQuantity > 0)
            {
                decimal priceChange = (bs.Priceatbooking ?? 0) * delta;
                bs.Quantity = newQuantity;
                bs.Booking.Totalprice = (bs.Booking.Totalprice ?? 0) + priceChange;
                await _context.SaveChangesAsync();
            }
        }
        return RedirectToAction(nameof(Index), new { bookingId });
    }
}