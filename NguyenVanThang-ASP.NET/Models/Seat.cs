namespace NguyenVanThang_ASP.NET.Models
{
    public class Seat
    {
        public int SeatId { get; set; }
        public int VehicleId { get; set; }
        public string SeatNumber { get; set; }
        public bool IsBooked { get; set; }
        public string SeatType { get; set; }

        public Vehicle Vehicle { get; set; }
    }
}
