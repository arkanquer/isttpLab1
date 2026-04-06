using System;
using System.Collections.Generic;

namespace HotelBookingSystem.Models;

public partial class Client
{
    public int Clientid { get; set; }

    public string? Fullname { get; set; }

    public string? Email { get; set; }

    public string? Status { get; set; }

    public string? Phonenumber { get; set; }

    public DateTime? Createdat { get; set; }

    public DateTime? Updatedat { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
