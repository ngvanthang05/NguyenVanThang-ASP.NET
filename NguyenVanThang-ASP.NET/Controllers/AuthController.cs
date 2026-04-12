// Controllers/AuthController.cs
using System.Text;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using NguyenVanThang_ASP.NET.Data;
using NguyenVanThang_ASP.NET.Models;
using NguyenVanThang_ASP.NET.DTOs;


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

        // POST api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            if (_context.Users.Any(u => u.Username == request.Username))
                return BadRequest(new { message = "Username đã tồn tại" });

            var user = new User
            {
                Username = request.Username,
                Password = HashPassword(request.Password),
                Role = "Customer"
            };
            _context.Users.Add(user);

            // Tạo Customer tương ứng
            var customer = new Customer
            {
                Name = request.FullName,
                Phone = request.Phone,
                Email = request.Email,
                CreatedAt = DateTime.Now
            };
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            // Link User -> Customer
            user.CustomerId = customer.CustomerId;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đăng ký thành công" });
        }

        // POST api/auth/login
        [HttpPost("login")]
        public IActionResult Login(LoginRequest request)
        {
            var hashedPassword = HashPassword(request.Password);
            var user = _context.Users.FirstOrDefault(
                x => x.Username == request.Username && x.Password == hashedPassword);

            if (user == null) return Unauthorized(new { message = "Sai tài khoản hoặc mật khẩu" });

            var token = GenerateJwtToken(user);
            return Ok(new { token, role = user.Role, userId = user.UserId });
        }

        // POST api/auth/change-password
        [HttpPost("change-password")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
        {
            var userId = int.Parse(User.FindFirst("id")?.Value!);
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            if (user.Password != HashPassword(request.OldPassword))
                return BadRequest(new { message = "Mật khẩu cũ không đúng" });

            user.Password = HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đổi mật khẩu thành công" });
        }

        private string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                claims: new[]
                {
                    new Claim("id", user.UserId.ToString()),
                    new Claim("customerId", user.CustomerId?.ToString() ?? "0"),
                    new Claim(ClaimTypes.Role, user.Role)
                },
                expires: DateTime.Now.AddHours(8),
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }
    }
}