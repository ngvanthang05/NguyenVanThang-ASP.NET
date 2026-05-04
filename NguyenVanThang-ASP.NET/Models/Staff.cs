// Models/Staff.cs — CẬP NHẬT: thêm UserId để link User account
namespace NguyenVanThang_ASP.NET.Models
{
    public class Staff
    {
        public int StaffId { get; set; }
        public int? UserId { get; set; }            // Liên kết tài khoản User (Role = "Staff")
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Role { get; set; } = "Cashier"; // Cashier | Supervisor | Operations
        public string Status { get; set; } = "Active"; // Active | Inactive

        // Navigation
        public User? User { get; set; }
        public ICollection<Checkin> Checkins { get; set; } = new List<Checkin>();
    }
}