// DTOs/StaffSalesDTOs.cs — Portal 3: Nhân viên bán vé

namespace NguyenVanThang_ASP.NET.DTOs
{
    // ===== Counter Sale (bán tại quầy) =====
    public class CounterSaleRequest
    {
        // Thông tin khách (có thể là khách vãng lai, không cần tài khoản)
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;

        // Thông tin vé
        public int TripId { get; set; }
        public int SeatId { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
    }

    public class CounterSaleResponse
    {
        public int BookingId { get; set; }
        public int TicketId { get; set; }
        public string QrCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string SeatNumber { get; set; } = string.Empty;
        public string SeatType { get; set; } = string.Empty;
        public string Departure { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public DateTime DepartureTime { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string BookingStatus { get; set; } = string.Empty;
    }

    // ===== Change Seat =====
    public class ChangeSeatRequest
    {
        public int NewSeatId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    // ===== Staff Cancel (nhân viên hủy vé) =====
    public class StaffCancelRequest
    {
        public string Reason { get; set; } = string.Empty;
        public bool ForceCancel { get; set; } = false; // bỏ qua giới hạn 2 giờ
    }

    // ===== Today's trip summary for cashier =====
    public class TripSummaryDto
    {
        public int TripId { get; set; }
        public string Departure { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public DateTime DepartureTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public string LicensePlate { get; set; } = string.Empty;
        public int TotalSeats { get; set; }
        public int SoldSeats { get; set; }
        public int AvailableSeats { get; set; }
        public decimal Revenue { get; set; }
        public string? DriverName { get; set; }
    }
}