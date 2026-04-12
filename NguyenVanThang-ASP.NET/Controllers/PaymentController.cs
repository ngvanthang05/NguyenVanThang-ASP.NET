using Microsoft.AspNetCore.Mvc;
using NguyenVanThang_ASP.NET.Data;
using NguyenVanThang_ASP.NET.Models;
using Microsoft.EntityFrameworkCore;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace NguyenVanThang_ASP.NET.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PaymentsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetPayments()
        {
            return Ok(await _context.Payments.ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> CreatePayment(Payment payment)
        {
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            return Ok(payment);
        }
    }
}
