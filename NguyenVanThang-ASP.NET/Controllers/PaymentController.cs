// Controllers/PaymentController.cs — CẬP NHẬT: thêm auth, refund, admin view
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NguyenVanThang_ASP.NET.Data;
using NguyenVanThang_ASP.NET.Models;

namespace NguyenVanThang_ASP.NET.Controllers
{
    [Route("api/payments")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PaymentsController(AppDbContext context) => _context = context;

        // GET api/payments — Admin xem tất cả thanh toán
        [HttpGet]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetPayments(
            [FromQuery] string? status = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            var query = _context.Payments
                .Include(p => p.Booking).ThenInclude(b => b.Customer)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status)) query = query.Where(p => p.PaymentStatus == status);
            if (from.HasValue) query = query.Where(p => p.Booking.BookingDate >= from.Value);
            if (to.HasValue) query = query.Where(p => p.Booking.BookingDate <= to.Value.AddDays(1));

            var payments = await query.OrderByDescending(p => p.PaymentId).Take(100).ToListAsync();

            return Ok(payments.Select(p => new
            {
                p.PaymentId,
                p.BookingId,
                CustomerName = p.Booking.Customer?.Name,
                CustomerPhone = p.Booking.Customer?.Phone,
                p.Amount,
                p.PaymentMethod,
                p.PaymentStatus,
                p.TransactionId,
                p.PaidAt
            }));
        }

        // GET api/payments/{id}
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetPayment(int id)
        {
            var payment = await _context.Payments
                .Include(p => p.Booking).ThenInclude(b => b.Customer)
                .FirstOrDefaultAsync(p => p.PaymentId == id);

            if (payment == null) return NotFound();

            // Customer chỉ xem payment của mình
            if (User.IsInRole("Customer"))
            {
                var customerId = int.Parse(User.FindFirst("customerId")?.Value ?? "0");
                if (payment.Booking.CustomerId != customerId) return Forbid();
            }

            return Ok(payment);
        }

        // PUT api/payments/{id}/mark-paid — Staff xác nhận đã thu tiền mặt
        [HttpPut("{id}/mark-paid")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> MarkPaid(int id, [FromBody] string? note = null)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment == null) return NotFound();
            if (payment.PaymentStatus == "Paid")
                return BadRequest(new { message = "Đã được xác nhận thanh toán trước đó" });

            payment.PaymentStatus = "Paid";
            payment.PaidAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã xác nhận thanh toán" });
        }

        // PUT api/payments/{id}/refund — Admin hoàn tiền
        [HttpPut("{id}/refund")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Refund(int id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment == null) return NotFound();
            if (payment.PaymentStatus == "Refunded")
                return BadRequest(new { message = "Đã được hoàn tiền trước đó" });

            payment.PaymentStatus = "Refunded";
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã xác nhận hoàn tiền" });
        }

        // GET api/payments/stats — Admin xem thống kê thanh toán
        [HttpGet("stats")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPaymentStats()
        {
            var stats = await _context.Payments.GroupBy(p => p.PaymentStatus)
                .Select(g => new { Status = g.Key, Count = g.Count(), Total = g.Sum(p => p.Amount) })
                .ToListAsync();

            var byMethod = await _context.Payments
                .Where(p => p.PaymentStatus == "Paid")
                .GroupBy(p => p.PaymentMethod)
                .Select(g => new { Method = g.Key, Count = g.Count(), Total = g.Sum(p => p.Amount) })
                .ToListAsync();

            return Ok(new { ByStatus = stats, ByMethod = byMethod });
        }
    }
}