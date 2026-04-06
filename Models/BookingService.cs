using System;
using System.Collections.Generic;

namespace HotelBookingSystem.Models;

public partial class BookingService
{
    public int Bookingid { get; set; }

    public int Serviceid { get; set; }

    public int? Quantity { get; set; }

    public decimal? Priceatbooking { get; set; }

    public virtual Booking Booking { get; set; } = null!;

    public virtual Service Service { get; set; } = null!;
}
