using System;
using System.Collections.Generic;

namespace HotelBookingSystem.Models;

public partial class Service
{
    public int Serviceid { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public decimal? Price { get; set; }

    public DateTime? Createdat { get; set; }

    public DateTime? Updatedat { get; set; }

    public bool IsAvailable { get; set; } = true;

    public virtual ICollection<BookingService> BookingServices { get; set; } = new List<BookingService>();
}
