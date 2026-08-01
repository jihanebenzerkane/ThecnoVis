using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechnoVIS.Data;
using TechnoVIS.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace TechnoVIS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MarchesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MarchesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetMarches()
        {
            var marches = await _context.Marches
                .Include(m => m.Client)
                .Select(m => new
                {
                    m.Id,
                    m.CodeMarche,
                    m.Libelle,
                    m.ClientId,
                    ClientNom = m.Client != null ? m.Client.NomSociete : "N/A",
                    m.DateDebut,
                    m.DateFin,
                    m.SlaHeures,
                    m.VisitesAnnuellesPrevues,
                    m.VisitesRealisees,
                    m.Statut
                })
                .ToListAsync();
            return Ok(marches);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _context.Marches
                .Include(m => m.Client)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Marche model)
        {
            if (model == null) return BadRequest("Données de marché invalides.");

            if (string.IsNullOrWhiteSpace(model.CodeMarche))
            {
                model.CodeMarche = $"MAR-{DateTime.Now.Year}-{new Random().Next(100, 999)}";
            }
            if (string.IsNullOrWhiteSpace(model.Statut))
            {
                model.Statut = "Actif";
            }

            _context.Marches.Add(model);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = model.Id }, model);
        }
    }
}
