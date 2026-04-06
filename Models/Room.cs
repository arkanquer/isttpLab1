using System;
using System.Collections.Generic;

namespace HotelBookingSystem.Models;

public partial class Room
{
    public int Roomid { get; set; }

    public string? Roomtype { get; set; }

    public int? Capacity { get; set; }

    public decimal? Pricepernight { get; set; }

    public string? Status { get; set; }

    public string? Roomnumber { get; set; }

    public DateTime? Createdat { get; set; }

    public DateTime? Updatedat { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
