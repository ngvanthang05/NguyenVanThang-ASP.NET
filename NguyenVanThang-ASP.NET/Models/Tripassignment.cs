// Models/TripAssignment.cs — Phân công tài xế (Portal 2 + 5)
namespace NguyenVanThang_ASP.NET.Models
{
    public class TripAssignment
    {
        public int Id { get; set; }
        public int AssignmentId { get; set; }
        public int TripId { get; set; }
        public int DriverId { get; set; }
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public string? Note { get; set; }

        // Navigation
        public Trip Trip { get; set; } = null!;
        public Driver Driver { get; set; } = null!;
    }
}