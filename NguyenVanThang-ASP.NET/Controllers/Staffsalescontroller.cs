// Controllers/StaffSalesController.cs — PORTAL 3: Nhân viên bán vé (Quầy)
// Roles: Admin, Staff
// Chức năng: Bán vé trực tiếp, check-in QR, hủy/đổi vé cho khách

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NguyenVanThang_ASP.NET.Data;
using NguyenVanThang_ASP.NET.DTOs;
using NguyenVanThang_ASP.NET.Models;

namespace NguyenVanThang_ASP.NET.Controllers
{
    [Route("api/staff-sales")]
    [ApiController]
    [Authorize(Roles = "Admin,Staff")]
    public class StaffSalesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StaffSalesController(AppDbContext context) => _context = context;

        // ============================================================
        // GET api/staff-sales/trips/today — Chuyến xe hôm nay
        // ============================================================
        [HttpGet("trips/today")]
        public async Task<IActionResult> GetTodayTrips()
        {
            var today = DateTime.Today;
            var trips = await _context.Trips
                .Include(t => t.Route)
                .Include(t => t.Vehicle).ThenInclude(v => v.Seats)
                .Include(t => t.Bookings).ThenInclude(b => b.Payment)
                .Where(t => t.DepartureTime.Date == today)
                .OrderBy(t => t.DepartureTime)
                .ToListAsync();

            var tripIds = trips.Select(t => t.TripId).ToList();
            var assignments = await _context.TripAssignments
                .Include(ta => ta.Driver)
                .Where(ta => tripIds.Contains(ta.TripId))
                .ToListAsync();

            var result = trips.Select(t =>
            {
                var assignment = assignments.FirstOrDefault(a => a.TripId == t.TripId);
                var validBookings = t.Bookings.Where(b => b.Status != "Cancelled").ToList();
                return new TripSummaryDto
                {
                    TripId = t.TripId,
                    Departure = t.Route.Departure,
                    Destination = t.Route.Destination,
                    DepartureTime = t.DepartureTime,
                    Status = t.Status,
                    LicensePlate = t.Vehicle.LicensePlate,
                    TotalSeats = t.Vehicle.SeatCount,
                    SoldSeats = validBookings.Count,
                    AvailableSeats = t.Vehicle.Seats.Count(s => !s.IsBooked),
                    Revenue = validBookings.Sum(b => b.Payment?.Amount ?? 0),
                    DriverName = assignment?.Driver.FullName
                };
            });

            return Ok(result);
        }

        // ============================================================
        // GET api/staff-sales/trips/{id}/seats — Sơ đồ ghế
        // ============================================================
        [HttpGet("trips/{id}/seats")]
        public async Task<IActionResult> GetAvailableSeats(int id)
        {
            var trip = await _context.Trips
                .Include(t => t.Vehicle).ThenInclude(v => v.Seats)
                .FirstOrDefaultAsync(t => t.TripId == id);

            if (trip == null) return NotFound();

            var seats = trip.Vehicle.Seats
                .OrderBy(s => s.SeatNumber)
                .Select(s => new { s.SeatId, s.SeatNumber, s.SeatType, s.IsBooked });

            return Ok(new
            {
                TripId = id,
                VehicleType = trip.Vehicle.VehicleType,
                LicensePlate = trip.Vehicle.LicensePlate,
                TotalSeats = trip.Vehicle.SeatCount,
                AvailableSeats = trip.Vehicle.Seats.Count(s => !s.IsBooked),
                Seats = seats
            });
        }

        // ============================================================
        // GET api/staff-sales/trips/{id}/passengers — DS hành khách
        // ============================================================
        [HttpGet("trips/{id}/passengers")]
        public async Task<IActionResult> GetPassengers(int id)
        {
            var bookings = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Seat)
                .Include(b => b.Payment)
                .Include(b => b.Tickets)
                .Where(b => b.TripId == id)
                .OrderBy(b => b.Seat!.SeatNumber)
                .ToListAsync();

