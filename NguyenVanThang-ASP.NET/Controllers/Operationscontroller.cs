// Controllers/OperationsController.cs — PORTAL 2: Điều hành
// Roles được phép: Admin, Operations
// Chức năng: Lên lịch chuyến, phân công tài xế, theo dõi vận hành

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NguyenVanThang_ASP.NET.Data;
using NguyenVanThang_ASP.NET.DTOs;
using NguyenVanThang_ASP.NET.Models;

namespace NguyenVanThang_ASP.NET.Controllers
{
    [Route("api/operations")]
    [ApiController]
    [Authorize(Roles = "Admin,Operations")]
    public class OperationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public OperationsController(AppDbContext context) => _context = context;

        // ============================================================
        // TRIP SCHEDULING — Lên lịch chuyến xe
        // ============================================================

        // GET api/operations/trips?date=&status=
        [HttpGet("trips")]
        public async Task<IActionResult> GetTrips(
            [FromQuery] DateTime? date = null,
            [FromQuery] string? status = null)
        {
            var query = _context.Trips
                .Include(t => t.Route)
                .Include(t => t.Vehicle).ThenInclude(v => v.Seats)
                .Include(t => t.Bookings)
                .AsQueryable();

            if (date.HasValue)
                query = query.Where(t => t.DepartureTime.Date == date.Value.Date);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(t => t.Status == status);

            var trips = await query.OrderBy(t => t.DepartureTime).ToListAsync();

            // Lấy phân công tài xế cho từng chuyến
            var tripIds = trips.Select(t => t.TripId).ToList();
            var assignments = await _context.TripAssignments
                .Include(ta => ta.Driver)
                .Where(ta => tripIds.Contains(ta.TripId))
                .ToListAsync();

            var result = trips.Select(t =>
            {
                var assignment = assignments.FirstOrDefault(a => a.TripId == t.TripId);
                return new TripMonitorDto
                {
                    TripId = t.TripId,
                    Departure = t.Route.Departure,
                    Destination = t.Route.Destination,
                    DepartureTime = t.DepartureTime,
                    ArrivalTime = t.ArrivalTime,
                    Status = t.Status,
                    Price = t.Price,
                    LicensePlate = t.Vehicle.LicensePlate,
                    VehicleType = t.Vehicle.VehicleType,
                    TotalSeats = t.Vehicle.SeatCount,
                    BookedSeats = t.Bookings.Count(b => b.Status != "Cancelled"),
                    AvailableSeats = t.Vehicle.Seats.Count(s => !s.IsBooked),
                    DriverName = assignment?.Driver.FullName,
                    DriverPhone = assignment?.Driver.Phone
                };
            });

            return Ok(result);
        }

