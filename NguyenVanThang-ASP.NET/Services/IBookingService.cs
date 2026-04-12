// Services/IBookingService.cs
using NguyenVanThang_ASP.NET.DTOs;
using NguyenVanThang_ASP.NET.Models;

namespace NguyenVanThang_ASP.NET.Services
{
    public interface IBookingService
    {
        Task<BookingResponse> CreateBookingAsync(CreateBookingRequest request, int customerId);
        Task<List<BookingResponse>> GetBookingsByCustomerAsync(int customerId);
        Task<bool> CancelBookingAsync(int bookingId, int customerId);
    }
}