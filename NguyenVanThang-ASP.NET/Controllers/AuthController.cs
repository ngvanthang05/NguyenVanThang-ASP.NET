// Controllers/AuthController.cs — CẬP NHẬT: thêm claim DriverId, Staff check, IsActive guard
using System.Text;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using NguyenVanThang_ASP.NET.Data;
using NguyenVanThang_ASP.NET.Models;
using NguyenVanThang_ASP.NET.DTOs;
using Microsoft.EntityFrameworkCore;

namespace NguyenVanThang_ASP.NET.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public AuthController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // ============================================================
        // POST api/auth/register — Đăng ký (chỉ Customer tự đăng ký)
        // ============================================================
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { message = "Username và password không được để trống" });

            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
                return BadRequest(new { message = "Username đã tồn tại" });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var customer = new Customer
                {
                    Name = request.FullName,
                    Phone = request.Phone,
                    Email = request.Email,
                    CreatedAt = DateTime.Now
                };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                var user = new User
                {
                    Username = request.Username,
                    Password = HashPassword(request.Password),
                    Role = "Customer",
                    CustomerId = customer.CustomerId,
                    IsActive = true
                };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return Ok(new { message = "Đăng ký thành công", customerId = customer.CustomerId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { error = ex.Message, inner = ex.InnerException?.Message });
            }
        }

        // ============================================================
        // POST api/auth/login — Đăng nhập (tất cả roles)
        // ============================================================
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var hashedPassword = HashPassword(request.Password);
            var user = await _context.Users.Include(u => u.Customer)
                .FirstOrDefaultAsync(x => x.Username == request.Username && x.Password == hashedPassword);

            if (user == null) return Unauthorized(new { message = "Sai tài khoản hoặc mật khẩu" });

            if (!user.IsActive) return Unauthorized(new { message = "Tài khoản đã bị vô hiệu hóa" });

            var token = await GenerateJwtToken(user);
            return Ok(new
            {
                token,
                userId = user.UserId,
                username = user.Username,
                role = user.Role,
                customerId = user.CustomerId,
                fullName = user.Customer?.Name
            });
        }

        // ============================================================
        // POST api/auth/change-password — Đổi mật khẩu
        // ============================================================
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
        {
            var userId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            if (user.Password != HashPassword(request.OldPassword))
                return BadRequest(new { message = "Mật khẩu cũ không đúng" });

            user.Password = HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đổi mật khẩu thành công" });
        }

        // ============================================================
        // GET api/auth/me — Thông tin user hiện tại
        // ============================================================
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMe()
        {
            var userId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            var user = await _context.Users.Include(u => u.Customer)
                .FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null) return NotFound();

            // Trả về profile tương ứng role
            object? profile = null;
            if (user.Role == "Staff")
                profile = await _context.Staffs.FirstOrDefaultAsync(s => s.UserId == userId);
            else if (user.Role == "Driver")
                profile = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == userId);

            return Ok(new
            {
                user.UserId,
                user.Username,
                user.Role,
                user.IsActive,
                Customer = user.Customer != null ? new { user.Customer.Name, user.Customer.Phone, user.Customer.Email } : null,
                Profile = profile
            });
        }

        // ============================================================
        // Helpers
        // ============================================================
        private async Task<string> GenerateJwtToken(User user)
        {
            var jwtKey = _config["Jwt:Key"]!;
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim("id", user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            };

            // Thêm claim CustomerId nếu là Customer
            if (user.CustomerId.HasValue)
                claims.Add(new Claim("customerId", user.CustomerId.Value.ToString()));

            // Thêm claim DriverId nếu là Driver
            if (user.Role == "Driver")
            {
                var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.UserId == user.UserId);
                if (driver != null)
                    claims.Add(new Claim("driverId", driver.DriverId.ToString()));
            }

            // Thêm claim StaffId nếu là Staff
            if (user.Role == "Staff")
            {
                var staff = await _context.Staffs.FirstOrDefaultAsync(s => s.UserId == user.UserId);
                if (staff != null)
                    claims.Add(new Claim("staffId", staff.StaffId.ToString()));
            }

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes);
        }
    }
}