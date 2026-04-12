// Models/User.cs — thêm CustomerId
namespace NguyenVanThang_ASP.NET.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty; // SHA-256 hashed
        public string Role { get; set; } = "Customer"; // Customer | Staff | Admin
        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }
    }
}