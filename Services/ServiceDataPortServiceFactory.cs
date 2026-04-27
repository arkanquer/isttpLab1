using HotelBookingSystem.Models;

namespace HotelBookingSystem.Services;

public class ServiceDataPortServiceFactory : IDataPortServiceFactory<Service>
{
    private readonly HotelDbContext _context;
    public ServiceDataPortServiceFactory(HotelDbContext context) => _context = context;

    public IImportService<Service> GetImportService(string contentType)
    {
        if (contentType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            return new ServiceImportService(_context);
        
        throw new NotImplementedException($"Імпорт для {contentType} не реалізовано");
    }

    public IExportService<Service> GetExportService(string contentType)
    {
        if (contentType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            return new ServiceExportService(_context);
            
        throw new NotImplementedException($"Експорт для {contentType} не реалізовано");
    }
}