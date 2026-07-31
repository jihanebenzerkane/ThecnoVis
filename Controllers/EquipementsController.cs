using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechnoVIS.Data;
using TechnoVIS.Models;
using TechnoVIS.Services;
using System.Threading.Tasks;
using System.Linq;

namespace TechnoVIS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EquipementsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ScoringService _scoringService;

        public EquipementsController(AppDbContext context, ScoringService scoringService)
        {
            _context = context;
            _scoringService = scoringService;
        }

        [HttpGet]
        public async Task<IActionResult> GetEquipements([FromQuery] string? categorie, [FromQuery] int? minRisque)
        {
            var query = _context.Equipements
                .Include(e => e.Site)
                .ThenInclude(s => s!.Client)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(categorie))
            {
                query = query.Where(e => e.Categorie == categorie);
            }
            if (minRisque.HasValue)
            {
                query = query.Where(e => e.ScoreRisque >= minRisque.Value);
            }

            var result = await query.Select(e => new
            {
                e.Id,
                e.SerialNumber,
                e.Nom,
                e.Categorie,
                SiteNom = e.Site != null ? e.Site.NomSite : "",
                ClientNom = e.Site != null && e.Site.Client != null ? e.Site.Client.NomSociete : "",
                e.DateInstallation,
                e.Criticiticite,
                e.ScoreSante,
                e.ScoreRisque,
                e.Statut,
                e.DerniereVisite,
                e.ProchaineVisitePrevue
            }).ToListAsync();

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _context.Equipements
                .Include(e => e.Site)
                .FirstOrDefaultAsync(e => e.Id == id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Equipement model)
        {
            model.ScoreRisque = _scoringService.CalculerScoreRisque(model);
            _context.Equipements.Add(model);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = model.Id }, model);
        }
    }
}
