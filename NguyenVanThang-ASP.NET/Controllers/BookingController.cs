// Controllers/BookingController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NguyenVanThang_ASP.NET.Services;
using NguyenVanThang_ASP.NET.DTOs;

namespace NguyenVanThang_ASP.NET.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class BookingsController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public BookingsController(IBookingService bookingService)
            => _bookingService = bookingService;

        // POST: api/bookings — đặt vé
        [HttpPost]
        public async Task<IActionResult> CreateBooking(CreateBookingRequest request)
        {
            try
            {
                var customerId = int.Parse(User.FindFirst("customerId")?.Value ?? "0");
                if (customerId == 0) return Unauthorized(new { message = "Tài khoản chưa liên kết khách hàng" });

                var result = await _bookingService.CreateBookingAsync(request, customerId);
                return CreatedAtAction(nameof(GetMyBookings), result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET: api/bookings/my — lịch sử đặt vé của tôi
        [HttpGet("my")]
        public async Task<IActionResult> GetMyBookings()
        {
            var customerId = int.Parse(User.FindFirst("customerId")?.Value ?? "0");
            if (customerId == 0) return Unauthorized();

            var result = await _bookingService.GetBookingsByCustomerAsync(customerId);
            return Ok(result);
        }

        // DELETE: api/bookings/5/cancel — hủy vé
        [HttpDelete("{id}/cancel")]
        public async Task<IActionResult> CancelBooking(int id)
        {
            try
            {
                var customerId = int.Parse(User.FindFirst("customerId")?.Value ?? "0");
                var success = await _bookingService.CancelBookingAsync(id, customerId);

                if (!success) return NotFound(new { message = "Không tìm thấy booking" });
                return Ok(new { message = "Hủy vé thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET: api/bookings — Admin xem tất cả
        [HttpGet]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetAllBookings()
        {
            // Inject trực tiếp context để admin xem tất cả
            return Ok(new { message = "Implement in Admin Service" });
        }
    }
}