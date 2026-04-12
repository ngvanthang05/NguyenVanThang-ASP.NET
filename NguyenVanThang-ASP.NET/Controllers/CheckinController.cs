// Controllers/CheckinController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NguyenVanThang_ASP.NET.Data;
using NguyenVanThang_ASP.NET.Models;

namespace NguyenVanThang_ASP.NET.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Staff")]
    public class CheckinsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CheckinsController(AppDbContext context) => _context = context;

        // POST: api/checkins/scan — quét QR code
        [HttpPost("scan")]
        public async Task<IActionResult> ScanTicket([FromBody] string qrCode)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Booking).ThenInclude(b => b.Trip).ThenInclude(t => t.Route)
                .Include(t => t.Seat)
                .FirstOrDefaultAsync(t => t.QrCode == qrCode);

            if (ticket == null)
                return NotFound(new { message = "QR Code không hợp lệ" });

            if (ticket.TicketStatus == "Used")
                return BadRequest(new { message = "Vé đã được sử dụng!" });

            if (ticket.TicketStatus == "Cancelled")
                return BadRequest(new { message = "Vé đã bị hủy!" });

            // Lấy StaffId từ JWT
            var staffId = int.Parse(User.FindFirst("id")?.Value ?? "0");

            // Tạo check-in record
            var checkin = new Checkin
            {
                TicketId = ticket.TicketId,
                StaffId = staffId,
                CheckinTime = DateTime.Now
            };
            _context.Checkins.Add(checkin);

            // Đánh dấu vé đã dùng
            ticket.TicketStatus = "Used";
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Check-in thành công!",
                customerName = ticket.Booking.Customer?.Name,
                seatNumber = ticket.Seat?.SeatNumber,
                departure = ticket.Booking.Trip.Route.Departure,
                destination = ticket.Booking.Trip.Route.Destination,
                departureTime = ticket.Booking.Trip.DepartureTime
            });
        }

        // GET: api/checkins/trip/5 — danh sách check-in theo chuyến
        [HttpGet("trip/{tripId}")]
        public async Task<IActionResult> GetCheckinsByTrip(int tripId)
        {
            var checkins = await _context.Checkins
                .Include(c => c.Ticket).ThenInclude(t => t.Booking)
                .Include(c => c.Ticket).ThenInclude(t => t.Seat)
                .Include(c => c.Staff)
                .Where(c => c.Ticket.Booking.TripId == tripId)
                .ToListAsync();

            return Ok(checkins);
        }
    }
}