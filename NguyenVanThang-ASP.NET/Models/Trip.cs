// Models/Trip.cs — thêm Price, AvailableSeats
namespace NguyenVanThang_ASP.NET.Models
{
    public class Trip
    {
        public int TripId { get; set; }
        public int RouteId { get; set; }
        public int VehicleId { get; set; }
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public string Status { get; set; } = "Active"; // Active | Cancelled | Completed
        public decimal Price { get; set; }

        public BusRoute Route { get; set; } = null!;
        public Vehicle Vehicle { get; set; } = null!;
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    }
}