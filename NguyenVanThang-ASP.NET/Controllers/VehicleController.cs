// Controllers/VehicleController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NguyenVanThang_ASP.NET.Data;
using NguyenVanThang_ASP.NET.Models;
using Microsoft.EntityFrameworkCore;

namespace NguyenVanThang_ASP.NET.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VehiclesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public VehiclesController(AppDbContext context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> GetVehicles()
            => Ok(await _context.Vehicles.Include(v => v.Seats).ToListAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetVehicle(int id)
        {
            var vehicle = await _context.Vehicles
                .Include(v => v.Seats)
                .FirstOrDefaultAsync(v => v.VehicleId == id);
            return vehicle == null ? NotFound() : Ok(vehicle);
        }

        // POST: api/vehicles — tạo xe và tự động tạo ghế
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateVehicle(Vehicle vehicle)
        {
            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            // Tự động tạo ghế theo SeatCount
            var seats = new List<Seat>();
            for (int i = 1; i <= vehicle.SeatCount; i++)
            {
                seats.Add(new Seat
                {
                    VehicleId = vehicle.VehicleId,
                    SeatNumber = i.ToString("D2"),
                    IsBooked = false,
                    SeatType = i <= vehicle.SeatCount / 2 ? "Thường" : "VIP"
                });
            }
            _context.Seats.AddRange(seats);
            await _context.SaveChangesAsync();

            return Ok(vehicle);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateVehicle(int id, Vehicle vehicle)
        {
            if (id != vehicle.VehicleId) return BadRequest();
            _context.Entry(vehicle).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return Ok(vehicle);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteVehicle(int id)
        {
            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle == null) return NotFound();
            _context.Vehicles.Remove(vehicle);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}