// Controllers/TripsController.cs — hoàn chỉnh
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NguyenVanThang_ASP.NET.Data;
using NguyenVanThang_ASP.NET.Models;
using NguyenVanThang_ASP.NET.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace NguyenVanThang_ASP.NET.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TripsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TripsController(AppDbContext context) => _context = context;

        // ⭐ GET: api/trips/search?departure=HCM&destination=HN&date=2026-04-20
        [HttpGet("search")]
        public async Task<IActionResult> SearchTrips([FromQuery] TripSearchRequest request)
        {
            var date = request.Date.Date;

            var trips = await _context.Trips
                .Include(t => t.Route)
                .Include(t => t.Vehicle).ThenInclude(v => v.Seats)
                .Where(t =>
                    t.Route.Departure.Contains(request.Departure) &&
                    t.Route.Destination.Contains(request.Destination) &&
                    t.DepartureTime.Date == date &&
                    t.Status == "Active")
                .ToListAsync();

            var result = trips.Select(t => new TripSearchResult
            {
                TripId = t.TripId,
                Departure = t.Route.Departure,
                Destination = t.Route.Destination,
                DepartureTime = t.DepartureTime,
                ArrivalTime = t.ArrivalTime,
                Price = t.Price,
                VehicleType = t.Vehicle.VehicleType,
                AvailableSeats = t.Vehicle.Seats.Count(s => !s.IsBooked),
                Status = t.Status
            });

            return Ok(result);
        }

        // GET: api/trips
        [HttpGet]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetTrips()
        {
            var trips = await _context.Trips
                .Include(t => t.Route)
                .Include(t => t.Vehicle)
                .ToListAsync();
            return Ok(trips);
        }

        // GET: api/trips/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTrip(int id)
        {
            var trip = await _context.Trips
                .Include(t => t.Route)
                .Include(t => t.Vehicle).ThenInclude(v => v.Seats)
                .FirstOrDefaultAsync(t => t.TripId == id);

            if (trip == null) return NotFound();
            return Ok(trip);
        }

        // GET: api/trips/5/seats — lấy danh sách ghế và trạng thái
        [HttpGet("{id}/seats")]
        public async Task<IActionResult> GetTripSeats(int id)
        {
            var trip = await _context.Trips
                .Include(t => t.Vehicle).ThenInclude(v => v.Seats)
                .FirstOrDefaultAsync(t => t.TripId == id);

            if (trip == null) return NotFound();

            var seats = trip.Vehicle.Seats.Select(s => new
            {
                s.SeatId,
                s.SeatNumber,
                s.SeatType,
                s.IsBooked
            });

            return Ok(seats);
        }

        // POST: api/trips — chỉ Admin
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateTrip(Trip trip)
        {
            _context.Trips.Add(trip);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetTrip), new { id = trip.TripId }, trip);
        }

        // PUT: api/trips/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateTrip(int id, Trip trip)
        {
            if (id != trip.TripId) return BadRequest();
            _context.Entry(trip).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return Ok(trip);
        }

        // DELETE: api/trips/5 — cancel trip
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CancelTrip(int id)
        {
            var trip = await _context.Trips.FindAsync(id);
            if (trip == null) return NotFound();

            trip.Status = "Cancelled";
            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã hủy chuyến xe" });
        }
    }
}