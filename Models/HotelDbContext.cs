using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingSystem.Models;

public partial class HotelDbContext : DbContext
{
    public HotelDbContext()
    {
    }

    public HotelDbContext(DbContextOptions<HotelDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Booking> Bookings { get; set; }
    public virtual DbSet<BookingHistory> BookingHistories { get; set; }
    public virtual DbSet<BookingService> BookingServices { get; set; }
    public virtual DbSet<Client> Clients { get; set; }
    public virtual DbSet<Employee> Employees { get; set; }
    public virtual DbSet<Medium> Media { get; set; }
    public virtual DbSet<Room> Rooms { get; set; }
    public virtual DbSet<Service> Services { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.Bookingid).HasName("bookings_pkey");

            entity.ToTable("bookings");

            entity.Property(e => e.Bookingid).HasColumnName("bookingid");
            entity.Property(e => e.Checkindate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("checkindate");
            entity.Property(e => e.Checkoutdate)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("checkoutdate");
            entity.Property(e => e.Clientid).HasColumnName("clientid");
            entity.Property(e => e.Createdat)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("createdat");
            entity.Property(e => e.Employeeid).HasColumnName("employeeid");
            entity.Property(e => e.Roomid).HasColumnName("roomid");
            entity.Property(e => e.Roompriceatbooking)
                .HasPrecision(10, 2)
                .HasColumnName("roompriceatbooking");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.Totalprice)
                .HasPrecision(10, 2)
                .HasColumnName("totalprice");
            entity.Property(e => e.Updatedat)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updatedat");

            entity.HasOne(d => d.Client).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.Clientid)
                .HasConstraintName("bookings_clientid_fkey");

            entity.HasOne(d => d.Employee).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.Employeeid)
                .HasConstraintName("bookings_employeeid_fkey");

            entity.HasOne(d => d.Room).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.Roomid)
                .HasConstraintName("bookings_roomid_fkey");
        });

        modelBuilder.Entity<BookingHistory>(entity =>
        {
            entity.HasKey(e => e.Historyid).HasName("booking_history_pkey");

            entity.ToTable("booking_history");

            entity.Property(e => e.Historyid).HasColumnName("historyid");
            entity.Property(e => e.Bookingid).HasColumnName("bookingid");
            entity.Property(e => e.Changeddate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("changeddate");
            entity.Property(e => e.Newstatus)
                .HasMaxLength(50)
                .HasColumnName("newstatus");
            entity.Property(e => e.Oldstatus)
                .HasMaxLength(50)
                .HasColumnName("oldstatus");

            entity.HasOne(d => d.Booking).WithMany(p => p.BookingHistories)
                .HasForeignKey(d => d.Bookingid)
                .HasConstraintName("booking_history_bookingid_fkey");
        });

        modelBuilder.Entity<BookingService>(entity =>
        {
            entity.HasKey(e => new { e.Bookingid, e.Serviceid }).HasName("booking_services_pkey");

            entity.ToTable("booking_services");

            entity.Property(e => e.Bookingid).HasColumnName("bookingid");
            entity.Property(e => e.Serviceid).HasColumnName("serviceid");
            entity.Property(e => e.Priceatbooking)
                .HasPrecision(10, 2)
                .HasColumnName("priceatbooking");
            entity.Property(e => e.Quantity)
                .HasDefaultValue(1)
                .HasColumnName("quantity");

            entity.HasOne(d => d.Booking).WithMany(p => p.BookingServices)
                .HasForeignKey(d => d.Bookingid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("booking_services_bookingid_fkey");

            entity.HasOne(d => d.Service).WithMany(p => p.BookingServices)
                .HasForeignKey(d => d.Serviceid)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("booking_services_serviceid_fkey");
        });

        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasKey(e => e.Clientid).HasName("clients_pkey");

            entity.ToTable("clients");

            entity.HasIndex(e => e.Email, "clients_email_key").IsUnique();

            entity.Property(e => e.Clientid).HasColumnName("clientid");
            entity.Property(e => e.Createdat)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("createdat");
            entity.Property(e => e.Email)
                .HasMaxLength(150)
                .HasColumnName("email");
            entity.Property(e => e.Fullname)
                .HasMaxLength(255)
                .HasColumnName("fullname");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Phonenumber)
                .HasMaxLength(20)
                .HasColumnName("phonenumber");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.Updatedat)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updatedat");
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Employeeid).HasName("employees_pkey");

            entity.ToTable("employees");

            entity.HasIndex(e => e.Email, "employees_email_key").IsUnique();

            entity.Property(e => e.Employeeid).HasColumnName("employeeid");
            entity.Property(e => e.Createdat)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("createdat");
            entity.Property(e => e.Email)
                .HasMaxLength(150)
                .HasColumnName("email");
            entity.Property(e => e.Fullname)
                .HasMaxLength(255)
                .HasColumnName("fullname");
            entity.Property(e => e.Phonenumber)
                .HasMaxLength(20)
                .HasColumnName("phonenumber");
            entity.Property(e => e.Position)
                .HasMaxLength(100)
                .HasColumnName("position");
            entity.Property(e => e.Salary)
                .HasPrecision(10, 2)
                .HasColumnName("salary");
            entity.Property(e => e.Updatedat)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updatedat");
        });

        modelBuilder.Entity<Medium>(entity =>
        {
            entity.HasKey(e => e.Mediaid).HasName("media_pkey");

            entity.ToTable("media");

            entity.Property(e => e.Mediaid).HasColumnName("mediaid");
            entity.Property(e => e.Createdat)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("createdat");
            entity.Property(e => e.Entityid).HasColumnName("entityid");
            entity.Property(e => e.Entitytype)
                .HasMaxLength(50)
                .HasColumnName("entitytype");
            entity.Property(e => e.Filetype)
                .HasMaxLength(50)
                .HasColumnName("filetype");
            entity.Property(e => e.Isprimary)
                .HasDefaultValue(false)
                .HasColumnName("isprimary");
            entity.Property(e => e.Updatedat)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updatedat");
            entity.Property(e => e.Url)
                .HasMaxLength(255)
                .HasColumnName("url");
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(e => e.Roomid).HasName("rooms_pkey");

            entity.ToTable("rooms");

            entity.HasIndex(e => e.Roomnumber, "rooms_roomnumber_key").IsUnique();

            entity.Property(e => e.Roomid).HasColumnName("roomid");
            entity.Property(e => e.Capacity).HasColumnName("capacity");
            entity.Property(e => e.Createdat)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("createdat");
            entity.Property(e => e.Pricepernight)
                .HasPrecision(10, 2)
                .HasColumnName("pricepernight");
            entity.Property(e => e.Roomnumber)
                .HasMaxLength(20)
                .HasColumnName("roomnumber");
            entity.Property(e => e.Roomtype)
                .HasMaxLength(100)
                .HasColumnName("roomtype");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.Updatedat)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updatedat");
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasKey(e => e.Serviceid).HasName("services_pkey");

            entity.ToTable("services");

            entity.Property(e => e.Serviceid).HasColumnName("serviceid");
            entity.Property(e => e.Createdat)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("createdat");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsAvailable)
                .HasDefaultValue(true)
                .HasColumnName("is_available");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.Price)
                .HasPrecision(10, 2)
                .HasColumnName("price");
            entity.Property(e => e.Updatedat)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updatedat");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
