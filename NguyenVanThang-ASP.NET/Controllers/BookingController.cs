// Controllers/BookingController.cs — PORTAL 4: Khách hàng (Website / App)
// Roles: Customer (tự đặt vé), Admin (xem tất cả)
// Chức năng: Tìm chuyến, đặt vé, xem vé cá nhân, hủy vé, đổi ghế

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using NguyenVanThang_ASP.NET.Data;
using NguyenVanThang_ASP.NET.DTOs;
using NguyenVanThang_ASP.NET.Services;

namespace NguyenVanThang_ASP.NET.Controllers
{
    [Route("api/bookings")]
    [ApiController]
    public class BookingController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IBookingService _bookingService;

        public BookingController(AppDbContext context, IBookingService bookingService)
        {
            _context = context;
            _bookingService = bookingService;
        }

        // Helper: lấy CustomerId từ JWT
        private int? GetCustomerId()
        {
            var claim = User.FindFirst("customerId")?.Value;
            return claim != null ? int.Parse(claim) : null;
        }

        // ============================================================
        // POST api/bookings/search — Tìm kiếm chuyến xe (public)
        // ============================================================
        [HttpPost("search")]
        public async Task<IActionResult> SearchTrips([FromBody] TripSearchRequest request)
        {
            var trips = await _context.Trips
                .Include(t => t.Route)
                .Include(t => t.Vehicle).ThenInclude(v => v.Seats)
                .Where(t =>
                    t.Route.Departure.ToLower().Contains(request.Departure.ToLower()) &&
                    t.Route.Destination.ToLower().Contains(request.Destination.ToLower()) &&
                    t.DepartureTime.Date == request.Date.Date &&
                    t.Status == "Active")
                .OrderBy(t => t.DepartureTime)
                .ToListAsync();

            var result = trips.Select(t => new TripSearchResult
            {
                TripId = t.TripId,
                Departure = t.Route.Departure,
                Destination = t.Route.Destination,
                DepartureTime = t.DepartureTime,
                ArrivalTime = t.ArrivalTime,
                Price = t.Price,
                VehicleType = t.Vehicle.VehicleType,
                AvailableSeats = t.Vehicle.Seats.Count(s => !s.IsBooked),
                Status = t.Status
            });

            return Ok(result);
        }

        // ============================================================
        // GET api/bookings/trips/{id}/seats — Xem sơ đồ ghế (public)
        // ============================================================
        [HttpGet("trips/{id}/seats")]
        public async Task<IActionResult> GetTripSeats(int id)
        {
            var trip = await _context.Trips
                .Include(t => t.Vehicle).ThenInclude(v => v.Seats)
                .Include(t => t.Route)
                .FirstOrDefaultAsync(t => t.TripId == id);

            if (trip == null) return NotFound();

            return Ok(new
            {
                TripId = id,
                Departure = trip.Route.Departure,
                Destination = trip.Route.Destination,
                DepartureTime = trip.DepartureTime,
                Price = trip.Price,
                VehicleType = trip.Vehicle.VehicleType,
                LicensePlate = trip.Vehicle.LicensePlate,
                Seats = trip.Vehicle.Seats
                    .OrderBy(s => s.SeatNumber)
                    .Select(s => new { s.SeatId, s.SeatNumber, s.SeatType, s.IsBooked })
            });
        }

        // ============================================================
        // POST api/bookings — Đặt vé online (Customer)
        // ============================================================
        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
        {
            var customerId = GetCustomerId();
            if (customerId == null)
                return Unauthorized(new { message = "Không xác định được thông tin khách hàng" });

            var result = await _bookingService.CreateBookingAsync(request, customerId.Value);
            return Ok(result);
        }

        // ============================================================
        // GET api/bookings/my — Lịch sử đặt vé của tôi
        // ============================================================
        [HttpGet("my")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetMyBookings()
        {
            var customerId = GetCustomerId();
            if (customerId == null) return Unauthorized();

            var result = await _bookingService.GetBookingsByCustomerAsync(customerId.Value);
            return Ok(result);
        }

        // ============================================================
        // GET api/bookings/{id} — Chi tiết booking + vé (Customer xem vé của mình)
        // ============================================================
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetBookingDetail(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Trip).ThenInclude(t => t.Route)
                .Include(b => b.Trip).ThenInclude(t => t.Vehicle)
                .Include(b => b.Seat)
                .Include(b => b.Payment)
                .Include(b => b.Tickets)
                .Include(b => b.Customer)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null) return NotFound();

            // Customer chỉ xem booking của mình
            if (User.IsInRole("Customer"))
            {
                var customerId = GetCustomerId();
                if (booking.CustomerId != customerId)
                    return Forbid();
            }

