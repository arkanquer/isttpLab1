namespace HotelBookingSystem.ViewModels;

public class CreateBookingViewModel
{
    public int RoomId { get; set; }
    
    public DateTime CheckInDate { get; set; } = DateTime.Now;
    public DateTime CheckOutDate { get; set; } = DateTime.Now.AddDays(1);
    
    public List<ServiceSelectionViewModel> AvailableServices { get; set; } = new();
}