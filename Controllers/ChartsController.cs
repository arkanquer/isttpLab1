using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelBookingSystem.Models;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;

namespace HotelBookingSystem.Controllers
{
    [Authorize(Roles = "admin")]
    public class ChartsController : Controller
    {
        private readonly HotelDbContext _context;

        public ChartsController(HotelDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        private record ChartDataItem(string Label, decimal Count);

        [HttpGet("api/charts/revenueByRoomType")]
        public async Task<IActionResult> GetRevenueByRoomType()
        {
            var data = await _context.Bookings
                .Include(b => b.Room)
                .Where(b => b.Status != "Cancelled" && b.Totalprice != null && b.Room != null)
                .GroupBy(b => b.Room!.Roomtype)
                .Select(g => new ChartDataItem(
                    g.Key ?? "Інше",
                    (decimal)g.Sum(b => b.Totalprice ?? 0)))
                .ToListAsync();
            return Json(data);
        }

        [HttpGet("api/charts/revenueByMonth")]
        public async Task<IActionResult> GetRevenueByMonth()
        {
            var bookings = await _context.Bookings
                .Where(b => b.Checkindate != null && b.Status != "Cancelled" && b.Totalprice != null)
                .ToListAsync();

            var data = bookings
                .GroupBy(b => new { b.Checkindate!.Value.Year, b.Checkindate!.Value.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new ChartDataItem(
                    $"{CultureInfo.GetCultureInfo("uk-UA").DateTimeFormat.GetMonthName(g.Key.Month)} {g.Key.Year}",
                    (decimal)g.Sum(b => b.Totalprice ?? 0)))
                .ToList();
            return Json(data);
        }
    }
}