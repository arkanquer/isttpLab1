using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HotelBookingSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace HotelBookingSystem.Controllers;

[Authorize]
public class BookingsController : Controller
{
    private readonly HotelDbContext _context;
    private readonly UserManager<User> _userManager;

    public BookingsController(HotelDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Index(string searchString, DateTime? arrivalDate)
    {
        ViewData["CurrentFilter"] = searchString;
        ViewData["ArrivalFilter"] = arrivalDate?.ToString("yyyy-MM-dd");

        var query = _context.Bookings
            .Include(b => b.Client)
            .Include(b => b.Room)
            .Include(b => b.Employee)
            .Where(b => b.Status != "Cancelled")
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchString))
        {
            string s = $"%{searchString.Trim()}%";
            query = query.Where(b => b.Client != null && b.Client.Fullname != null && EF.Functions.ILike(b.Client.Fullname, s));
        }

        if (arrivalDate.HasValue)
        {
            var dateVal = arrivalDate.Value.Date;
            query = query.Where(b => b.Checkindate == dateVal);
        }
        var result = await query.OrderByDescending(b => b.Bookingid).ToListAsync();
        return View(result);
    }

    public async Task<IActionResult> MyBookings()
    {
        var userId = _userManager.GetUserId(User);
        var bookings = await _context.Bookings
            .Include(b => b.Room)
            .Include(b => b.BookingServices).ThenInclude(bs => bs.Service)
            .Where(b => b.Client!.UserId == userId)
            .OrderByDescending(b => b.Createdat)
            .ToListAsync();
        return View(bookings);
    }

    public async Task<IActionResult> Create(int? roomId)
    {
        var booking = new Booking();
        if (roomId.HasValue) booking.Roomid = roomId.Value;

        if (!User.IsInRole("admin"))
        {
            var userId = _userManager.GetUserId(User);
            var client = await _context.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
            if (client != null) booking.Clientid = client.Clientid;
        }

        await PopulateDropDownLists(booking);
        return View(booking);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Booking booking)
    {
        if (!User.IsInRole("admin"))
        {
            var userId = _userManager.GetUserId(User);
            var client = await _context.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
            if (client != null) booking.Clientid = client.Clientid;
            booking.Status = "Pending";
            booking.Employeeid = null;
        }
        else
        {
            booking.Status = "Confirmed";
        }

        if (booking.Checkindate >= booking.Checkoutdate)
            ModelState.AddModelError("", "Дата виїзду має бути пізніше дати заїзду.");

        bool isOverlapping = await _context.Bookings.AnyAsync(b =>
            b.Roomid == booking.Roomid &&
            b.Status != "Cancelled" &&
            booking.Checkindate < b.Checkoutdate &&
            booking.Checkoutdate > b.Checkindate);

        if (isOverlapping) ModelState.AddModelError("", "Цей номер уже зайнятий на вибрані дати.");

        if (ModelState.IsValid)
        {
            var room = await _context.Rooms.FindAsync(booking.Roomid);
            if (room != null)
            {
                var days = (int)(booking.Checkoutdate!.Value - booking.Checkindate!.Value).TotalDays;
                if (days < 1) days = 1;

                booking.Roompriceatbooking = room.Pricepernight;
                booking.Totalprice = (decimal)days * (room.Pricepernight ?? 0);
                booking.Createdat = DateTime.Now;

                _context.Add(booking);
                await _context.SaveChangesAsync();
                await SaveHistory(booking.Bookingid, "New", booking.Status!);

                return User.IsInRole("admin") ? RedirectToAction(nameof(Index)) : RedirectToAction(nameof(MyBookings));
            }
        }

        await PopulateDropDownLists(booking);
        return View(booking);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddService(int bookingId, int serviceId)
    {
        var booking = await _context.Bookings
            .Include(b => b.Client)
            .Include(b => b.BookingServices)
            .FirstOrDefaultAsync(b => b.Bookingid == bookingId);
        
        var service = await _context.Services.FindAsync(serviceId);

        if (booking == null || service == null) return NotFound();

        if (!User.IsInRole("admin"))
        {
            var userId = _userManager.GetUserId(User);
            if (booking.Client?.UserId != userId) return Forbid();
        }

        var existingService = booking.BookingServices
            .FirstOrDefault(bs => bs.Serviceid == serviceId);

        if (existingService != null)
        {
            existingService.Quantity += 1;
            _context.Update(existingService);
        }
        else
        {
            var bookingService = new BookingService
            {
                Bookingid = bookingId,
                Serviceid = serviceId,
                Priceatbooking = service.Price ?? 0,
                Quantity = 1
            };
            _context.BookingServices.Add(bookingService);
        }

        booking.Totalprice += service.Price ?? 0;
        await _context.SaveChangesAsync();

        return User.IsInRole("admin") 
            ? RedirectToAction(nameof(Edit), new { id = bookingId }) 
            : RedirectToAction(nameof(MyBookings));
    }

    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null) return NotFound();
        var booking = await _context.Bookings
            .Include(b => b.BookingServices).ThenInclude(bs => bs.Service)
            .Include(b => b.Room)
            .Include(b => b.Client)
            .FirstOrDefaultAsync(b => b.Bookingid == id);
        
        if (booking is null) return NotFound();

        await PopulateDropDownLists(booking);
        return View(booking);
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Booking booking)
    {
        if (id != booking.Bookingid) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                var room = await _context.Rooms.FindAsync(booking.Roomid);
                if (room != null)
                {
                    var days = (int)(booking.Checkoutdate!.Value - booking.Checkindate!.Value).TotalDays;
                    if (days < 1) days = 1;

                    booking.Roompriceatbooking = room.Pricepernight;

                    var servicesSum = await _context.BookingServices
                        .Where(bs => bs.Bookingid == id)
                        .SumAsync(bs => (bs.Priceatbooking * bs.Quantity));

                    booking.Totalprice = ((decimal)days * (room.Pricepernight ?? 0)) + servicesSum;
                }

                var oldBooking = await _context.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Bookingid == id);
                _context.Update(booking);
                if (oldBooking != null && oldBooking.Status != booking.Status)
                {
                    await SaveHistory(id, oldBooking.Status!, booking.Status!);
                }
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BookingExists(id)) return NotFound();
                else throw;
            }
            return RedirectToAction(nameof(Index));
        }

        await PopulateDropDownLists(booking);
        return View(booking);
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var booking = await _context.Bookings
            .Include(b => b.Room)
            .FirstOrDefaultAsync(b => b.Bookingid == id);

        if (booking != null)
        {
            string oldStatus = booking.Status ?? "Unknown";
            booking.Status = "Cancelled";
            await SaveHistory(id, oldStatus, "Cancelled");
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropDownLists(Booking? booking = null)
    {
        var clientsQuery = _context.Clients.Where(c => c.IsActive == true || (booking != null && c.Clientid == booking.Clientid));
        ViewBag.ClientIdList = new SelectList(await clientsQuery.OrderBy(c => c.Fullname).ToListAsync(), "Clientid", "Fullname", booking?.Clientid);

        var employeesQuery = _context.Employees.Where(e => (e.Position != "Звільнений" && e.Position != "fired") || (booking != null && e.Employeeid == booking.Employeeid));
        ViewBag.EmployeeIdList = new SelectList(await employeesQuery.OrderBy(e => e.Fullname).ToListAsync(), "Employeeid", "Fullname", booking?.Employeeid);

        var roomsQuery = _context.Rooms.Where(r => (r.Status != "Maintenance" && r.Status != "archived") || (booking != null && r.Roomid == booking.Roomid));
        ViewBag.RoomIdList = new SelectList(await roomsQuery.OrderBy(r => r.Roomnumber).ToListAsync(), "Roomid", "Roomnumber", booking?.Roomid);
    }

    private async Task SaveHistory(int bookingId, string oldStatus, string newStatus)
    {
        var history = new BookingHistory { Bookingid = bookingId, Oldstatus = oldStatus, Newstatus = newStatus, Changeddate = DateTime.Now };
        _context.BookingHistories.Add(history);
    }

    private bool BookingExists(int id) => _context.Bookings.Any(e => e.Bookingid == id);
}