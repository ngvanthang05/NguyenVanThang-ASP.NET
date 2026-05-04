// Data/AppDbContext.cs — CẬP NHẬT: thêm Driver, TripAssignment
using Microsoft.EntityFrameworkCore;
using NguyenVanThang_ASP.NET.Models;

namespace NguyenVanThang_ASP.NET.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Core tables
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

        // New tables
        public DbSet<Driver> Drivers { get; set; }
        public DbSet<TripAssignment> TripAssignments { get; set; }

        // Giữ lại nếu cần cho demo
        public DbSet<Student> Students { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ---- Naming ----
            modelBuilder.Entity<BusRoute>().ToTable("Routes");

            // ---- Booking -> Seat (Restrict để tránh multiple cascade) ----
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Seat)
                .WithMany()
                .HasForeignKey(b => b.SeatId)
                .OnDelete(DeleteBehavior.Restrict);

            // ---- Ticket -> Seat ----
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Seat)
                .WithMany()
                .HasForeignKey(t => t.SeatId)
                .OnDelete(DeleteBehavior.Restrict);

            // ---- User -> Customer (optional) ----
            modelBuilder.Entity<User>()
                .HasOne(u => u.Customer)
                .WithMany()
                .HasForeignKey(u => u.CustomerId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // ---- Staff -> User (optional) ----
            modelBuilder.Entity<Staff>()
                .HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // ---- Driver -> User (optional) ----
            modelBuilder.Entity<Driver>()
                .HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // ---- TripAssignment (Restrict để tránh cascade conflict) ----
            modelBuilder.Entity<TripAssignment>()
                .HasOne(ta => ta.Trip)
                .WithMany()
                .HasForeignKey(ta => ta.TripId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TripAssignment>()
                .HasOne(ta => ta.Driver)
                .WithMany(d => d.TripAssignments)
                .HasForeignKey(ta => ta.DriverId)
                .OnDelete(DeleteBehavior.Restrict);

            // ---- Checkin -> Staff ----
            modelBuilder.Entity<Checkin>()
                .HasOne(c => c.Staff)
                .WithMany(s => s.Checkins)
                .HasForeignKey(c => c.StaffId)
                .OnDelete(DeleteBehavior.Restrict);

            // ---- Decimal precision ----
            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Trip>()
                .Property(t => t.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<BusRoute>()
                .Property(r => r.BasePrice)
                .HasPrecision(18, 2);

            // ---- Indexes ----
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username).IsUnique();

            modelBuilder.Entity<Ticket>()
                .HasIndex(t => t.QrCode).IsUnique();
        }
    }
}