            return Ok(new
            {
                booking.BookingId,
                booking.Status,
                booking.BookingDate,
                Customer = new { booking.Customer?.Name, booking.Customer?.Phone, booking.Customer?.Email },
                Trip = new
                {
                    booking.Trip.Route.Departure,
                    booking.Trip.Route.Destination,
                    booking.Trip.DepartureTime,
                    booking.Trip.ArrivalTime,
                    booking.Trip.Vehicle.LicensePlate,
                    booking.Trip.Vehicle.VehicleType,
                    booking.Trip.Status
                },
                Seat = new { booking.Seat?.SeatNumber, booking.Seat?.SeatType },
                Payment = new
                {
                    booking.Payment?.Amount,
                    booking.Payment?.PaymentMethod,
                    booking.Payment?.PaymentStatus
                },
                Ticket = booking.Tickets.Select(t => new { t.QrCode, t.TicketStatus }).FirstOrDefault()
            });
        }

        // ============================================================
        // DELETE api/bookings/{id} — Hủy vé (Customer tự hủy)
        // ============================================================
        [HttpDelete("{id}")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CancelBooking(int id)
        {
            var customerId = GetCustomerId();
            if (customerId == null) return Unauthorized();

            var success = await _bookingService.CancelBookingAsync(id, customerId.Value);
            if (!success) return NotFound(new { message = "Không tìm thấy booking" });

            return Ok(new { message = "Đã hủy vé thành công" });
        }

        // ============================================================
        // GET api/bookings/my/ticket/{ticketQrCode} — Xem vé bằng QR code
        // ============================================================
        [HttpGet("my/ticket/{qrCode}")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetMyTicket(string qrCode)
        {
            var customerId = GetCustomerId();

            var ticket = await _context.Tickets
                .Include(t => t.Booking).ThenInclude(b => b.Trip).ThenInclude(t => t.Route)
                .Include(t => t.Booking).ThenInclude(b => b.Customer)
                .Include(t => t.Seat)
                .FirstOrDefaultAsync(t =>
                    t.QrCode == qrCode &&
                    t.Booking.CustomerId == customerId);

            if (ticket == null) return NotFound(new { message = "Không tìm thấy vé" });

            return Ok(new
            {
                ticket.QrCode,
                ticket.TicketStatus,
                CustomerName = ticket.Booking.Customer?.Name,
                SeatNumber = ticket.Seat?.SeatNumber,
                SeatType = ticket.Seat?.SeatType,
                Departure = ticket.Booking.Trip.Route.Departure,
                Destination = ticket.Booking.Trip.Route.Destination,
                DepartureTime = ticket.Booking.Trip.DepartureTime,
                TripStatus = ticket.Booking.Trip.Status
            });
        }

        // ============================================================
        // PUT api/bookings/{id}/payment-callback — Callback thanh toán điện tử
        // (Dùng cho MoMo/VNPay webhook gọi về sau khi thanh toán thành công)
        // ============================================================
        [HttpPut("{id}/payment-callback")]
        [AllowAnonymous] // Webhook từ cổng thanh toán, không có token
        public async Task<IActionResult> PaymentCallback(int id, [FromBody] PaymentCallbackRequest request)
        {
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.BookingId == id);

            if (payment == null)
                return NotFound(new { message = "Không tìm thấy thanh toán" });

            // TODO: Verify signature từ cổng thanh toán (VNPay/MoMo checksum)
            // Hiện tại: trust request (cần bổ sung verify trong production)
            if (request.Status == "Success")
            {
                payment.PaymentStatus = "Paid";
                payment.TransactionId = request.TransactionId;
                payment.PaidAt = DateTime.Now;

                // Confirm booking
                var booking = await _context.Bookings.FindAsync(id);
                if (booking != null) booking.Status = "Confirmed";
            }
            else
            {
                payment.PaymentStatus = "Failed";
                // Hoàn ghế nếu thanh toán thất bại
                var booking = await _context.Bookings.Include(b => b.Seat)
                    .FirstOrDefaultAsync(b => b.BookingId == id);
                if (booking != null)
                {
                    booking.Status = "Cancelled";
                    if (booking.Seat != null) booking.Seat.IsBooked = false;
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Cập nhật trạng thái thanh toán thành công" });
        }

        // ============================================================
        // Admin: GET api/bookings?tripId=&status= — Xem tất cả booking
        // ============================================================
        [HttpGet]
        [Authorize(Roles = "Admin,Staff,Operations")]
        public async Task<IActionResult> GetAllBookings(
            [FromQuery] int? tripId = null,
            [FromQuery] string? status = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            var query = _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Trip).ThenInclude(t => t.Route)
                .Include(b => b.Seat)
                .Include(b => b.Payment)
                .AsQueryable();

            if (tripId.HasValue) query = query.Where(b => b.TripId == tripId.Value);
            if (!string.IsNullOrEmpty(status)) query = query.Where(b => b.Status == status);
            if (from.HasValue) query = query.Where(b => b.BookingDate >= from.Value);
            if (to.HasValue) query = query.Where(b => b.BookingDate <= to.Value.AddDays(1));

            var bookings = await query
                .OrderByDescending(b => b.BookingDate)
                .Take(100)
                .ToListAsync();

            return Ok(bookings.Select(b => new
            {
                b.BookingId,
                b.Status,
                b.BookingDate,
                CustomerName = b.Customer?.Name,
                CustomerPhone = b.Customer?.Phone,
                Departure = b.Trip.Route.Departure,
                Destination = b.Trip.Route.Destination,
                DepartureTime = b.Trip.DepartureTime,
                SeatNumber = b.Seat?.SeatNumber,
                Amount = b.Payment?.Amount,
                PaymentStatus = b.Payment?.PaymentStatus
            }));
        }
    }

    // DTO cho payment callback
    public class PaymentCallbackRequest
    {
        public string Status { get; set; } = string.Empty; // Success | Failed
        public string? TransactionId { get; set; }
        public string? Signature { get; set; }
    }
}