// Models/Driver.cs — Tài xế (Portal 5)
namespace NguyenVanThang_ASP.NET.Models
{
    public class Driver
    {
        public int DriverId { get; set; }
        public int? UserId { get; set; }                // Liên kết tài khoản User (Role = "Driver")
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;  // Số bằng lái
        public int ExperienceYears { get; set; }
        public string Status { get; set; } = "Active";  // Active | Inactive | OnLeave

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public User? User { get; set; }
        public ICollection<TripAssignment> TripAssignments { get; set; } = new List<TripAssignment>();
    }
}