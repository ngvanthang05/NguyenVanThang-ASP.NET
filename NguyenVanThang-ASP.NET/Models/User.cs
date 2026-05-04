// Models/User.cs — CẬP NHẬT: thêm role Driver
namespace NguyenVanThang_ASP.NET.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty; // SHA-256 hashed
        // Roles: Customer | Staff | Driver | Operations | Admin
        public string Role { get; set; } = "Customer";
        public bool IsActive { get; set; } = true;
        public int? CustomerId { get; set; }

        // Navigation
        public Customer? Customer { get; set; }
    }
}