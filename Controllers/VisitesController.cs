using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechnoVIS.Data;
using TechnoVIS.Models;
using TechnoVIS.Services;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace TechnoVIS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VisitesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ScoringService _scoringService;

        public VisitesController(AppDbContext context, ScoringService scoringService)
        {
            _context = context;
            _scoringService = scoringService;
        }

        [HttpGet]
        public async Task<IActionResult> GetVisites([FromQuery] string? statut, [FromQuery] string? technicien)
        {
            var query = _context.Visites
                .Include(v => v.Equipement)
                .ThenInclude(e => e!.Site)
                .ThenInclude(s => s!.Client)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(statut))
            {
                query = query.Where(v => v.Statut == statut);
            }
            if (!string.IsNullOrWhiteSpace(technicien))
            {
                query = query.Where(v => v.TechnicienAssigne.Contains(technicien));
            }

            var result = await query
                .OrderByDescending(v => v.ScorePriorite)
                .Select(v => new
                {
                    v.Id,
                    v.Reference,
                    v.TypeVisite,
                    v.EquipementId,
                    EquipementNom = v.Equipement != null ? v.Equipement.Nom : "Inconnu",
                    EquipementSerial = v.Equipement != null ? v.Equipement.SerialNumber : "",
                    SiteNom = v.Equipement != null && v.Equipement.Site != null ? v.Equipement.Site.NomSite : "",
                    ClientNom = v.Equipement != null && v.Equipement.Site != null && v.Equipement.Site.Client != null ? v.Equipement.Site.Client.NomSociete : "",
                    v.TechnicienAssigne,
                    v.DatePrevue,
                    v.DateRealisee,
                    v.DureeEstimeeMinutes,
                    v.Statut,
                    v.ScorePriorite,
                    v.RapportTechnique,
                    v.ActionsCorrectives
                })
                .ToListAsync();

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetVisiteById(int id)
        {
            var visite = await _context.Visites
                .Include(v => v.Equipement)
                .ThenInclude(e => e!.Site)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (visite == null) return NotFound(new { message = "Visite non trouvée." });
            return Ok(visite);
        }

        [HttpPost]
        public async Task<IActionResult> CreateVisite([FromBody] Visite model)
        {
            if (model == null) return BadRequest();

            var equipement = await _context.Equipements.FindAsync(model.EquipementId);
            if (equipement != null)
            {
                model.ScorePriorite = _scoringService.CalculerPrioriteVisite(equipement, model.TypeVisite, model.DatePrevue);
            }

            if (string.IsNullOrWhiteSpace(model.Reference))
            {
                model.Reference = $"VIS-{DateTime.Now.Year}-{new Random().Next(1000, 9999)}";
            }

            _context.Visites.Add(model);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetVisiteById), new { id = model.Id }, model);
        }

        [HttpPut("{id}/statut")]
        public async Task<IActionResult> UpdateStatut(int id, [FromBody] StatutUpdateRequest update)
        {
            var visite = await _context.Visites.FindAsync(id);
            if (visite == null) return NotFound();

            visite.Statut = update.Statut;
            if (!string.IsNullOrWhiteSpace(update.RapportTechnique))
            {
                visite.RapportTechnique = update.RapportTechnique;
            }
            if (!string.IsNullOrWhiteSpace(update.ActionsCorrectives))
            {
                visite.ActionsCorrectives = update.ActionsCorrectives;
            }
            if (update.Statut == "Validée")
            {
                visite.DateRealisee = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return Ok(visite);
        }
    }

    public class StatutUpdateRequest
    {
        public string Statut { get; set; } = string.Empty;
        public string? RapportTechnique { get; set; }
        public string? ActionsCorrectives { get; set; }
    }
}
