using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HotelBookingSystem.Models;

namespace HotelBookingSystem.Controllers;

public class BookingsController : Controller
{
    private readonly HotelDbContext _context;

    public BookingsController(HotelDbContext context) => _context = context;

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

    public async Task<IActionResult> Create()
    {
        await PopulateDropDownLists();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Booking booking)
    {
        if (booking.Checkindate >= booking.Checkoutdate)
        {
            ModelState.AddModelError("", "Дата виїзду має бути пізніше дати заїзду.");
        }

        bool isOverlapping = await _context.Bookings.AnyAsync(b =>
            b.Roomid == booking.Roomid &&
            b.Status != "Cancelled" &&
            booking.Checkindate < b.Checkoutdate &&
            booking.Checkoutdate > b.Checkindate);

        if (isOverlapping)
        {
            ModelState.AddModelError("", "Цей номер уже зайнятий на вибрані дати іншим клієнтом.");
        }

        if (ModelState.IsValid)
        {
            var room = await _context.Rooms.FindAsync(booking.Roomid);
            if (room != null)
            {
                var days = (int)(booking.Checkoutdate!.Value - booking.Checkindate!.Value).TotalDays;
                booking.Roompriceatbooking = room.Pricepernight;
                booking.Totalprice = (decimal)(days < 1 ? 1 : days) * (room.Pricepernight ?? 0);

                booking.Status = "Confirmed";
                booking.Createdat = DateTime.Now;

                _context.Add(booking);
                await _context.SaveChangesAsync();
                await SaveHistory(booking.Bookingid, "New", "Confirmed");

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
        }

        await PopulateDropDownLists(booking);
        return View(booking);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null) return NotFound();
        var booking = await _context.Bookings.FindAsync(id);
        if (booking is null) return NotFound();

        await PopulateDropDownLists(booking);
        return View(booking);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Booking booking)
    {
        if (id != booking.Bookingid) return NotFound();

        bool isOverlapping = await _context.Bookings.AnyAsync(b =>
            b.Roomid == booking.Roomid &&
            b.Bookingid != id &&
            b.Status != "Cancelled" &&
            booking.Checkindate < b.Checkoutdate &&
            booking.Checkoutdate > b.Checkindate);

        if (isOverlapping)
        {
            ModelState.AddModelError("", "Неможливо змінити: номер уже заброньовано на ці дати.");
        }

        if (ModelState.IsValid)
        {
            try
            {
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
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var booking = await _context.Bookings
            .Include(b => b.Room)
            .FirstOrDefaultAsync(b => b.Bookingid == id);

        if (booking != null)
        {
            if (booking.Status == "Cancelled")
            {
                return RedirectToAction(nameof(Index));
            }
            string oldStatus = booking.Status ?? "Unknown";
            booking.Status = "Cancelled";
            await SaveHistory(id, oldStatus, "Cancelled");
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropDownLists(Booking? booking = null)
    {
        var clientsQuery = _context.Clients.AsQueryable();
        if (booking is null)
        {
            clientsQuery = clientsQuery.Where(c => c.IsActive == true);
        }
        else
        {
            clientsQuery = clientsQuery.Where(c => c.IsActive == true || c.Clientid == booking.Clientid);
        }
        ViewBag.ClientIdList = new SelectList(await clientsQuery.OrderBy(c => c.Fullname).ToListAsync(), "Clientid", "Fullname", booking?.Clientid);

        var employeesQuery = _context.Employees.AsQueryable();
        if (booking is null)
        {
            employeesQuery = employeesQuery.Where(e => e.Position != "fired");
        }
        else
        {
            employeesQuery = employeesQuery.Where(e => e.Position != "fired" || e.Employeeid == booking.Employeeid);
        }
        ViewBag.EmployeeIdList = new SelectList(await employeesQuery.OrderBy(e => e.Fullname).ToListAsync(), "Employeeid", "Fullname", booking?.Employeeid);

        var roomsQuery = _context.Rooms.AsQueryable();
        if (booking is null)
        {
            roomsQuery = roomsQuery.Where(r => r.Status != "Maintenance" && r.Status != "archived");
        }
        else
        {
            roomsQuery = roomsQuery.Where(r => (r.Status != "Maintenance" && r.Status != "archived") || r.Roomid == booking.Roomid);
        }
        ViewBag.RoomIdList = new SelectList(await roomsQuery.OrderBy(r => r.Roomnumber).ToListAsync(), "Roomid", "Roomnumber", booking?.Roomid);
    }
    private async Task SaveHistory(int bookingId, string oldStatus, string newStatus)
    {
        var history = new BookingHistory
        {
            Bookingid = bookingId,
            Oldstatus = oldStatus,
            Newstatus = newStatus,
            Changeddate = DateTime.Now
        };
        _context.BookingHistories.Add(history);
    }

    private bool BookingExists(int id) => _context.Bookings.Any(e => e.Bookingid == id);
}