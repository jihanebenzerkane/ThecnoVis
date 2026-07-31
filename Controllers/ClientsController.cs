using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechnoVIS.Data;
using System.Threading.Tasks;
using System.Linq;

namespace TechnoVIS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ClientsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetClients()
        {
            var clients = await _context.Clients
                .Select(c => new
                {
                    c.Id,
                    c.CodeClient,
                    c.NomSociete,
                    c.ContactPrincipal,
                    c.Email,
                    c.Telephone,
                    c.Adresse,
                    Sites = c.Sites.Select(s => new { s.Id, s.CodeSite, s.NomSite, s.Ville }).ToList()
                })
                .ToListAsync();
            return Ok(clients);
        }

        [HttpGet("/api/marches")]
        public async Task<IActionResult> GetMarches()
        {
            var marches = await _context.Marches
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
    }
}