        // POST api/operations/trips — Tạo lịch chuyến mới
        [HttpPost("trips")]
        public async Task<IActionResult> CreateTrip([FromBody] CreateTripRequest request)
        {
            // Validate Route & Vehicle tồn tại
            var route = await _context.BusRoutes.FindAsync(request.RouteId);
            if (route == null) return BadRequest(new { message = "Tuyến đường không tồn tại" });

            var vehicle = await _context.Vehicles.Include(v => v.Seats)
                .FirstOrDefaultAsync(v => v.VehicleId == request.VehicleId);
            if (vehicle == null) return BadRequest(new { message = "Xe không tồn tại" });

            // Kiểm tra xe có bị trùng lịch không
            var conflicted = await _context.Trips.AnyAsync(t =>
                t.VehicleId == request.VehicleId &&
                t.Status == "Active" &&
                t.DepartureTime < request.ArrivalTime &&
                t.ArrivalTime > request.DepartureTime);

            if (conflicted)
                return BadRequest(new { message = "Xe đã có lịch trùng trong khoảng thời gian này" });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Reset ghế xe (cho chuyến mới)
                foreach (var seat in vehicle.Seats)
                    seat.IsBooked = false;

                var trip = new Trip
                {
                    RouteId = request.RouteId,
                    VehicleId = request.VehicleId,
                    DepartureTime = request.DepartureTime,
                    ArrivalTime = request.ArrivalTime,
                    Price = request.Price,
                    Status = "Active"
                };
                _context.Trips.Add(trip);
                await _context.SaveChangesAsync();

                // Phân công tài xế ngay nếu có
                if (request.DriverId.HasValue)
                {
                    var driver = await _context.Drivers.FindAsync(request.DriverId.Value);
                    if (driver == null)
                        return BadRequest(new { message = "Tài xế không tồn tại" });

                    // Kiểm tra tài xế có bị trùng không
                    var driverConflict = await _context.TripAssignments
                        .Include(ta => ta.Trip)
                        .AnyAsync(ta => ta.DriverId == request.DriverId.Value &&
                                        ta.Trip.Status == "Active" &&
                                        ta.Trip.DepartureTime < request.ArrivalTime &&
                                        ta.Trip.ArrivalTime > request.DepartureTime);

                    if (driverConflict)
                        return BadRequest(new { message = "Tài xế đã được phân công chuyến khác trong khoảng thời gian này" });

                    _context.TripAssignments.Add(new TripAssignment
                    {
                        TripId = trip.TripId,
                        DriverId = request.DriverId.Value,
                        Note = request.AssignNote
                    });
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                return Ok(new { message = "Tạo chuyến xe thành công", tripId = trip.TripId });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // PUT api/operations/trips/{id}/status — Cập nhật trạng thái chuyến
        [HttpPut("trips/{id}/status")]
        public async Task<IActionResult> UpdateTripStatus(int id, [FromBody] UpdateTripStatusRequest request)
        {
            var allowed = new[] { "Active", "Departed", "Arrived", "Completed", "Cancelled" };
            if (!allowed.Contains(request.Status))
                return BadRequest(new { message = "Trạng thái không hợp lệ" });

            var trip = await _context.Trips.FindAsync(id);
            if (trip == null) return NotFound();

            trip.Status = request.Status;
            await _context.SaveChangesAsync();
            return Ok(new { message = $"Đã cập nhật trạng thái chuyến thành '{request.Status}'" });
        }

        // PUT api/operations/trips/{id} — Sửa thông tin chuyến
        [HttpPut("trips/{id}")]
        public async Task<IActionResult> UpdateTrip(int id, [FromBody] CreateTripRequest request)
        {
            var trip = await _context.Trips.FindAsync(id);
            if (trip == null) return NotFound();
            if (trip.Status == "Departed" || trip.Status == "Completed")
                return BadRequest(new { message = "Không thể sửa chuyến đã xuất phát hoặc hoàn thành" });

            trip.RouteId = request.RouteId;
            trip.VehicleId = request.VehicleId;
            trip.DepartureTime = request.DepartureTime;
            trip.ArrivalTime = request.ArrivalTime;
            trip.Price = request.Price;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Cập nhật chuyến xe thành công" });
        }

        // DELETE api/operations/trips/{id} — Hủy chuyến
        [HttpDelete("trips/{id}")]
        public async Task<IActionResult> CancelTrip(int id)
        {
            var trip = await _context.Trips
                .Include(t => t.Bookings).ThenInclude(b => b.Tickets)
                .Include(t => t.Bookings).ThenInclude(b => b.Payment)
                .Include(t => t.Bookings).ThenInclude(b => b.Seat)
                .FirstOrDefaultAsync(t => t.TripId == id);

            if (trip == null) return NotFound();
            if (trip.Status == "Completed")
                return BadRequest(new { message = "Chuyến đã hoàn thành, không thể hủy" });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                trip.Status = "Cancelled";

                // Hủy tất cả booking liên quan & hoàn ghế
                foreach (var booking in trip.Bookings.Where(b => b.Status != "Cancelled"))
                {
                    booking.Status = "Cancelled";
                    foreach (var ticket in booking.Tickets)
                        ticket.TicketStatus = "Cancelled";
                    if (booking.Payment != null)
                        booking.Payment.PaymentStatus = "Refunded";
                    if (booking.Seat != null)
                        booking.Seat.IsBooked = false;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok(new { message = "Đã hủy chuyến và hoàn vé cho hành khách" });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ============================================================
        // DRIVER ASSIGNMENT — Phân công tài xế
        // ============================================================

        // GET api/operations/trips/{id}/assignment
        [HttpGet("trips/{id}/assignment")]
        public async Task<IActionResult> GetTripAssignment(int id)
        {
            var assignment = await _context.TripAssignments
                .Include(ta => ta.Driver)
                .Include(ta => ta.Trip).ThenInclude(t => t.Route)
                .FirstOrDefaultAsync(ta => ta.TripId == id);

            if (assignment == null)
                return Ok(new { message = "Chuyến chưa có tài xế được phân công", assignment = (object?)null });

            return Ok(new TripAssignmentDto
            {
                AssignmentId = assignment.AssignmentId,
                TripId = assignment.TripId,
                Departure = assignment.Trip.Route.Departure,
                Destination = assignment.Trip.Route.Destination,
                DepartureTime = assignment.Trip.DepartureTime,
                ArrivalTime = assignment.Trip.ArrivalTime,
                TripStatus = assignment.Trip.Status,
                DriverId = assignment.DriverId,
                DriverName = assignment.Driver.FullName,
                DriverPhone = assignment.Driver.Phone,
                AssignedAt = assignment.AssignedAt,
                Note = assignment.Note
            });
        }

        // POST api/operations/trips/{id}/assign — Phân công tài xế
        [HttpPost("trips/{id}/assign")]
        public async Task<IActionResult> AssignDriver(int id, [FromBody] AssignDriverRequest request)
        {
            var trip = await _context.Trips.FindAsync(id);
            if (trip == null) return NotFound(new { message = "Chuyến không tồn tại" });
            if (trip.Status == "Cancelled" || trip.Status == "Completed")
                return BadRequest(new { message = "Không thể phân công cho chuyến đã hủy hoặc hoàn thành" });

            var driver = await _context.Drivers.FindAsync(request.DriverId);
            if (driver == null) return NotFound(new { message = "Tài xế không tồn tại" });
            if (driver.Status != "Active")
                return BadRequest(new { message = "Tài xế không hoạt động" });

            // Kiểm tra tài xế có chuyến trùng không
            var conflict = await _context.TripAssignments
                .Include(ta => ta.Trip)
                .AnyAsync(ta => ta.DriverId == request.DriverId &&
                                ta.AssignmentId != 0 &&
                                ta.Trip.Status == "Active" &&
                                ta.Trip.DepartureTime < trip.ArrivalTime &&
                                ta.Trip.ArrivalTime > trip.DepartureTime);

            if (conflict)
                return BadRequest(new { message = "Tài xế đã có lịch chuyến trùng giờ" });

            // Xóa phân công cũ nếu có
            var existing = await _context.TripAssignments.FirstOrDefaultAsync(ta => ta.TripId == id);
            if (existing != null) _context.TripAssignments.Remove(existing);

            _context.TripAssignments.Add(new TripAssignment
            {
                TripId = id,
                DriverId = request.DriverId,
                Note = request.Note
            });
            await _context.SaveChangesAsync();
            return Ok(new { message = $"Đã phân công tài xế '{driver.FullName}' cho chuyến #{id}" });
        }

        // DELETE api/operations/trips/{id}/assign — Hủy phân công
        [HttpDelete("trips/{id}/assign")]
        public async Task<IActionResult> UnassignDriver(int id)
        {
            var assignment = await _context.TripAssignments.FirstOrDefaultAsync(ta => ta.TripId == id);
            if (assignment == null) return NotFound(new { message = "Chuyến chưa có phân công tài xế" });

            _context.TripAssignments.Remove(assignment);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã hủy phân công tài xế" });
        }

        // ============================================================
        // GET api/operations/trips/{id}/passengers — Danh sách hành khách
        // ============================================================
        [HttpGet("trips/{id}/passengers")]
        public async Task<IActionResult> GetPassengers(int id)
        {
            var trip = await _context.Trips.FindAsync(id);
            if (trip == null) return NotFound();

            var bookings = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Seat)
                .Include(b => b.Payment)
                .Include(b => b.Tickets)
                .Where(b => b.TripId == id)
                .ToListAsync();

            // Lấy check-in status
            var ticketIds = bookings.SelectMany(b => b.Tickets).Select(t => t.TicketId).ToList();
            var checkedInTickets = await _context.Checkins
                .Where(c => ticketIds.Contains(c.TicketId))
                .Select(c => c.TicketId)
                .ToListAsync();

            var result = bookings.Select(b => new PassengerDto
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
                IsCheckedIn = b.Tickets.Any(t => checkedInTickets.Contains(t.TicketId))
            });

            return Ok(new
            {
                TripId = id,
                TotalPassengers = bookings.Count(b => b.Status != "Cancelled"),
                CheckedIn = bookings.Count(b => b.Tickets.Any(t => checkedInTickets.Contains(t.TicketId))),
                Passengers = result
            });
        }

        // ============================================================
        // GET api/operations/monitor — Dashboard vận hành realtime
        // ============================================================
        [HttpGet("monitor")]
        public async Task<IActionResult> GetMonitor()
        {
            var now = DateTime.Now;
            var todayStart = DateTime.Today;
            var todayEnd = todayStart.AddDays(1);

            var trips = await _context.Trips
                .Include(t => t.Route)
                .Include(t => t.Vehicle).ThenInclude(v => v.Seats)
                .Include(t => t.Bookings)
                .Where(t => t.DepartureTime >= todayStart && t.DepartureTime < todayEnd)
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
                return new TripMonitorDto
                {
                    TripId = t.TripId,
                    Departure = t.Route.Departure,
                    Destination = t.Route.Destination,
                    DepartureTime = t.DepartureTime,
                    ArrivalTime = t.ArrivalTime,
                    Status = t.Status,
                    Price = t.Price,
                    LicensePlate = t.Vehicle.LicensePlate,
                    VehicleType = t.Vehicle.VehicleType,
                    TotalSeats = t.Vehicle.SeatCount,
                    BookedSeats = t.Bookings.Count(b => b.Status != "Cancelled"),
                    AvailableSeats = t.Vehicle.Seats.Count(s => !s.IsBooked),
                    DriverName = assignment?.Driver.FullName,
                    DriverPhone = assignment?.Driver.Phone
                };
            });

            return Ok(new
            {
                Date = todayStart.ToString("dd/MM/yyyy"),
                Summary = new
                {
                    Total = trips.Count,
                    Active = trips.Count(t => t.Status == "Active"),
                    Departed = trips.Count(t => t.Status == "Departed"),
                    Completed = trips.Count(t => t.Status == "Completed"),
                    Cancelled = trips.Count(t => t.Status == "Cancelled"),
                    WithoutDriver = trips.Count(t => !assignments.Any(a => a.TripId == t.TripId))
                },
                Trips = result
            });
        }

        // ============================================================
        // GET api/operations/drivers/available?from=&to=
        // Danh sách tài xế rảnh trong khoảng thời gian
        // ============================================================
        [HttpGet("drivers/available")]
        public async Task<IActionResult> GetAvailableDrivers(
            [FromQuery] DateTime from,
            [FromQuery] DateTime to)
        {
            // Lấy tài xế có chuyến trùng
            var busyDriverIds = await _context.TripAssignments
                .Include(ta => ta.Trip)
                .Where(ta => ta.Trip.Status == "Active" &&
                             ta.Trip.DepartureTime < to &&
                             ta.Trip.ArrivalTime > from)
                .Select(ta => ta.DriverId)
                .ToListAsync();

            var available = await _context.Drivers
                .Where(d => d.Status == "Active" && !busyDriverIds.Contains(d.DriverId))
                .Select(d => new { d.DriverId, d.FullName, d.Phone, d.LicenseNumber, d.ExperienceYears })
                .ToListAsync();

            return Ok(available);
        }
    }
}