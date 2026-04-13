using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelBookingSystem.Models;

namespace HotelBookingSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly HotelDbContext _context;

        public HomeController(HotelDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.RoomsCount = await _context.Rooms.CountAsync();
            ViewBag.ActiveBookings = await _context.Bookings.Where(b => b.Status == "Confirmed").CountAsync();
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}