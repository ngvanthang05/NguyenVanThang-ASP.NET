// DTOs/TripSearchRequest.cs
namespace NguyenVanThang_ASP.NET.DTOs
{
    public class TripSearchRequest
    {
        public string Departure { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public DateTime Date { get; set; }
    }

    public class TripSearchResult
    {
        public int TripId { get; set; }
        public string Departure { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public decimal Price { get; set; }
        public string VehicleType { get; set; } = string.Empty;
        public int AvailableSeats { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}