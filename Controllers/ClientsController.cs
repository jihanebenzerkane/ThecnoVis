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

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var client = await _context.Clients
                .Include(c => c.Sites)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (client == null) return NotFound();
            return Ok(client);
        }
    }
}
