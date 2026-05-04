// DTOs/AdminDTOs.cs — Portal 1: Admin

namespace NguyenVanThang_ASP.NET.DTOs
{
    // ===== Dashboard =====
    public class DashboardResponse
    {
        public int TotalTripsToday { get; set; }
        public int TotalTripsMonth { get; set; }
        public decimal RevenueToday { get; set; }
        public decimal RevenueMonth { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalVehicles { get; set; }
        public int TotalDrivers { get; set; }
        public int TotalStaff { get; set; }
        public int ActiveTrips { get; set; }
        public int CancelledTripsMonth { get; set; }
        public List<RevenueByDay> RevenueLast7Days { get; set; } = new();
        public List<TopRouteDto> TopRoutes { get; set; } = new();
    }

    public class RevenueByDay
    {
        public string Date { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int Bookings { get; set; }
    }

    public class TopRouteDto
    {
        public string Departure { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public int TripCount { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    // ===== Revenue Report =====
    public class RevenueReportRequest
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public string GroupBy { get; set; } = "Day"; // Day | Month
    }

    public class RevenueReportResponse
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalBookings { get; set; }
        public int TotalTrips { get; set; }
        public List<RevenueByDay> Details { get; set; } = new();
    }

    // ===== Staff Management =====
    public class CreateStaffRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Role { get; set; } = "Cashier"; // Cashier | Supervisor | Operations
    }

    public class StaffDto
    {
        public int StaffId { get; set; }
        public int? UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Username { get; set; }
    }

    // ===== Driver Management =====
    public class CreateDriverRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public int ExperienceYears { get; set; }
    }

    public class DriverDto
    {
        public int DriverId { get; set; }
        public int? UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public int ExperienceYears { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Username { get; set; }
        // Chuyến sắp tới
        public TripAssignmentDto? UpcomingTrip { get; set; }
    }
}