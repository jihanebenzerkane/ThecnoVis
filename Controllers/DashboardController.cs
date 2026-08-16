using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechnoVIS.Data;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace TechnoVIS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var totalVisites = await _context.Visites.CountAsync();
            var visitesPlanifiees = await _context.Visites.CountAsync(v => v.Statut == "Planifiée");
            var visitesEnRetard = await _context.Visites.CountAsync(v => v.Statut == "En retard");
            var visitesValidees = await _context.Visites.CountAsync(v => v.Statut == "Validée");

            var totalEquipements = await _context.Equipements.CountAsync();
            var equipementsCritiques = await _context.Equipements.CountAsync(e => e.ScoreRisque >= 70);
            var tauxConformite = totalVisites > 0 ? (double)visitesValidees / totalVisites * 100.0 : 100.0;

            var alertesVisites = await _context.Visites
                .Include(v => v.Equipement)
                .ThenInclude(e => e!.Site)
                .Where(v => v.Statut == "En retard" || v.ScorePriorite >= 80)
                .OrderByDescending(v => v.ScorePriorite)
                .Take(5)
                .Select(v => new
                {
                    v.Id,
                    v.Reference,
                    v.TypeVisite,
                    Equipement = v.Equipement != null ? v.Equipement.Nom : "N/A",
                    Site = v.Equipement != null && v.Equipement.Site != null ? v.Equipement.Site.NomSite : "N/A",
                    v.DatePrevue,
                    v.Statut,
                    v.ScorePriorite
                })
                .ToListAsync();

            var totalMarches = await _context.Marches.CountAsync();
            var marchesActifs = await _context.Marches.CountAsync(m => m.Statut == "Actif");
            var totalClients = await _context.Clients.CountAsync();
            var totalTechniciens = await _context.Techniciens.CountAsync();

            return Ok(new
            {
                TotalVisites = totalVisites,
                VisitesPlanifiees = visitesPlanifiees,
                VisitesEnRetard = visitesEnRetard,
                VisitesValidees = visitesValidees,
                TotalEquipements = totalEquipements,
                EquipementsCritiques = equipementsCritiques,
                TauxConformite = Math.Round(tauxConformite, 1),
                TotalMarches = totalMarches,
                MarchesActifs = marchesActifs,
                TotalClients = totalClients,
                TotalTechniciens = totalTechniciens,
                AlertesUrgent = alertesVisites
            });
        }
    }
}
