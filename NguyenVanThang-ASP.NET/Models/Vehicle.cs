namespace NguyenVanThang_ASP.NET.Models
{
    public class Vehicle
    {
        public int VehicleId { get; set; }
        public string LicensePlate { get; set; }
        public string VehicleType { get; set; }
        public int SeatCount { get; set; }

        public ICollection<Seat> Seats { get; set; }
        public ICollection<Trip> Trips { get; set; }
    }
}
