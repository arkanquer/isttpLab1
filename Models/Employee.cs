using System;
using System.Collections.Generic;

namespace HotelBookingSystem.Models;

public partial class Employee
{
    public bool IsActive { get; set; } = true;
    public string? UserId { get; set; }
    public int Employeeid { get; set; }

    public string? Fullname { get; set; }

    public string? Email { get; set; }

    public string? Phonenumber { get; set; }

    public string? Position { get; set; }

    public decimal? Salary { get; set; }

    public DateTime? Createdat { get; set; }

    public DateTime? Updatedat { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
