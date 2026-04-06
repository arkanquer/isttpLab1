using System;
using System.Collections.Generic;

namespace HotelBookingSystem.Models;

public partial class Booking
{
    public int Bookingid { get; set; }

    public int? Clientid { get; set; }

    public int? Roomid { get; set; }

    public int? Employeeid { get; set; }

    public DateTime? Checkindate { get; set; }

    public DateTime? Checkoutdate { get; set; }

    public decimal? Roompriceatbooking { get; set; }

    public decimal? Totalprice { get; set; }

    public string? Status { get; set; }

    public DateTime? Createdat { get; set; }

    public DateTime? Updatedat { get; set; }

    public virtual ICollection<BookingHistory> BookingHistories { get; set; } = new List<BookingHistory>();

    public virtual ICollection<BookingService> BookingServices { get; set; } = new List<BookingService>();

    public virtual Client? Client { get; set; }

    public virtual Employee? Employee { get; set; }

    public virtual Room? Room { get; set; }
}
