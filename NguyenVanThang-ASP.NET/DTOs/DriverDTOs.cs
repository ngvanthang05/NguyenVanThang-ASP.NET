// DTOs/DriverDTOs.cs — Portal 5: Tài xế

namespace NguyenVanThang_ASP.NET.DTOs
{
    public class DriverScheduleDto
    {
        public int AssignmentId { get; set; }
        public int TripId { get; set; }
        public string Departure { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public string TripStatus { get; set; } = string.Empty;
        public string LicensePlate { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public int TotalPassengers { get; set; }
        public int CheckedIn { get; set; }
        public string? Note { get; set; }
    }

    public class DriverProfileDto
    {
        public int DriverId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public int ExperienceYears { get; set; }
        public string Status { get; set; } = string.Empty;
        public int TotalTripsCompleted { get; set; }
    }

    public class DriverUpdateTripRequest
    {
        // Departed | Arrived | Completed
        public string Status { get; set; } = string.Empty;
        public string? Note { get; set; }
    }
}