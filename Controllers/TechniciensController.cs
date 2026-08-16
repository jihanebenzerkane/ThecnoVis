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
            return await _context.Techniciens
                .Include(t => t.SiteRattache)
                .ToListAsync();
        }

        // GET: api/Techniciens/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Technicien>> GetTechnicien(int id)
        {
            var technicien = await _context.Techniciens
                .Include(t => t.SiteRattache)
                .Include(t => t.Visites)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (technicien == null)
            {
                return NotFound(new { message = "Technicien non trouvé." });
            }

            return Ok(technicien);
        }

        // POST: api/Techniciens
        [HttpPost]
        public async Task<ActionResult<Technicien>> PostTechnicien([FromBody] Technicien technicien)
        {
            if (technicien == null) return BadRequest();

            _context.Techniciens.Add(technicien);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTechnicien), new { id = technicien.Id }, technicien);
        }

        // PUT: api/Techniciens/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTechnicien(int id, [FromBody] Technicien updated)
        {
            if (id != updated.Id && updated.Id != 0)
            {
                updated.Id = id;
            }

            var technicien = await _context.Techniciens.FindAsync(id);
            if (technicien == null)
            {
                return NotFound(new { message = "Technicien non trouvé." });
            }

            technicien.Nom = updated.Nom;
            technicien.Prenom = updated.Prenom;
            technicien.Email = updated.Email;
            technicien.Telephone = updated.Telephone;
            technicien.Specialites = updated.Specialites;
            technicien.SiteRattacheId = updated.SiteRattacheId;
            technicien.ChargeActuelle = updated.ChargeActuelle;
            technicien.Disponible = updated.Disponible;

            await _context.SaveChangesAsync();
            return Ok(technicien);
        }

        // DELETE: api/Techniciens/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTechnicien(int id)
        {
            var technicien = await _context.Techniciens.FindAsync(id);
            if (technicien == null)
            {
                return NotFound(new { message = "Technicien non trouvé." });
            }

            _context.Techniciens.Remove(technicien);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
