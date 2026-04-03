namespace NguyenVanThang_ASP.NET.Models
{
    public class Trip
    {
        public int TripId { get; set; }
        public int RouteId { get; set; }
        public int VehicleId { get; set; }
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public string Status { get; set; }

        public BusRoute Route { get; set; }
        public Vehicle Vehicle { get; set; }
        public ICollection<Booking> Bookings { get; set; }
    }
}
