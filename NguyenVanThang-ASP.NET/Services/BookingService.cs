// Services/BookingService.cs
using Microsoft.EntityFrameworkCore;
using NguyenVanThang_ASP.NET.Data;
using NguyenVanThang_ASP.NET.Models;
using NguyenVanThang_ASP.NET.DTOs;
using NguyenVanThang_ASP.NET.Services;

public class BookingService : IBookingService
{
    private readonly AppDbContext _context;

    public BookingService(AppDbContext context) => _context = context;

    public async Task<BookingResponse> CreateBookingAsync(CreateBookingRequest request, int customerId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Kiểm tra seat còn trống
            var seat = await _context.Seats
                .Include(s => s.Vehicle)
                .FirstOrDefaultAsync(s => s.SeatId == request.SeatId && !s.IsBooked);
            if (seat == null)
                throw new Exception("Ghế đã được đặt hoặc không tồn tại!");

            // 2. Lấy thông tin Trip
            var trip = await _context.Trips
                .Include(t => t.Route)
                .FirstOrDefaultAsync(t => t.TripId == request.TripId && t.Status == "Active");
            if (trip == null)
                throw new Exception("Chuyến xe không tồn tại hoặc đã hủy!");

            // 3. Kiểm tra seat thuộc vehicle của trip
            if (seat.VehicleId != trip.VehicleId)
                throw new Exception("Ghế không thuộc xe của chuyến này!");

            // 4. Lock ghế
            seat.IsBooked = true;

            // 5. Tạo Booking
            var booking = new Booking
            {
                CustomerId = customerId,
                TripId = request.TripId,
                SeatId = request.SeatId,
                BookingDate = DateTime.Now,
                Status = "Confirmed",
                CreatedAt = DateTime.Now
            };
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            // 6. Tạo Ticket với QR Code
            var qrCode = GenerateQrCode(booking.BookingId, request.TripId, request.SeatId);
            var ticket = new Ticket
            {
                BookingId = booking.BookingId,
                SeatId = request.SeatId,
                QrCode = qrCode,
                TicketStatus = "Valid"
            };
            _context.Tickets.Add(ticket);

            // 7. Tạo Payment
            var payment = new Payment
            {
                BookingId = booking.BookingId,
                Amount = trip.Price,
                PaymentMethod = request.PaymentMethod,
                PaymentStatus = request.PaymentMethod == "Cash" ? "Pending" : "Paid"
            };
            _context.Payments.Add(payment);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new BookingResponse
            {
                BookingId = booking.BookingId,
                Status = booking.Status,
                BookingDate = booking.BookingDate,
                Trip = new TripInfo
                {
                    Departure = trip.Route.Departure,
                    Destination = trip.Route.Destination,
                    DepartureTime = trip.DepartureTime
                },
                Seat = new SeatInfo
                {
                    SeatNumber = seat.SeatNumber,
                    SeatType = seat.SeatType
                },
                Payment = new PaymentInfo
                {
                    Amount = payment.Amount,
                    PaymentMethod = payment.PaymentMethod,
                    PaymentStatus = payment.PaymentStatus
                },
                TicketQrCode = qrCode
            };
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<List<BookingResponse>> GetBookingsByCustomerAsync(int customerId)
    {
        var bookings = await _context.Bookings
            .Include(b => b.Trip).ThenInclude(t => t.Route)
            .Include(b => b.Seat)
            .Include(b => b.Payment)
            .Include(b => b.Tickets)
            .Where(b => b.CustomerId == customerId)
            .OrderByDescending(b => b.BookingDate)
            .ToListAsync();

        return bookings.Select(b => new BookingResponse
        {
            BookingId = b.BookingId,
            Status = b.Status,
            BookingDate = b.BookingDate,
            Trip = new TripInfo
            {
                Departure = b.Trip.Route.Departure,
                Destination = b.Trip.Route.Destination,
                DepartureTime = b.Trip.DepartureTime
            },
            Seat = new SeatInfo
            {
                SeatNumber = b.Seat?.SeatNumber ?? "",
                SeatType = b.Seat?.SeatType ?? ""
            },
            Payment = new PaymentInfo
            {
                Amount = b.Payment?.Amount ?? 0,
                PaymentMethod = b.Payment?.PaymentMethod ?? "",
                PaymentStatus = b.Payment?.PaymentStatus ?? ""
            },
            TicketQrCode = b.Tickets.FirstOrDefault()?.QrCode ?? ""
        }).ToList();
    }

    public async Task<bool> CancelBookingAsync(int bookingId, int customerId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var booking = await _context.Bookings
                .Include(b => b.Tickets)
                .FirstOrDefaultAsync(b => b.BookingId == bookingId && b.CustomerId == customerId);

            if (booking == null) return false;
            if (booking.Status == "Cancelled") return false;

            // Cho phép hủy trước giờ khởi hành (ví dụ: 2 giờ)
            var trip = await _context.Trips.FindAsync(booking.TripId);
            if (trip != null && trip.DepartureTime <= DateTime.Now.AddHours(2))
                throw new Exception("Không thể hủy vé trong vòng 2 giờ trước khởi hành!");

            // Hoàn ghế
            var seat = await _context.Seats.FindAsync(booking.SeatId);
            if (seat != null) seat.IsBooked = false;

            // Cập nhật trạng thái
            booking.Status = "Cancelled";
            foreach (var ticket in booking.Tickets)
                ticket.TicketStatus = "Cancelled";

            // Cập nhật payment (refund logic)
            var payment = await _context.Payments.FirstOrDefaultAsync(p => p.BookingId == bookingId);
            if (payment != null) payment.PaymentStatus = "Refunded";

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static string GenerateQrCode(int bookingId, int tripId, int seatId)
    {
        // Trong thực tế nên dùng thư viện QRCoder để tạo ảnh QR
        return $"BUS-{bookingId:D6}-TRIP{tripId}-SEAT{seatId}-{DateTime.Now:yyyyMMddHHmmss}";
    }
}