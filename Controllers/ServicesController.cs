using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HotelBookingSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using HotelBookingSystem.Services;

namespace HotelBookingSystem.Controllers;

public class ServicesController : Controller
{
    private readonly HotelDbContext _context;
    private readonly IWebHostEnvironment _hostEnvironment;
    private readonly UserManager<User> _userManager;

    public ServicesController(HotelDbContext context, IWebHostEnvironment hostEnvironment, UserManager<User> userManager)
    {
        _context = context;
        _hostEnvironment = hostEnvironment;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string status = "available")
    {
        ViewData["CurrentStatus"] = status;

        var query = _context.Services.AsQueryable();

        if (status == "archived")
        {
            query = query.Where(s => s.IsAvailable == false);
        }
        else
        {
            query = query.Where(s => s.IsAvailable == true);
        }

        ViewBag.Photos = await _context.Media
            .Where(m => m.Entitytype == "Service")
            .ToListAsync();

        if (User.Identity?.IsAuthenticated == true && !User.IsInRole("admin"))
        {
            var userId = _userManager.GetUserId(User);
            ViewBag.UserBookings = await _context.Bookings
                .Include(b => b.Client)
                .Where(b => b.Client != null && b.Client.UserId == userId)
                .Where(b => b.Status != null &&
                            b.Status.ToLower() != "cancelled" &&
                            b.Status.ToLower() != "checkedout")
                .Where(b => b.Checkoutdate >= DateTime.Now.Date)
                .OrderByDescending(b => b.Checkindate)
                .ToListAsync();
        }

        var services = await query.OrderBy(s => s.Name).ToListAsync();
        return View(services);
    }

    [Authorize(Roles = "admin")]
    [HttpPost]
    public async Task<IActionResult> UploadPhoto(int serviceId, IFormFile photo)
    {
        if (photo is not null && photo.Length > 0)
        {
            string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "images", "services");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            string uniqueFileName = Guid.NewGuid().ToString() + "_" + photo.FileName;
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await photo.CopyToAsync(fileStream);
            }

            var oldPhoto = await _context.Media
                .FirstOrDefaultAsync(m => m.Entityid == serviceId && m.Entitytype == "Service");
            if (oldPhoto is not null) _context.Media.Remove(oldPhoto);

            var media = new Medium
            {
                Entityid = serviceId,
                Entitytype = "Service",
                Url = "/images/services/" + uniqueFileName,
                Createdat = DateTime.Now
            };

            _context.Media.Add(media);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "admin")]
    public IActionResult Create() => View();

    [Authorize(Roles = "admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Service service)
    {
        if (ModelState.IsValid)
        {
            service.IsAvailable = true;
            service.Createdat = DateTime.Now;
            _context.Add(service);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(service);
    }

    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null) return NotFound();
        var service = await _context.Services.FindAsync(id);
        if (service is null) return NotFound();
        return View(service);
    }

    [Authorize(Roles = "admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Service service)
    {
        if (id != service.Serviceid) return NotFound();

        if (ModelState.IsValid)
        {
            service.Updatedat = DateTime.Now;
            _context.Update(service);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(service);
    }

    [Authorize(Roles = "admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var service = await _context.Services.FindAsync(id);
        if (service is not null)
        {
            service.IsAvailable = false;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index), new { status = "available" });
    }

    [Authorize(Roles = "admin")]
    [HttpPost]
    public async Task<IActionResult> Restore(int id)
    {
        var service = await _context.Services.FindAsync(id);
        if (service is not null)
        {
            service.IsAvailable = true;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index), new { status = "archived" });
    }

    [HttpGet]
    public IActionResult Import() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(IFormFile fileExcel, CancellationToken cancellationToken)
    {
        if (fileExcel is not null && fileExcel.ContentType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
        {
            using var stream = fileExcel.OpenReadStream();
            var importService = new ServiceImportService(_context);
            await importService.ImportFromStreamAsync(stream, cancellationToken);
            return RedirectToAction(nameof(Index));
        }
        ModelState.AddModelError("", "Будь ласка, завантажте валідний Excel-файл (.xlsx)");
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Export(CancellationToken cancellationToken)
    {
        string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        var factory = new ServiceDataPortServiceFactory(_context);
        var exportService = factory.GetExportService(contentType);

        var memoryStream = new MemoryStream();
        await exportService.WriteToAsync(memoryStream, cancellationToken);

        memoryStream.Position = 0;
        return new FileStreamResult(memoryStream, contentType)
        {
            FileDownloadName = $"hotel_services_{DateTime.Now:yyyyMMdd}.xlsx"
        };
    }
}