// Models/Payment.cs — CẬP NHẬT: thêm TransactionId, PaidAt
namespace NguyenVanThang_ASP.NET.Models
{
    public class Payment
    {
        public int PaymentId { get; set; }
        public int BookingId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = "Cash"; // Cash | MoMo | VNPay | Banking
        public string PaymentStatus { get; set; } = "Pending"; // Pending | Paid | Failed | Refunded
        public string? TransactionId { get; set; }  // Mã giao dịch từ cổng thanh toán
        public DateTime? PaidAt { get; set; }

        public Booking Booking { get; set; } = null!;
    }
}