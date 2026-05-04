// Controllers/CustomerController.cs — CẬP NHẬT: thêm auth đầy đủ
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NguyenVanThang_ASP.NET.Data;
using NguyenVanThang_ASP.NET.Models;

namespace NguyenVanThang_ASP.NET.Controllers
{
    [Route("api/customers")]
    [ApiController]
    public class CustomersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CustomersController(AppDbContext context) => _context = context;

        // GET api/customers — Admin xem danh sách khách hàng
        [HttpGet]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetCustomers(
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = _context.Customers.AsQueryable();
            if (!string.IsNullOrEmpty(search))
                query = query.Where(c =>
                    c.Name.Contains(search) ||
                    c.Phone.Contains(search) ||
                    c.Email.Contains(search));

            var total = await query.CountAsync();
            var customers = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new { total, page, pageSize, data = customers });
        }

        // GET api/customers/{id} — Admin xem, Customer xem của mình
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetCustomer(int id)
        {
            // Customer chỉ xem thông tin của chính mình
            if (User.IsInRole("Customer"))
            {
                var myCustomerId = int.Parse(User.FindFirst("customerId")?.Value ?? "0");
                if (myCustomerId != id) return Forbid();
            }

            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return NotFound();
            return Ok(customer);
        }

        // PUT api/customers/{id} — Customer tự cập nhật, Admin cập nhật bất kỳ
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateCustomer(int id, [FromBody] Customer dto)
        {
            if (User.IsInRole("Customer"))
            {
                var myCustomerId = int.Parse(User.FindFirst("customerId")?.Value ?? "0");
                if (myCustomerId != id) return Forbid();
            }

            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return NotFound();

            customer.Name = dto.Name;
            customer.Phone = dto.Phone;
            customer.Email = dto.Email;
            await _context.SaveChangesAsync();
            return Ok(customer);
        }

        // POST api/customers — Admin tạo khách hàng trực tiếp
        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> CreateCustomer([FromBody] Customer customer)
        {
            customer.CreatedAt = DateTime.Now;
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
            return Ok(customer);
        }

        // DELETE api/customers/{id} — Chỉ Admin
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return NotFound();
            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();
            return Ok();
        }

        // GET api/customers/{id}/bookings — Lịch sử đặt vé của khách
        [HttpGet("{id}/bookings")]
        [Authorize]
        public async Task<IActionResult> GetCustomerBookings(int id)
        {
            if (User.IsInRole("Customer"))
            {
                var myCustomerId = int.Parse(User.FindFirst("customerId")?.Value ?? "0");
                if (myCustomerId != id) return Forbid();
            }

            var bookings = await _context.Bookings
                .Include(b => b.Trip).ThenInclude(t => t.Route)
                .Include(b => b.Seat)
                .Include(b => b.Payment)
                .Include(b => b.Tickets)
                .Where(b => b.CustomerId == id)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            return Ok(bookings.Select(b => new
            {
                b.BookingId,
                b.Status,
                b.BookingDate,
                Departure = b.Trip.Route.Departure,
                Destination = b.Trip.Route.Destination,
                DepartureTime = b.Trip.DepartureTime,
                SeatNumber = b.Seat?.SeatNumber,
                Amount = b.Payment?.Amount,
                PaymentStatus = b.Payment?.PaymentStatus,
                QrCode = b.Tickets.FirstOrDefault()?.QrCode
            }));
        }
    }
}