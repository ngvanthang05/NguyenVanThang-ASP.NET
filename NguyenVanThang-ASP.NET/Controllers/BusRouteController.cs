// Controllers/BusRouteController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NguyenVanThang_ASP.NET.Data;
using NguyenVanThang_ASP.NET.Models;
using Microsoft.EntityFrameworkCore;

namespace NguyenVanThang_ASP.NET.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BusRoutesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BusRoutesController(AppDbContext context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> GetRoutes()
            => Ok(await _context.BusRoutes.ToListAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRoute(int id)
        {
            var route = await _context.BusRoutes
                .Include(r => r.Trips)
                .FirstOrDefaultAsync(r => r.BusRouteId == id);
            return route == null ? NotFound() : Ok(route);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateRoute(BusRoute route)
        {
            _context.BusRoutes.Add(route);
            await _context.SaveChangesAsync();
            return Ok(route);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateRoute(int id, BusRoute route)
        {
            if (id != route.BusRouteId) return BadRequest();
            _context.Entry(route).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return Ok(route);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteRoute(int id)
        {
            var route = await _context.BusRoutes.FindAsync(id);
            if (route == null) return NotFound();
            _context.BusRoutes.Remove(route);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}