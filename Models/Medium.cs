using System;
using System.Collections.Generic;

namespace HotelBookingSystem.Models;

public partial class Medium
{
    public int Mediaid { get; set; }

    public string? Url { get; set; }

    public string? Filetype { get; set; }

    public string? Entitytype { get; set; }

    public int? Entityid { get; set; }

    public bool? Isprimary { get; set; }

    public DateTime? Createdat { get; set; }

    public DateTime? Updatedat { get; set; }
}