            var ticketIds = bookings.SelectMany(b => b.Tickets).Select(t => t.TicketId).ToList();
            var checkedIn = await _context.Checkins
                .Where(c => ticketIds.Contains(c.TicketId))
                .Select(c => c.TicketId).ToListAsync();

            return Ok(bookings.Select(b => new PassengerDto
            {
                BookingId = b.BookingId,
                CustomerName = b.Customer?.Name ?? "",
                CustomerPhone = b.Customer?.Phone ?? "",
                SeatNumber = b.Seat?.SeatNumber ?? "",
                SeatType = b.Seat?.SeatType ?? "",
                BookingStatus = b.Status,
                TicketStatus = b.Tickets.FirstOrDefault()?.TicketStatus ?? "",
                PaymentMethod = b.Payment?.PaymentMethod ?? "",
                Amount = b.Payment?.Amount ?? 0,
                IsCheckedIn = b.Tickets.Any(t => checkedIn.Contains(t.TicketId))
            }));
        }

        // ============================================================
        // POST api/staff-sales/bookings — Bán vé tại quầy
        // Khách vãng lai (không cần tài khoản) hoặc dùng số điện thoại
        // ============================================================
        [HttpPost("bookings")]
        public async Task<IActionResult> CounterSale([FromBody] CounterSaleRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Kiểm tra chuyến
                var trip = await _context.Trips
                    .Include(t => t.Route)
                    .FirstOrDefaultAsync(t => t.TripId == request.TripId && t.Status == "Active");
                if (trip == null)
                    return BadRequest(new { message = "Chuyến xe không tồn tại hoặc đã hủy" });

                if (trip.DepartureTime <= DateTime.Now)
                    return BadRequest(new { message = "Chuyến xe đã khởi hành" });

                // 2. Kiểm tra ghế
                var seat = await _context.Seats
                    .Include(s => s.Vehicle)
                    .FirstOrDefaultAsync(s => s.SeatId == request.SeatId && !s.IsBooked);
                if (seat == null)
                    return BadRequest(new { message = "Ghế đã được đặt hoặc không tồn tại" });

                if (seat.VehicleId != trip.VehicleId)
                    return BadRequest(new { message = "Ghế không thuộc xe của chuyến này" });

                // 3. Tìm hoặc tạo Customer (theo SĐT)
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.Phone == request.CustomerPhone);

                if (customer == null)
                {
                    customer = new Customer
                    {
                        Name = request.CustomerName,
                        Phone = request.CustomerPhone,
                        Email = request.CustomerEmail,
                        CreatedAt = DateTime.Now
                    };
                    _context.Customers.Add(customer);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    // Cập nhật tên nếu đã biết
                    if (!string.IsNullOrEmpty(request.CustomerName))
                        customer.Name = request.CustomerName;
                }

                // 4. Lock ghế + tạo Booking
                seat.IsBooked = true;

                var booking = new Booking
                {
                    CustomerId = customer.CustomerId,
                    TripId = request.TripId,
                    SeatId = request.SeatId,
                    BookingDate = DateTime.Now,
                    Status = "Confirmed",
                    CreatedAt = DateTime.Now
                };
                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                // 5. Tạo Ticket
                var qrCode = GenerateQrCode(booking.BookingId, request.TripId, request.SeatId);
                var ticket = new Ticket
                {
                    BookingId = booking.BookingId,
                    SeatId = request.SeatId,
                    QrCode = qrCode,
                    TicketStatus = "Valid"
                };
                _context.Tickets.Add(ticket);

                // 6. Tạo Payment
                // Tiền mặt tại quầy -> Paid ngay
                var payment = new Payment
                {
                    BookingId = booking.BookingId,
                    Amount = trip.Price,
                    PaymentMethod = request.PaymentMethod,
                    PaymentStatus = request.PaymentMethod == "Cash" ? "Paid" : "Pending"
                };
                _context.Payments.Add(payment);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new CounterSaleResponse
                {
                    BookingId = booking.BookingId,
                    TicketId = ticket.TicketId,
                    QrCode = qrCode,
                    CustomerName = customer.Name,
                    CustomerPhone = customer.Phone,
                    SeatNumber = seat.SeatNumber,
                    SeatType = seat.SeatType,
                    Departure = trip.Route.Departure,
                    Destination = trip.Route.Destination,
                    DepartureTime = trip.DepartureTime,
                    Amount = payment.Amount,
                    PaymentMethod = payment.PaymentMethod,
                    PaymentStatus = payment.PaymentStatus,
                    BookingStatus = booking.Status
                });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ============================================================
        // POST api/staff-sales/checkin/scan — Quét QR check-in
        // Nhân viên quầy tự scan (hoặc nhập QR code)
        // ============================================================
        [HttpPost("checkin/scan")]
        public async Task<IActionResult> ScanTicket([FromBody] string qrCode)
        {
            var ticket = await _context.Tickets
                .Include(t => t.Booking).ThenInclude(b => b.Trip).ThenInclude(t => t.Route)
                .Include(t => t.Booking).ThenInclude(b => b.Customer)
                .Include(t => t.Seat)
                .FirstOrDefaultAsync(t => t.QrCode == qrCode);

            if (ticket == null)
                return NotFound(new { message = "QR Code không hợp lệ" });

            if (ticket.TicketStatus == "Used")
                return BadRequest(new { message = "Vé đã được sử dụng!" });

            if (ticket.TicketStatus == "Cancelled")
                return BadRequest(new { message = "Vé đã bị hủy!" });

            if (ticket.Booking.Status == "Cancelled")
                return BadRequest(new { message = "Booking đã bị hủy!" });

            // Lấy StaffId từ JWT (UserId của staff đang đăng nhập)
            var userId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            var staff = await _context.Staffs.FirstOrDefaultAsync(s => s.UserId == userId);
            if (staff == null)
                return Unauthorized(new { message = "Không tìm thấy hồ sơ nhân viên" });

            var checkin = new Checkin
            {
                TicketId = ticket.TicketId,
                StaffId = staff.StaffId,
                CheckinTime = DateTime.Now
            };
            _context.Checkins.Add(checkin);
            ticket.TicketStatus = "Used";
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "✅ Check-in thành công!",
                customerName = ticket.Booking.Customer?.Name,
                customerPhone = ticket.Booking.Customer?.Phone,
                seatNumber = ticket.Seat?.SeatNumber,
                seatType = ticket.Seat?.SeatType,
                departure = ticket.Booking.Trip.Route.Departure,
                destination = ticket.Booking.Trip.Route.Destination,
                departureTime = ticket.Booking.Trip.DepartureTime,
                checkinTime = checkin.CheckinTime
            });
        }

        // ============================================================
        // PUT api/staff-sales/bookings/{id}/cancel — Nhân viên hủy vé
        // ============================================================
        [HttpPut("bookings/{id}/cancel")]
        public async Task<IActionResult> CancelBooking(int id, [FromBody] StaffCancelRequest request)
        {
            var booking = await _context.Bookings
                .Include(b => b.Tickets)
                .Include(b => b.Payment)
                .Include(b => b.Seat)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null) return NotFound();
            if (booking.Status == "Cancelled")
                return BadRequest(new { message = "Vé đã được hủy trước đó" });

            // Staff có thể bỏ qua giới hạn 2 giờ nếu ForceCancel = true
            if (!request.ForceCancel)
            {
                var trip = await _context.Trips.FindAsync(booking.TripId);
                if (trip != null && trip.DepartureTime <= DateTime.Now.AddHours(2))
                    return BadRequest(new { message = "Vé trong vòng 2 giờ khởi hành. Dùng ForceCancel = true nếu cần hủy khẩn cấp." });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                booking.Status = "Cancelled";
                foreach (var t in booking.Tickets)
                    t.TicketStatus = "Cancelled";
                if (booking.Payment != null)
                    booking.Payment.PaymentStatus = "Refunded";
                if (booking.Seat != null)
                    booking.Seat.IsBooked = false;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok(new { message = "Đã hủy vé và hoàn tiền cho khách" });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ============================================================
        // PUT api/staff-sales/bookings/{id}/change-seat — Đổi ghế
        // ============================================================
        [HttpPut("bookings/{id}/change-seat")]
        public async Task<IActionResult> ChangeSeat(int id, [FromBody] ChangeSeatRequest request)
        {
            var booking = await _context.Bookings
                .Include(b => b.Tickets)
                .Include(b => b.Seat)
                .Include(b => b.Trip)
                .FirstOrDefaultAsync(b => b.BookingId == id);

            if (booking == null) return NotFound();
            if (booking.Status == "Cancelled")
                return BadRequest(new { message = "Booking đã bị hủy" });
            if (booking.Trip.Status == "Departed" || booking.Trip.Status == "Completed")
                return BadRequest(new { message = "Chuyến đã xuất phát, không thể đổi ghế" });

            var newSeat = await _context.Seats
                .FirstOrDefaultAsync(s => s.SeatId == request.NewSeatId && !s.IsBooked);
            if (newSeat == null)
                return BadRequest(new { message = "Ghế mới không tồn tại hoặc đã được đặt" });

            if (newSeat.VehicleId != booking.Trip.VehicleId)
                return BadRequest(new { message = "Ghế không thuộc xe của chuyến này" });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Hoàn ghế cũ
                if (booking.Seat != null)
                    booking.Seat.IsBooked = false;

                // Đặt ghế mới
                newSeat.IsBooked = true;
                booking.SeatId = request.NewSeatId;

                // Cập nhật Ticket
                var ticket = booking.Tickets.FirstOrDefault();
                if (ticket != null)
                {
                    ticket.SeatId = request.NewSeatId;
                    // Tạo lại QR code
                    ticket.QrCode = GenerateQrCode(booking.BookingId, booking.TripId, request.NewSeatId);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    message = "Đổi ghế thành công",
                    newSeat = new { newSeat.SeatId, newSeat.SeatNumber, newSeat.SeatType },
                    newQrCode = ticket?.QrCode
                });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ============================================================
        // GET api/staff-sales/bookings/search?phone=&bookingId=
        // Tìm booking của khách (theo SĐT hoặc bookingId)
        // ============================================================
        [HttpGet("bookings/search")]
        public async Task<IActionResult> SearchBooking(
            [FromQuery] string? phone = null,
            [FromQuery] int? bookingId = null)
        {
            if (string.IsNullOrEmpty(phone) && !bookingId.HasValue)
                return BadRequest(new { message = "Cần cung cấp SĐT hoặc mã booking" });

            IQueryable<Booking> query = _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Trip).ThenInclude(t => t.Route)
                .Include(b => b.Seat)
                .Include(b => b.Payment)
                .Include(b => b.Tickets);

            if (bookingId.HasValue)
                query = query.Where(b => b.BookingId == bookingId.Value);
            else if (!string.IsNullOrEmpty(phone))
                query = query.Where(b => b.Customer != null && b.Customer.Phone == phone);

            var bookings = await query
                .OrderByDescending(b => b.BookingDate)
                .Take(20)
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
                TripStatus = b.Trip.Status,
                SeatNumber = b.Seat?.SeatNumber,
                SeatType = b.Seat?.SeatType,
                Amount = b.Payment?.Amount,
                PaymentStatus = b.Payment?.PaymentStatus,
                QrCode = b.Tickets.FirstOrDefault()?.QrCode,
                TicketStatus = b.Tickets.FirstOrDefault()?.TicketStatus
            }));
        }

        // ============================================================
        // Helpers
        // ============================================================
        private static string GenerateQrCode(int bookingId, int tripId, int seatId)
            => $"BUS-{bookingId:D6}-TRIP{tripId}-SEAT{seatId}-{DateTime.Now:yyyyMMddHHmmss}";
    }
}