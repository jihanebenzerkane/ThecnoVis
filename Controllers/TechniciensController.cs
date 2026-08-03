using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechnoVIS.Data;
using TechnoVIS.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TechnoVIS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TechniciensController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TechniciensController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Techniciens
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Technicien>>> GetTechniciens()
        {
            return await _context.Techniciens.ToListAsync();
        }

        // POST: api/Techniciens
        [HttpPost]
        public async Task<ActionResult<Technicien>> PostTechnicien(Technicien technicien)
        {
            _context.Techniciens.Add(technicien);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTechniciens), new { id = technicien.Id }, technicien);
        }
    }
}
