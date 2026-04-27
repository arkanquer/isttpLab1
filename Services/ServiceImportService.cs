using ClosedXML.Excel;
using HotelBookingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingSystem.Services;

public class ServiceImportService : IImportService<Service>
{
    private readonly HotelDbContext _context;
    public ServiceImportService(HotelDbContext context) => _context = context;

    public async Task ImportFromStreamAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (!stream.CanRead) throw new ArgumentException("Дані не можуть бути прочитані");

        using var workBook = new XLWorkbook(stream);
        foreach (var worksheet in workBook.Worksheets)
        {
            foreach (var row in worksheet.RowsUsed().Skip(1))
            {
                var name = row.Cell(1).Value.ToString().Trim();

                if (string.IsNullOrWhiteSpace(name)) continue;

                var service = await _context.Services.FirstOrDefaultAsync(s => 
                s.Name != null && s.Name.ToLower() == name.ToLower(), cancellationToken);
                
                if (service is null)
                {
                    service = new Service 
                    { 
                        Name = name,
                        Price = decimal.TryParse(row.Cell(2).Value.ToString(), out var p) ? p : 0,
                        Description = row.Cell(3).Value.ToString().Trim(),
                        IsAvailable = true,
                        Createdat = DateTime.Now
                    };
                    _context.Services.Add(service);
                }
                else
                {
                    if (decimal.TryParse(row.Cell(2).Value.ToString(), out var newPrice))
                    {
                        service.Price = newPrice;
                    }
                    
                    service.Description = row.Cell(3).Value.ToString().Trim();
                    
                    _context.Services.Update(service);
                }
            }
        }
        
        await _context.SaveChangesAsync(cancellationToken);
    }
}