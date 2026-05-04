// Controllers/DriverController.cs — PORTAL 5: Tài xế (Mobile App)
// Roles: Driver
// Chức năng: Xem lịch chạy, danh sách hành khách, cập nhật trạng thái chuyến

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NguyenVanThang_ASP.NET.Data;
using NguyenVanThang_ASP.NET.DTOs;

namespace NguyenVanThang_ASP.NET.Controllers
{
    [Route("api/driver")]
    [ApiController]
    [Authorize(Roles = "Driver")]
    public class DriverController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DriverController(AppDbContext context) => _context = context;

        // Lấy DriverId từ JWT (UserId → Driver)
        private async Task<int?> GetDriverIdAsync()
        {
            var userId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);
            return driver?.DriverId;
        }

        // ============================================================
        // GET api/driver/profile — Thông tin tài xế
        // ============================================================
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var driverId = await GetDriverIdAsync();
            if (driverId == null) return NotFound(new { message = "Không tìm thấy hồ sơ tài xế" });

            var driver = await _context.Drivers
                .Include(d => d.TripAssignments).ThenInclude(ta => ta.Trip)
                .FirstOrDefaultAsync(d => d.DriverId == driverId);

            if (driver == null) return NotFound();

            return Ok(new DriverProfileDto
            {
                DriverId = driver.DriverId,
                FullName = driver.FullName,
                Phone = driver.Phone,
                LicenseNumber = driver.LicenseNumber,
                ExperienceYears = driver.ExperienceYears,
                Status = driver.Status,
                TotalTripsCompleted = driver.TripAssignments
                    .Count(ta => ta.Trip.Status == "Completed")
            });
        }

        // ============================================================
        // GET api/driver/schedule — Lịch chạy của tài xế
        // GET api/driver/schedule?date=2024-12-01 (lọc theo ngày)
        // ============================================================
        [HttpGet("schedule")]
        public async Task<IActionResult> GetSchedule([FromQuery] DateTime? date = null)
        {
            var driverId = await GetDriverIdAsync();
            if (driverId == null) return NotFound(new { message = "Không tìm thấy hồ sơ tài xế" });

            var query = _context.TripAssignments
                .Include(ta => ta.Trip).ThenInclude(t => t.Route)
                .Include(ta => ta.Trip).ThenInclude(t => t.Vehicle)
                .Include(ta => ta.Trip).ThenInclude(t => t.Bookings)
                .Where(ta => ta.DriverId == driverId);

            if (date.HasValue)
                query = query.Where(ta => ta.Trip.DepartureTime.Date == date.Value.Date);
            else
                // Mặc định: lịch từ hôm nay trở đi
                query = query.Where(ta => ta.Trip.DepartureTime >= DateTime.Today);

            var assignments = await query
                .OrderBy(ta => ta.Trip.DepartureTime)
                .ToListAsync();

            // Đếm số hành khách đã check-in cho mỗi chuyến
            var tripIds = assignments.Select(ta => ta.TripId).ToList();
            var checkinCounts = await _context.Checkins
                .Where(c => _context.Tickets
                    .Any(t => t.TicketId == c.TicketId &&
                              _context.Bookings.Any(b => b.BookingId == t.BookingId &&
                                                         tripIds.Contains(b.TripId))))
                .GroupBy(c => _context.Bookings
                    .FirstOrDefault(b => _context.Tickets
                        .Any(t => t.TicketId == c.TicketId && t.BookingId == b.BookingId))!.TripId)
                .Select(g => new { TripId = g.Key, Count = g.Count() })
                .ToListAsync();

            var result = assignments.Select(ta =>
            {
                var bookedCount = ta.Trip.Bookings.Count(b => b.Status != "Cancelled");
                var checkedIn = checkinCounts.FirstOrDefault(c => c.TripId == ta.TripId)?.Count ?? 0;

                return new DriverScheduleDto
                {
                    AssignmentId = ta.AssignmentId,
                    TripId = ta.TripId,
                    Departure = ta.Trip.Route.Departure,
                    Destination = ta.Trip.Route.Destination,
                    DepartureTime = ta.Trip.DepartureTime,
                    ArrivalTime = ta.Trip.ArrivalTime,
                    TripStatus = ta.Trip.Status,
                    LicensePlate = ta.Trip.Vehicle.LicensePlate,
                    VehicleType = ta.Trip.Vehicle.VehicleType,
                    TotalPassengers = bookedCount,
                    CheckedIn = checkedIn,
                    Note = ta.Note
                };
            });

            return Ok(result);
        }

        // ============================================================
        // GET api/driver/trips/{id} — Chi tiết chuyến
        // ============================================================
        [HttpGet("trips/{id}")]
        public async Task<IActionResult> GetTripDetail(int id)
        {
            var driverId = await GetDriverIdAsync();
            if (driverId == null) return NotFound(new { message = "Không tìm thấy hồ sơ tài xế" });

            // Xác nhận tài xế được phân công chuyến này
            var assigned = await _context.TripAssignments
                .AnyAsync(ta => ta.TripId == id && ta.DriverId == driverId);
            if (!assigned) return Forbid();

            var trip = await _context.Trips
                .Include(t => t.Route)
                .Include(t => t.Vehicle).ThenInclude(v => v.Seats)
                .FirstOrDefaultAsync(t => t.TripId == id);

            if (trip == null) return NotFound();

            return Ok(new
            {
                trip.TripId,
                Departure = trip.Route.Departure,
                Destination = trip.Route.Destination,
                trip.DepartureTime,
                trip.ArrivalTime,
                trip.Status,
                trip.Price,
                trip.Vehicle.LicensePlate,
                trip.Vehicle.VehicleType,
                TotalSeats = trip.Vehicle.SeatCount,
                BookedSeats = trip.Vehicle.Seats.Count(s => s.IsBooked)
            });
        }

        // ============================================================
        // GET api/driver/trips/{id}/passengers — DS hành khách trên chuyến
        // ============================================================
        [HttpGet("trips/{id}/passengers")]
        public async Task<IActionResult> GetPassengers(int id)
        {
            var driverId = await GetDriverIdAsync();
            if (driverId == null) return NotFound(new { message = "Không tìm thấy hồ sơ tài xế" });

            var assigned = await _context.TripAssignments
                .AnyAsync(ta => ta.TripId == id && ta.DriverId == driverId);
            if (!assigned) return Forbid();

            var bookings = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Seat)
                .Include(b => b.Tickets)
                .Where(b => b.TripId == id && b.Status != "Cancelled")
                .OrderBy(b => b.Seat!.SeatNumber)
                .ToListAsync();

            var ticketIds = bookings.SelectMany(b => b.Tickets).Select(t => t.TicketId).ToList();
            var checkedIn = await _context.Checkins
                .Where(c => ticketIds.Contains(c.TicketId))
                .Select(c => c.TicketId).ToListAsync();

            // Tài xế chỉ thấy thông tin cơ bản (không cần thấy PaymentMethod)
            return Ok(new
            {
                TripId = id,
                TotalPassengers = bookings.Count,
                CheckedIn = bookings.Count(b => b.Tickets.Any(t => checkedIn.Contains(t.TicketId))),
                Passengers = bookings.Select(b => new
                {
                    b.BookingId,
                    CustomerName = b.Customer?.Name,
                    CustomerPhone = b.Customer?.Phone,
                    SeatNumber = b.Seat?.SeatNumber,
                    SeatType = b.Seat?.SeatType,
                    IsCheckedIn = b.Tickets.Any(t => checkedIn.Contains(t.TicketId))
                })
            });
        }

        // ============================================================
        // PUT api/driver/trips/{id}/status — Tài xế cập nhật trạng thái chuyến
        // Chỉ cho phép: Departed | Arrived | Completed
        // ============================================================
        [HttpPut("trips/{id}/status")]
        public async Task<IActionResult> UpdateTripStatus(int id, [FromBody] DriverUpdateTripRequest request)
        {
            var driverId = await GetDriverIdAsync();
            if (driverId == null) return NotFound(new { message = "Không tìm thấy hồ sơ tài xế" });

            var assigned = await _context.TripAssignments
                .AnyAsync(ta => ta.TripId == id && ta.DriverId == driverId);
            if (!assigned) return Forbid();

            // Tài xế chỉ được cập nhật các trạng thái vận hành
            var allowedStatuses = new[] { "Departed", "Arrived", "Completed" };
            if (!allowedStatuses.Contains(request.Status))
                return BadRequest(new { message = $"Tài xế chỉ được cập nhật: {string.Join(", ", allowedStatuses)}" });

            var trip = await _context.Trips.FindAsync(id);
            if (trip == null) return NotFound();
            if (trip.Status == "Cancelled")
                return BadRequest(new { message = "Chuyến đã bị hủy" });
            if (trip.Status == "Completed")
                return BadRequest(new { message = "Chuyến đã hoàn thành" });

            // Validate thứ tự trạng thái hợp lệ
            var validTransitions = new Dictionary<string, string[]>
            {
                { "Active", new[] { "Departed" } },
                { "Departed", new[] { "Arrived", "Completed" } },
                { "Arrived", new[] { "Completed" } }
            };

            if (validTransitions.TryGetValue(trip.Status, out var next) && !next.Contains(request.Status))
                return BadRequest(new { message = $"Không thể chuyển từ '{trip.Status}' sang '{request.Status}'" });

            trip.Status = request.Status;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"Đã cập nhật trạng thái chuyến thành '{request.Status}'",
                tripId = id,
                status = request.Status,
                updatedAt = DateTime.Now
            });
        }
    }
}