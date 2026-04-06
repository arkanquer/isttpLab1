using System;
using System.Collections.Generic;

namespace HotelBookingSystem.Models;

public partial class BookingHistory
{
    public int Historyid { get; set; }

    public int? Bookingid { get; set; }

    public string? Oldstatus { get; set; }

    public string? Newstatus { get; set; }

    public DateTime? Changeddate { get; set; }

    public virtual Booking? Booking { get; set; }
}
