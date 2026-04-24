using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HotelBookingSystem.Models;
using HotelBookingSystem.ViewModels;
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
            .Where(b => b.Client != null && b.Client.UserId == userId)
            .OrderByDescending(b => b.Createdat)
            .ToListAsync();
        return View(bookings);
    }

    public async Task<IActionResult> Create(int? roomId)
    {
        if (roomId is null) return NotFound();

        var services = await _context.Services.Where(s => s.IsAvailable == true).ToListAsync();
        
        var model = new CreateBookingViewModel
        {
            RoomId = roomId.Value,
            CheckInDate = DateTime.Now,
            CheckOutDate = DateTime.Now.AddDays(1),
            AvailableServices = services.Select(s => new ServiceSelectionViewModel
            {
                ServiceId = s.Serviceid,
                Name = s.Name ?? "Без назви",
                Price = s.Price ?? 0
            }).ToList()
        };

        if (User.IsInRole("admin"))
        {
            await PopulateDropDownLists();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateBookingViewModel model)
    {
        if (model.CheckInDate >= model.CheckOutDate)
        {
            ModelState.AddModelError("", "Дата виїзду має бути пізніше дати заїзду.");
        }

        bool isOverlapping = await _context.Bookings.AnyAsync(b =>
            b.Roomid == model.RoomId &&
            b.Status != "Cancelled" &&
            model.CheckInDate < b.Checkoutdate &&
            model.CheckOutDate > b.Checkindate);

        if (isOverlapping)
        {
            ModelState.AddModelError("", "Цей номер уже зайнятий на вибрані дати.");
        }

        if (ModelState.IsValid)
        {
            var room = await _context.Rooms.FindAsync(model.RoomId);
            if (room is not null)
            {
                var booking = new Booking
                {
                    Roomid = model.RoomId,
                    Checkindate = model.CheckInDate,
                    Checkoutdate = model.CheckOutDate,
                    Createdat = DateTime.Now,
                    Roompriceatbooking = room.Pricepernight,
                    Status = User.IsInRole("admin") ? "Confirmed" : "Pending"
                };

                if (!User.IsInRole("admin"))
                {
                    var userId = _userManager.GetUserId(User);
                    var client = await _context.Clients.FirstOrDefaultAsync(c => c.UserId == userId);
                    if (client is not null) booking.Clientid = client.Clientid;
                }

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                decimal servicesSum = 0;
                foreach (var s in model.AvailableServices.Where(x => x.IsSelected))
                {
                    var bs = new BookingService
                    {
                        Bookingid = booking.Bookingid,
                        Serviceid = s.ServiceId,
                        Quantity = s.Quantity > 0 ? s.Quantity : 1,
                        Priceatbooking = s.Price
                    };
                    _context.BookingServices.Add(bs);
                    // ВИПРАВЛЕНО: Додано перевірку на null для Quantity
                    servicesSum += s.Price * (bs.Quantity ?? 1);
                }

                var days = (int)(booking.Checkoutdate.Value - booking.Checkindate.Value).TotalDays;
                if (days < 1) days = 1;
                
                // ВИПРАВЛЕНО: Розрахунок підсумкової суми
                booking.Totalprice = ((decimal)days * (room.Pricepernight ?? 0)) + servicesSum;

                await _context.SaveChangesAsync();
                await SaveHistory(booking.Bookingid, "New", booking.Status ?? "Pending");

                return User.IsInRole("admin") ? RedirectToAction(nameof(Index)) : RedirectToAction(nameof(MyBookings));
            }
        }

        if (User.IsInRole("admin")) await PopulateDropDownLists();
        return View(model);
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
                if (room is not null)
                {
                    var days = (int)(booking.Checkoutdate!.Value - booking.Checkindate!.Value).TotalDays;
                    if (days < 1) days = 1;

                    booking.Roompriceatbooking = room.Pricepernight;

                    var servicesSum = await _context.BookingServices
                        .Where(bs => bs.Bookingid == id)
                        .SumAsync(bs => (bs.Priceatbooking ?? 0) * (bs.Quantity ?? 0));

                    booking.Totalprice = ((decimal)days * (room.Pricepernight ?? 0)) + servicesSum;
                }

                var oldBooking = await _context.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Bookingid == id);
                _context.Update(booking);
                if (oldBooking is not null && oldBooking.Status != booking.Status)
                {
                    await SaveHistory(id, oldBooking.Status ?? "Unknown", booking.Status ?? "Unknown");
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
        var booking = await _context.Bookings.FindAsync(id);
        if (booking is not null)
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

        var employeesQuery = _context.Employees.Where(e => e.Position != "fired" || (booking != null && e.Employeeid == booking.Employeeid));
        ViewBag.EmployeeIdList = new SelectList(await employeesQuery.OrderBy(e => e.Fullname).ToListAsync(), "Employeeid", "Fullname", booking?.Employeeid);

        var roomsQuery = _context.Rooms.Where(r => r.Status != "archived" || (booking != null && r.Roomid == booking.Roomid));
        ViewBag.RoomIdList = new SelectList(await roomsQuery.OrderBy(r => r.Roomnumber).ToListAsync(), "Roomid", "Roomnumber", booking?.Roomid);
    }

    private async Task SaveHistory(int bookingId, string oldStatus, string newStatus)
    {
        var history = new BookingHistory { Bookingid = bookingId, Oldstatus = oldStatus, Newstatus = newStatus, Changeddate = DateTime.Now };
        _context.BookingHistories.Add(history);
    }

    private bool BookingExists(int id) => _context.Bookings.Any(e => e.Bookingid == id);
}