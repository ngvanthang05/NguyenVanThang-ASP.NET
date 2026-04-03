namespace NguyenVanThang_ASP.NET.Models
{
    public class Ticket
    {
        public int TicketId { get; set; }
        public int BookingId { get; set; }
        public int SeatId { get; set; }
        public string QrCode { get; set; }
        public string TicketStatus { get; set; }

        public Booking Booking { get; set; }
        public Seat Seat { get; set; }
    }
}
