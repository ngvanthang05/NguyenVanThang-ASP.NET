// DTOs/OperationsDTOs.cs — Portal 2: Điều hành

namespace NguyenVanThang_ASP.NET.DTOs
{
    // ===== Trip Schedule =====
    public class CreateTripRequest
    {
        public int RouteId { get; set; }
        public int VehicleId { get; set; }
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public decimal Price { get; set; }
        public int? DriverId { get; set; }  // Tuỳ chọn phân công ngay
        public string? AssignNote { get; set; }
    }

    public class AssignDriverRequest
    {
        public int DriverId { get; set; }
        public string? Note { get; set; }
    }

    public class TripAssignmentDto
    {
        public int AssignmentId { get; set; }
        public int TripId { get; set; }
        public string Departure { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public string TripStatus { get; set; } = string.Empty;
        public int DriverId { get; set; }
        public string DriverName { get; set; } = string.Empty;
        public string DriverPhone { get; set; } = string.Empty;
        public DateTime AssignedAt { get; set; }
        public string? Note { get; set; }
    }

    // ===== Operations Monitor =====
    public class TripMonitorDto
    {
        public int TripId { get; set; }
        public string Departure { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string LicensePlate { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public int TotalSeats { get; set; }
        public int BookedSeats { get; set; }
        public int AvailableSeats { get; set; }
        public string? DriverName { get; set; }
        public string? DriverPhone { get; set; }
    }

    // ===== Passenger List (dùng cho nhiều portal) =====
    public class PassengerDto
    {
        public int BookingId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string SeatNumber { get; set; } = string.Empty;
        public string SeatType { get; set; } = string.Empty;
        public string BookingStatus { get; set; } = string.Empty;
        public string TicketStatus { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public bool IsCheckedIn { get; set; }
    }

    // ===== Update Trip Status =====
    public class UpdateTripStatusRequest
    {
        // Active | Departed | Arrived | Cancelled | Completed
        public string Status { get; set; } = string.Empty;
        public string? Note { get; set; }
    }
}