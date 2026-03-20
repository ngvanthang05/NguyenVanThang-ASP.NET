using Microsoft.EntityFrameworkCore;
using NguyenVanThang_ASP.NET.Models;

namespace NguyenVanThang_ASP.NET.Data
{
    public class AppDbContext : DbContext
    {
        // Constructor này bắt buộc phải có để nhận Connection String từ Program.cs
            public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
                    {
                    }
        public DbSet<Student> Students { get; set; }
    }
}
