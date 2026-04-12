// DTOs/CreateBookingRequest.cs — cập nhật
namespace NguyenVanThang_ASP.NET.DTOs
{
    public class CreateBookingRequest
    {
        public int TripId { get; set; }
        public int SeatId { get; set; }
        public string PaymentMethod { get; set; } = "Cash"; // Cash | MoMo | VNPay | Banking
    }

    public class BookingResponse
    {
        public int BookingId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime BookingDate { get; set; }
        public TripInfo Trip { get; set; } = null!;
        public SeatInfo Seat { get; set; } = null!;
        public PaymentInfo Payment { get; set; } = null!;
        public string TicketQrCode { get; set; } = string.Empty;
    }

    public class TripInfo
    {
        public string Departure { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public DateTime DepartureTime { get; set; }
    }

    public class SeatInfo
    {
        public string SeatNumber { get; set; } = string.Empty;
        public string SeatType { get; set; } = string.Empty;
    }

    public class PaymentInfo
    {
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
    }
}