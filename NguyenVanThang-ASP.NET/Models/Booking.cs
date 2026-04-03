using System.Net.Sockets;

namespace NguyenVanThang_ASP.NET.Models
{
    public class Booking
    {
        public int BookingId { get; set; }
        public int CustomerId { get; set; }
        public int TripId { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }

        public Customer Customer { get; set; }
        public Trip Trip { get; set; }
        public ICollection<Ticket> Tickets { get; set; }
    }
}
