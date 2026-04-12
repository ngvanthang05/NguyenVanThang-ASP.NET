// Data/AppDbContext.cs
using Microsoft.EntityFrameworkCore;
using NguyenVanThang_ASP.NET.Models;

namespace NguyenVanThang_ASP.NET.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<BusRoute> BusRoutes { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<Seat> Seats { get; set; }
        public DbSet<Trip> Trips { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Staff> Staffs { get; set; }
        public DbSet<Checkin> Checkins { get; set; }
        public DbSet<Student> Students { get; set; } // giữ lại nếu cần

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // BusRoute -> "Routes" table
            modelBuilder.Entity<BusRoute>().ToTable("Routes");

            // Booking -> Seat (không cascade để tránh conflict)
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Seat)
                .WithMany()
                .HasForeignKey(b => b.SeatId)
                .OnDelete(DeleteBehavior.Restrict);

            // Ticket -> Seat
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Seat)
                .WithMany()
                .HasForeignKey(t => t.SeatId)
                .OnDelete(DeleteBehavior.Restrict);

            // User -> Customer (optional)
            modelBuilder.Entity<User>()
                .HasOne(u => u.Customer)
                .WithMany()
                .HasForeignKey(u => u.CustomerId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // Payment decimal precision
            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Trip>()
                .Property(t => t.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<BusRoute>()
                .Property(r => r.BasePrice)
                .HasPrecision(18, 2);
        }
    }
}