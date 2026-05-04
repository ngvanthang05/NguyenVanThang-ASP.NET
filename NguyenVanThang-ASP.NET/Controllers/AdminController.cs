// Controllers/AdminController.cs — PORTAL 1: Admin
// Roles được phép: Admin
// Chức năng: Dashboard, báo cáo doanh thu, quản lý Staff, Driver, Users, cấu hình

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using NguyenVanThang_ASP.NET.Data;
using NguyenVanThang_ASP.NET.DTOs;
using NguyenVanThang_ASP.NET.Models;

namespace NguyenVanThang_ASP.NET.Controllers
{
    [Route("api/admin")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context) => _context = context;

        // ============================================================
        // GET api/admin/dashboard — Tổng quan hệ thống
        // ============================================================
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var today = DateTime.Today;
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var last7Days = today.AddDays(-6);

            // Doanh thu & booking hôm nay
            var todayPayments = await _context.Payments
                .Where(p => p.PaymentStatus == "Paid" &&
                            _context.Bookings.Any(b =>
                                b.BookingId == p.BookingId &&
                                b.BookingDate.Date == today))
                .ToListAsync();

            var monthPayments = await _context.Payments
                .Where(p => p.PaymentStatus == "Paid" &&
                            _context.Bookings.Any(b =>
                                b.BookingId == p.BookingId &&
                                b.BookingDate >= monthStart))
                .ToListAsync();

            // Thống kê chuyến xe
            var tripsToday = await _context.Trips
                .CountAsync(t => t.DepartureTime.Date == today);
            var tripsMonth = await _context.Trips
                .CountAsync(t => t.DepartureTime >= monthStart);
            var activeTrips = await _context.Trips
                .CountAsync(t => t.Status == "Active" || t.Status == "Departed");
            var cancelledMonth = await _context.Trips
                .CountAsync(t => t.Status == "Cancelled" && t.DepartureTime >= monthStart);

            // Doanh thu 7 ngày gần nhất
            var revenueLast7 = new List<RevenueByDay>();
            for (int i = 6; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                var dayPayments = await _context.Payments
                    .Where(p => p.PaymentStatus == "Paid" &&
                                _context.Bookings.Any(b =>
                                    b.BookingId == p.BookingId &&
                                    b.BookingDate.Date == date))
                    .ToListAsync();
                var dayBookings = await _context.Bookings
                    .CountAsync(b => b.BookingDate.Date == date && b.Status != "Cancelled");
                revenueLast7.Add(new RevenueByDay
                {
                    Date = date.ToString("dd/MM"),
                    Revenue = dayPayments.Sum(p => p.Amount),
                    Bookings = dayBookings
                });
            }

            // Top 5 tuyến đường theo doanh thu
            var topRoutes = await _context.Trips
                .Where(t => t.DepartureTime >= monthStart)
                .Include(t => t.Route)
                .Include(t => t.Bookings).ThenInclude(b => b.Payment)
                .GroupBy(t => new { t.Route.Departure, t.Route.Destination })
                .Select(g => new TopRouteDto
                {
                    Departure = g.Key.Departure,
                    Destination = g.Key.Destination,
                    TripCount = g.Count(),
                    TotalRevenue = g.SelectMany(t => t.Bookings)
                                    .Where(b => b.Payment != null && b.Payment.PaymentStatus == "Paid")
                                    .Sum(b => b.Payment!.Amount)
                })
                .OrderByDescending(r => r.TotalRevenue)
                .Take(5)
                .ToListAsync();

