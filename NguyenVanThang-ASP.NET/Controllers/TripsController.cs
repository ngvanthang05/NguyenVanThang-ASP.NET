using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NguyenVanThang_ASP.NET.Data;
using NguyenVanThang_ASP.NET.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace NguyenVanThang_ASP.NET.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TripsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TripsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/trips
        [HttpGet]
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
                .Include(t => t.Vehicle)
                .FirstOrDefaultAsync(t => t.TripId == id);

            if (trip == null) return NotFound();

            return Ok(trip);
        }

        // POST
        [HttpPost]
        public async Task<IActionResult> CreateTrip(Trip trip)
        {
            _context.Trips.Add(trip);
            await _context.SaveChangesAsync();

            return Ok(trip);
        }

        // PUT
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTrip(int id, Trip trip)
        {
            if (id != trip.TripId) return BadRequest();

            _context.Entry(trip).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return Ok(trip);
        }

        // DELETE
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTrip(int id)
        {
            var trip = await _context.Trips.FindAsync(id);
            if (trip == null) return NotFound();

            _context.Trips.Remove(trip);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }  
}
