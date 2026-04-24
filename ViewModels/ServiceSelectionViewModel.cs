namespace HotelBookingSystem.ViewModels;

public class ServiceSelectionViewModel
{
    public int ServiceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    
    public bool IsSelected { get; set; }
    
    public int Quantity { get; set; } = 1;
}