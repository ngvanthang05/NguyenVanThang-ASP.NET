using Microsoft.EntityFrameworkCore;
using NguyenVanThang_ASP.NET.Models;

namespace NguyenVanThang_ASP.NET.Data
{
    public class AppDbContext : DbContext
    {
        // Constructor này bắt buộc phải có để nhận Connection String từ Program.cs
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)  { }
        public DbSet<Student> Students { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<BusRoute> Routes { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<Seat> Seats { get; set; }
        public DbSet<Trip> Trips { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Staff> Staffs { get; set; }
        public DbSet<Checkin> Checkins { get; set; }

        // THÊM ĐOẠN DƯỚI ĐÂY ĐỂ FIX LỖI
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Fix lỗi "Multiple Cascade Paths" trên bảng Tickets
            // Khi xóa một Ghế (Seat), không tự động xóa Vé (Ticket) để tránh xung đột đường dẫn
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Seat)
                .WithMany() // Nếu trong model Seat.cs bạn có ICollection<Ticket> Tickets thì điền vào đây
                .HasForeignKey(t => t.SeatId)
                .OnDelete(DeleteBehavior.Restrict);

            // 2. Fix lỗi cảnh báo Decimal cho cột Amount trong bảng Payment
            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasPrecision(18, 2);
        }
    }

}
