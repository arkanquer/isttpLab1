using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelBookingSystem.Models;
using System.Globalization;

namespace HotelBookingSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChartsController : ControllerBase
    {
        private readonly HotelDbContext _context;

        public ChartsController(HotelDbContext context)
        {
            _context = context;
        }

        private record ChartDataItem(string Label, decimal Count);

        [HttpGet("revenueByRoomType")]
        public async Task<JsonResult> GetRevenueByRoomType()
        {
            var data = await _context.Bookings
                .Include(b => b.Room)
                .Where(b => b.Status != "Cancelled" && b.Totalprice != null && b.Room != null)
                .GroupBy(b => b.Room!.Roomtype)
                .Select(g => new ChartDataItem(
                    g.Key ?? "Інше",
                    (decimal)g.Sum(b => b.Totalprice ?? 0)))
                .ToListAsync();

            return new JsonResult(data);
        }

        [HttpGet("revenueByMonth")]
        public async Task<JsonResult> GetRevenueByMonth()
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

            return new JsonResult(data);
        }
    }
}