            return Ok(new DashboardResponse
            {
                TotalTripsToday = tripsToday,
                TotalTripsMonth = tripsMonth,
                RevenueToday = todayPayments.Sum(p => p.Amount),
                RevenueMonth = monthPayments.Sum(p => p.Amount),
                TotalCustomers = await _context.Customers.CountAsync(),
                TotalVehicles = await _context.Vehicles.CountAsync(),
                TotalDrivers = await _context.Drivers.CountAsync(d => d.Status == "Active"),
                TotalStaff = await _context.Staffs.CountAsync(s => s.Status == "Active"),
                ActiveTrips = activeTrips,
                CancelledTripsMonth = cancelledMonth,
                RevenueLast7Days = revenueLast7,
                TopRoutes = topRoutes
            });
        }

        // ============================================================
        // GET api/admin/reports/revenue?from=&to=&groupBy=Day
        // ============================================================
        [HttpGet("reports/revenue")]
        public async Task<IActionResult> GetRevenueReport(
            [FromQuery] DateTime from,
            [FromQuery] DateTime to,
            [FromQuery] string groupBy = "Day")
        {
            if (to < from)
                return BadRequest(new { message = "Ngày 'to' phải lớn hơn 'from'" });

            var payments = await _context.Payments
                .Where(p => p.PaymentStatus == "Paid")
                .Include(p => p.Booking)
                .Where(p => p.Booking.BookingDate >= from && p.Booking.BookingDate <= to.AddDays(1))
                .ToListAsync();

            // Group theo ngày
            var details = payments
                .GroupBy(p => p.Booking.BookingDate.Date)
                .OrderBy(g => g.Key)
                .Select(g => new RevenueByDay
                {
                    Date = g.Key.ToString("dd/MM/yyyy"),
                    Revenue = g.Sum(p => p.Amount),
                    Bookings = g.Count()
                }).ToList();

            return Ok(new RevenueReportResponse
            {
                From = from,
                To = to,
                TotalRevenue = payments.Sum(p => p.Amount),
                TotalBookings = payments.Count,
                TotalTrips = await _context.Trips.CountAsync(t =>
                    t.DepartureTime >= from && t.DepartureTime <= to.AddDays(1)),
                Details = details
            });
        }

        // ============================================================
        // GET api/admin/reports/trips?from=&to=
        // ============================================================
        [HttpGet("reports/trips")]
        public async Task<IActionResult> GetTripReport(
            [FromQuery] DateTime from,
            [FromQuery] DateTime to)
        {
            var trips = await _context.Trips
                .Include(t => t.Route)
                .Include(t => t.Vehicle)
                .Include(t => t.Bookings).ThenInclude(b => b.Payment)
                .Where(t => t.DepartureTime >= from && t.DepartureTime <= to.AddDays(1))
                .OrderByDescending(t => t.DepartureTime)
                .ToListAsync();

            var result = trips.Select(t => new
            {
                t.TripId,
                t.Route.Departure,
                t.Route.Destination,
                t.DepartureTime,
                t.ArrivalTime,
                t.Status,
                t.Price,
                t.Vehicle.LicensePlate,
                TotalSeats = t.Vehicle.SeatCount,
                BookedSeats = t.Bookings.Count(b => b.Status != "Cancelled"),
                Revenue = t.Bookings
                    .Where(b => b.Payment?.PaymentStatus == "Paid")
                    .Sum(b => b.Payment?.Amount ?? 0)
            });

            return Ok(new
            {
                From = from,
                To = to,
                TotalTrips = trips.Count,
                TotalRevenue = trips.Sum(t => t.Bookings
                    .Where(b => b.Payment?.PaymentStatus == "Paid")
                    .Sum(b => b.Payment?.Amount ?? 0)),
                Trips = result
            });
        }

        // ============================================================
        // STAFF MANAGEMENT
        // GET api/admin/staff
        // ============================================================
        [HttpGet("staff")]
        public async Task<IActionResult> GetStaff()
        {
            var staffList = await _context.Staffs
                .Include(s => s.User)
                .Select(s => new StaffDto
                {
                    StaffId = s.StaffId,
                    UserId = s.UserId,
                    Name = s.Name,
                    Phone = s.Phone,
                    Role = s.Role,
                    Status = s.Status,
                    Username = s.User != null ? s.User.Username : null
                })
                .ToListAsync();
            return Ok(staffList);
        }

        // GET api/admin/staff/{id}
        [HttpGet("staff/{id}")]
        public async Task<IActionResult> GetStaffById(int id)
        {
            var staff = await _context.Staffs
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.StaffId == id);
            if (staff == null) return NotFound();
            return Ok(new StaffDto
            {
                StaffId = staff.StaffId,
                UserId = staff.UserId,
                Name = staff.Name,
                Phone = staff.Phone,
                Role = staff.Role,
                Status = staff.Status,
                Username = staff.User?.Username
            });
        }

        // POST api/admin/staff — Tạo nhân viên + tạo tài khoản User
        [HttpPost("staff")]
        public async Task<IActionResult> CreateStaff([FromBody] CreateStaffRequest request)
        {
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
                return BadRequest(new { message = "Username đã tồn tại" });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Tạo tài khoản User với role Staff
                var user = new User
                {
                    Username = request.Username,
                    Password = HashPassword(request.Password),
                    Role = "Staff",
                    IsActive = true
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Tạo hồ sơ Staff
                var staff = new Staff
                {
                    UserId = user.UserId,
                    Name = request.Name,
                    Phone = request.Phone,
                    Role = request.Role,
                    Status = "Active"
                };
                _context.Staffs.Add(staff);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return Ok(new { message = "Tạo nhân viên thành công", staffId = staff.StaffId, userId = user.UserId });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // PUT api/admin/staff/{id}
        [HttpPut("staff/{id}")]
        public async Task<IActionResult> UpdateStaff(int id, [FromBody] StaffDto dto)
        {
            var staff = await _context.Staffs.FindAsync(id);
            if (staff == null) return NotFound();

            staff.Name = dto.Name;
            staff.Phone = dto.Phone;
            staff.Role = dto.Role;
            staff.Status = dto.Status;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Cập nhật thành công" });
        }

        // DELETE api/admin/staff/{id}
        [HttpDelete("staff/{id}")]
        public async Task<IActionResult> DeleteStaff(int id)
        {
            var staff = await _context.Staffs.FindAsync(id);
            if (staff == null) return NotFound();

            // Chỉ deactivate, không xóa cứng (giữ lịch sử check-in)
            staff.Status = "Inactive";
            if (staff.UserId.HasValue)
            {
                var user = await _context.Users.FindAsync(staff.UserId.Value);
                if (user != null) user.IsActive = false;
            }
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã vô hiệu hóa nhân viên" });
        }

        // ============================================================
        // DRIVER MANAGEMENT
        // GET api/admin/drivers
        // ============================================================
        [HttpGet("drivers")]
        public async Task<IActionResult> GetDrivers()
        {
            var drivers = await _context.Drivers
                .Include(d => d.User)
                .Include(d => d.TripAssignments).ThenInclude(ta => ta.Trip).ThenInclude(t => t.Route)
                .ToListAsync();

            var result = drivers.Select(d =>
            {
                var upcoming = d.TripAssignments
                    .Where(ta => ta.Trip.DepartureTime > DateTime.Now && ta.Trip.Status == "Active")
                    .OrderBy(ta => ta.Trip.DepartureTime)
                    .FirstOrDefault();

                return new DriverDto
                {
                    DriverId = d.DriverId,
                    UserId = d.UserId,
                    FullName = d.FullName,
                    Phone = d.Phone,
                    LicenseNumber = d.LicenseNumber,
                    ExperienceYears = d.ExperienceYears,
                    Status = d.Status,
                    Username = d.User?.Username,
                    UpcomingTrip = upcoming != null ? new TripAssignmentDto
                    {
                        AssignmentId = upcoming.AssignmentId,
                        TripId = upcoming.TripId,
                        Departure = upcoming.Trip.Route.Departure,
                        Destination = upcoming.Trip.Route.Destination,
                        DepartureTime = upcoming.Trip.DepartureTime,
                        ArrivalTime = upcoming.Trip.ArrivalTime,
                        TripStatus = upcoming.Trip.Status,
                        DriverId = d.DriverId,
                        DriverName = d.FullName,
                        DriverPhone = d.Phone,
                        AssignedAt = upcoming.AssignedAt
                    } : null
                };
            });

            return Ok(result);
        }

        // POST api/admin/drivers — Tạo tài xế + tài khoản User
        [HttpPost("drivers")]
        public async Task<IActionResult> CreateDriver([FromBody] CreateDriverRequest request)
        {
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
                return BadRequest(new { message = "Username đã tồn tại" });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = new User
                {
                    Username = request.Username,
                    Password = HashPassword(request.Password),
                    Role = "Driver",
                    IsActive = true
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var driver = new Driver
                {
                    UserId = user.UserId,
                    FullName = request.FullName,
                    Phone = request.Phone,
                    LicenseNumber = request.LicenseNumber,
                    ExperienceYears = request.ExperienceYears,
                    Status = "Active"
                };
                _context.Drivers.Add(driver);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return Ok(new { message = "Tạo tài xế thành công", driverId = driver.DriverId, userId = user.UserId });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // PUT api/admin/drivers/{id}
        [HttpPut("drivers/{id}")]
        public async Task<IActionResult> UpdateDriver(int id, [FromBody] DriverDto dto)
        {
            var driver = await _context.Drivers.FindAsync(id);
            if (driver == null) return NotFound();

            driver.FullName = dto.FullName;
            driver.Phone = dto.Phone;
            driver.LicenseNumber = dto.LicenseNumber;
            driver.ExperienceYears = dto.ExperienceYears;
            driver.Status = dto.Status;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Cập nhật tài xế thành công" });
        }

        // DELETE api/admin/drivers/{id}
        [HttpDelete("drivers/{id}")]
        public async Task<IActionResult> DeleteDriver(int id)
        {
            var driver = await _context.Drivers.FindAsync(id);
            if (driver == null) return NotFound();
            driver.Status = "Inactive";
            if (driver.UserId.HasValue)
            {
                var user = await _context.Users.FindAsync(driver.UserId.Value);
                if (user != null) user.IsActive = false;
            }
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã vô hiệu hóa tài xế" });
        }

        // ============================================================
        // USER MANAGEMENT (quản lý tài khoản, đổi role, reset password)
        // GET api/admin/users
        // ============================================================
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users
                .Select(u => new { u.UserId, u.Username, u.Role, u.IsActive, u.CustomerId })
                .ToListAsync();
            return Ok(users);
        }

        // PUT api/admin/users/{id}/role
        [HttpPut("users/{id}/role")]
        public async Task<IActionResult> ChangeUserRole(int id, [FromBody] string role)
        {
            var allowed = new[] { "Admin", "Staff", "Driver", "Operations", "Customer" };
            if (!allowed.Contains(role))
                return BadRequest(new { message = "Role không hợp lệ" });

            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            user.Role = role;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã cập nhật role" });
        }

        // PUT api/admin/users/{id}/reset-password
        [HttpPut("users/{id}/reset-password")]
        public async Task<IActionResult> ResetPassword(int id, [FromBody] string newPassword)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            user.Password = HashPassword(newPassword);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã đặt lại mật khẩu" });
        }

        // PUT api/admin/users/{id}/toggle-active
        [HttpPut("users/{id}/toggle-active")]
        public async Task<IActionResult> ToggleUserActive(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();
            return Ok(new { message = user.IsActive ? "Đã kích hoạt" : "Đã vô hiệu hóa", isActive = user.IsActive });
        }

        // DELETE api/admin/users/{id}
        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return Ok();
        }

        // ============================================================
        // SYSTEM STATS — api/admin/system/stats
        // ============================================================
        [HttpGet("system/stats")]
        public async Task<IActionResult> GetSystemStats()
        {
            return Ok(new
            {
                TotalUsers = await _context.Users.CountAsync(),
                TotalCustomers = await _context.Customers.CountAsync(),
                TotalStaff = await _context.Staffs.CountAsync(),
                TotalDrivers = await _context.Drivers.CountAsync(),
                TotalVehicles = await _context.Vehicles.CountAsync(),
                TotalRoutes = await _context.BusRoutes.CountAsync(),
                TotalTrips = await _context.Trips.CountAsync(),
                TotalBookings = await _context.Bookings.CountAsync(),
                TotalTickets = await _context.Tickets.CountAsync(),
                TotalRevenue = await _context.Payments
                    .Where(p => p.PaymentStatus == "Paid")
                    .SumAsync(p => p.Amount),
                BookingsByStatus = new
                {
                    Confirmed = await _context.Bookings.CountAsync(b => b.Status == "Confirmed"),
                    Cancelled = await _context.Bookings.CountAsync(b => b.Status == "Cancelled"),
                    Pending = await _context.Bookings.CountAsync(b => b.Status == "Pending")
                },
                TripsByStatus = new
                {
                    Active = await _context.Trips.CountAsync(t => t.Status == "Active"),
                    Departed = await _context.Trips.CountAsync(t => t.Status == "Departed"),
                    Completed = await _context.Trips.CountAsync(t => t.Status == "Completed"),
                    Cancelled = await _context.Trips.CountAsync(t => t.Status == "Cancelled")
                }
            });
        }

        // ============================================================
        // HELPER
        // ============================================================
        private static string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes);
        }
    }
}