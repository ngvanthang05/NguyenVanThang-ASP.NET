// Models/Booking.cs — thêm navigation Seat, fix SeatId
using System.Text.Json.Serialization;

namespace NguyenVanThang_ASP.NET.Models
{
    public class Booking
    {
        public int BookingId { get; set; }
        public int CustomerId { get; set; }
        public int TripId { get; set; }
        public int SeatId { get; set; }
        public DateTime BookingDate { get; set; } = DateTime.Now;
        public string Status { get; set; } = "Pending"; // Pending | Confirmed | Cancelled
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [JsonIgnore]
        public Customer Customer { get; set; } = null!;
        [JsonIgnore]
        public Trip Trip { get; set; } = null!;
        public Seat? Seat { get; set; }
        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
        public Payment? Payment { get; set; }
    }
}