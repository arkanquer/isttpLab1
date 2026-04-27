using ClosedXML.Excel;
using HotelBookingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingSystem.Services;

public class ServiceExportService : IExportService<Service>
{
    private readonly HotelDbContext _context;
    private static readonly string[] HeaderNames = { "Назва послуги", "Ціна (грн)", "Опис", "Статус" };

    public ServiceExportService(HotelDbContext context) => _context = context;

    public async Task WriteToAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (!stream.CanWrite) throw new ArgumentException("Потік не підтримує запис");

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Послуги готелю");

        for (int i = 0; i < HeaderNames.Length; i++)
        {
            worksheet.Cell(1, i + 1).Value = HeaderNames[i];
            worksheet.Cell(1, i + 1).Style.Font.Bold = true;
            worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        var services = await _context.Services
            .Where(s => s.IsAvailable)
            .ToListAsync(cancellationToken);

        int rowIndex = 2;
        foreach (var s in services)
        {
            worksheet.Cell(rowIndex, 1).Value = s.Name;
            worksheet.Cell(rowIndex, 2).Value = s.Price ?? 0;
            worksheet.Cell(rowIndex, 3).Value = s.Description ?? "Без опису";
            worksheet.Cell(rowIndex, 4).Value = "Доступна";
            rowIndex++;
        }

        worksheet.Columns().AdjustToContents();
        workbook.SaveAs(stream);
    }
}