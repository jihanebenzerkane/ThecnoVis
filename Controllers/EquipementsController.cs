using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechnoVIS.Data;
using TechnoVIS.Models;
using TechnoVIS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TechnoVIS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EquipementsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ScoringService _scoringService;
        private readonly ExcelImportService _excelService;

        public EquipementsController(AppDbContext context, ScoringService scoringService, ExcelImportService excelService)
        {
            _context = context;
            _scoringService = scoringService;
            _excelService = excelService;
        }

        [HttpGet]
        public async Task<IActionResult> GetEquipements([FromQuery] string? search, [FromQuery] string? categorie, [FromQuery] int? minRisque, [FromQuery] string? statut)
        {
            var query = _context.Equipements
                .Include(e => e.Site)
                .ThenInclude(s => s!.Client)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(e => e.Nom.ToLower().Contains(s) || 
                                         e.SerialNumber.ToLower().Contains(s) ||
                                         (e.Site != null && e.Site.NomSite.ToLower().Contains(s)) ||
                                         (e.Site != null && e.Site.Client != null && e.Site.Client.NomSociete.ToLower().Contains(s)));
            }

            if (!string.IsNullOrWhiteSpace(categorie))
            {
                query = query.Where(e => e.Categorie == categorie);
            }
            if (!string.IsNullOrWhiteSpace(statut))
            {
                query = query.Where(e => e.Statut == statut);
            }
            if (minRisque.HasValue)
            {
                query = query.Where(e => e.ScoreRisque >= minRisque.Value);
            }

            var list = await query.ToListAsync();

            var result = list.Select(e =>
            {
                // Dynamic recalculation of risk score
                var dynamicScoreRisque = _scoringService.CalculerScoreRisque(e);
                return new
                {
                    e.Id,
                    e.SerialNumber,
                    e.Nom,
                    e.Categorie,
                    e.SiteId,
                    SiteNom = e.Site != null ? e.Site.NomSite : "N/A",
                    SiteVille = e.Site != null ? e.Site.Ville : "N/A",
                    ClientId = e.Site?.ClientId,
                    ClientNom = e.Site?.Client?.NomSociete ?? "N/A",
                    e.DateInstallation,
                    e.Criticiticite,
                    e.ScoreSante,
                    ScoreRisque = dynamicScoreRisque,
                    e.Statut,
                    e.DerniereVisite,
                    e.ProchaineVisitePrevue
                };
            }).ToList();

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _context.Equipements
                .Include(e => e.Site)
                .ThenInclude(s => s!.Client)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (item == null) return NotFound(new { message = "Équipement non trouvé." });

            item.ScoreRisque = _scoringService.CalculerScoreRisque(item);
            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Equipement model)
        {
            if (model == null) return BadRequest(new { message = "Données d'équipement invalides." });

            if (string.IsNullOrWhiteSpace(model.SerialNumber))
            {
                var count = await _context.Equipements.CountAsync();
                var cat = string.IsNullOrWhiteSpace(model.Categorie) ? "EQ" : new string(model.Categorie.Where(char.IsLetterOrDigit).Take(3).ToArray()).ToUpper();
                model.SerialNumber = $"EQ-{cat}-{(count + 1):D4}";
            }

            // Verify unique serial number
            var exists = await _context.Equipements.AnyAsync(e => e.SerialNumber == model.SerialNumber);
            if (exists)
            {
                return BadRequest(new { message = $"Un équipement avec le numéro de série '{model.SerialNumber}' existe déjà." });
            }

            model.ScoreRisque = _scoringService.CalculerScoreRisque(model);
            if (model.DateInstallation == default) model.DateInstallation = DateTime.UtcNow;
            if (model.DerniereVisite == default) model.DerniereVisite = DateTime.UtcNow;
            if (model.ProchaineVisitePrevue == default) model.ProchaineVisitePrevue = DateTime.UtcNow.AddMonths(3);

            _context.Equipements.Add(model);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = model.Id }, model);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Equipement updated)
        {
            var item = await _context.Equipements.FindAsync(id);
            if (item == null) return NotFound(new { message = "Équipement non trouvé." });

            item.Nom = updated.Nom;
            item.Categorie = updated.Categorie;
            item.SiteId = updated.SiteId;
            item.Criticiticite = updated.Criticiticite;
            item.ScoreSante = updated.ScoreSante;
            item.Statut = updated.Statut;
            item.DateInstallation = updated.DateInstallation;
            item.ScoreRisque = _scoringService.CalculerScoreRisque(item);

            await _context.SaveChangesAsync();
            return Ok(item);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.Equipements.FindAsync(id);
            if (item == null) return NotFound(new { message = "Équipement non trouvé." });

            _context.Equipements.Remove(item);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ── EXCEL IMPORT ÉQUIPEMENTS ────────────────────────────────────────

        [HttpPost("import/preview")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> ImportPreview(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "Aucun fichier fourni." });

            if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { error = "Le fichier doit être au format .xlsx." });

            try
            {
                using var stream = file.OpenReadStream();
                var rows = _excelService.ParseEquipementsExcel(stream);

                // Load all existing clients, sites and equipments for validation
                var existingClients = await _context.Clients.Include(c => c.Sites).ToListAsync();
                var existingEquipements = await _context.Equipements.Select(e => e.SerialNumber).ToListAsync();
                var serialSet = new HashSet<string>(existingEquipements, StringComparer.OrdinalIgnoreCase);

                foreach (var r in rows)
                {
                    var warnings = new List<string>();
                    if (!string.IsNullOrEmpty(r.ParseWarning)) warnings.Add(r.ParseWarning);

                    // Validate Serial Number duplicate in DB
                    if (!string.IsNullOrWhiteSpace(r.SerialNumber) && serialSet.Contains(r.SerialNumber))
                    {
                        warnings.Add($"N° de série '{r.SerialNumber}' existe déjà (sera mis à jour)");
                    }

                    // Validate Client ↔ Site consistency
                    if (!string.IsNullOrWhiteSpace(r.ClientNom) && !string.IsNullOrWhiteSpace(r.SiteNom))
                    {
                        var client = existingClients.FirstOrDefault(c => string.Equals(c.NomSociete, r.ClientNom.Trim(), StringComparison.OrdinalIgnoreCase));
                        if (client != null)
                        {
                            var siteInClient = client.Sites.FirstOrDefault(s => string.Equals(s.NomSite, r.SiteNom.Trim(), StringComparison.OrdinalIgnoreCase) ||
                                                                                string.Equals(s.Ville, r.SiteNom.Trim(), StringComparison.OrdinalIgnoreCase));
                            if (siteInClient == null)
                            {
                                // Check if this site exists in another client
                                var siteInOtherClient = existingClients.SelectMany(c => c.Sites.Select(s => new { Client = c, Site = s }))
                                    .FirstOrDefault(x => string.Equals(x.Site.NomSite, r.SiteNom.Trim(), StringComparison.OrdinalIgnoreCase));

                                if (siteInOtherClient != null)
                                {
                                    warnings.Add($"Incohérence : le site '{r.SiteNom}' appartient actuellement à '{siteInOtherClient.Client.NomSociete}', pas à '{r.ClientNom}'.");
                                }
                                else
                                {
                                    warnings.Add($"Nouveau site '{r.SiteNom}' à créer pour '{r.ClientNom}'.");
                                }
                            }
                        }
                        else
                        {
                            warnings.Add($"Nouveau client '{r.ClientNom}' et site '{r.SiteNom}' à initialiser.");
                        }
                    }

                    r.ParseWarning = warnings.Count > 0 ? string.Join(" | ", warnings) : null;
                }

                return Ok(new
                {
                    rowCount = rows.Count,
                    preview = rows.Take(5).Select(r => new
                    {
                        r.RowIndex,
                        r.SerialNumber,
                        r.Nom,
                        r.Categorie,
                        r.ClientNom,
                        r.SiteNom,
                        r.Criticite,
                        DateInstallation = r.DateInstallation.ToString("dd/MM/yyyy"),
                        r.Statut,
                        r.ParseWarning
                    }),
                    allRows = rows
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = $"Erreur lors de la lecture du fichier : {ex.Message}" });
            }
        }

        [HttpPost("import/confirm")]
        public async Task<IActionResult> ImportConfirm([FromBody] List<EquipementImportRow> rows)
        {
            if (rows == null || rows.Count == 0)
                return BadRequest(new { error = "Aucune ligne à importer." });

            int imported = 0;
            int updated = 0;
            int skipped = 0;
            var errors = new List<string>();

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var existingClients = await _context.Clients.Include(c => c.Sites).ToListAsync();
                var existingEquipements = await _context.Equipements.ToListAsync();
                var allSites = await _context.Sites.ToListAsync();

                int clientSeq = existingClients.Count;
                int siteSeq = allSites.Count;
                int eqSeq = existingEquipements.Count;

                foreach (var row in rows)
                {
                    if (string.IsNullOrWhiteSpace(row.Nom) && string.IsNullOrWhiteSpace(row.SerialNumber))
                    {
                        skipped++;
                        continue;
                    }

                    // 1. Resolve or create Client
                    var clientNom = string.IsNullOrWhiteSpace(row.ClientNom) ? "Client ECS Standard" : row.ClientNom.Trim();
                    var client = existingClients.FirstOrDefault(c => string.Equals(c.NomSociete, clientNom, StringComparison.OrdinalIgnoreCase));
                    if (client == null)
                    {
                        clientSeq++;
                        var prefix = clientNom.Length >= 3 ? new string(clientNom.Where(char.IsLetterOrDigit).Take(3).ToArray()).ToUpper() : "CLI";
                        client = new Client
                        {
                            NomSociete = clientNom,
                            CodeClient = $"CL-{prefix}-{clientSeq:D4}"
                        };
                        _context.Clients.Add(client);
                        await _context.SaveChangesAsync();
                        existingClients.Add(client);
                    }

                    // 2. Resolve or create Site
                    var siteNom = string.IsNullOrWhiteSpace(row.SiteNom) ? "Site Principal" : row.SiteNom.Trim();
                    var site = client.Sites.FirstOrDefault(s => string.Equals(s.NomSite, siteNom, StringComparison.OrdinalIgnoreCase) ||
                                                                string.Equals(s.Ville, siteNom, StringComparison.OrdinalIgnoreCase));
                    if (site == null)
                    {
                        siteSeq++;
                        site = new Site
                        {
                            ClientId = client.Id,
                            NomSite = siteNom,
                            CodeSite = $"SITE-{siteSeq:D4}",
                            Ville = siteNom,
                            Adresse = siteNom
                        };
                        _context.Sites.Add(site);
                        await _context.SaveChangesAsync();
                        client.Sites.Add(site);
                        allSites.Add(site);
                    }

                    // 3. Serial Number resolution
                    string serial = row.SerialNumber?.Trim() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(serial))
                    {
                        eqSeq++;
                        var catPrefix = !string.IsNullOrWhiteSpace(row.Categorie) 
                            ? new string(row.Categorie.Where(char.IsLetterOrDigit).Take(3).ToArray()).ToUpper() 
                            : "EQ";
                        serial = $"EQ-{catPrefix}-{eqSeq:D4}";
                    }

                    // 4. Create or Update Equipement
                    var existing = existingEquipements.FirstOrDefault(e => string.Equals(e.SerialNumber, serial, StringComparison.OrdinalIgnoreCase));
                    if (existing != null)
                    {
                        existing.Nom = string.IsNullOrWhiteSpace(row.Nom) ? existing.Nom : row.Nom.Trim();
                        existing.Categorie = string.IsNullOrWhiteSpace(row.Categorie) ? existing.Categorie : row.Categorie.Trim();
                        existing.SiteId = site.Id;
                        existing.Criticiticite = row.Criticite > 0 ? row.Criticite : existing.Criticiticite;
                        existing.ScoreSante = row.ScoreSante > 0 ? row.ScoreSante : existing.ScoreSante;
                        existing.Statut = string.IsNullOrWhiteSpace(row.Statut) ? existing.Statut : row.Statut;
                        if (row.DateInstallation != default) existing.DateInstallation = row.DateInstallation;
                        existing.ScoreRisque = _scoringService.CalculerScoreRisque(existing);
                        updated++;
                    }
                    else
                    {
                        var newEq = new Equipement
                        {
                            SerialNumber = serial,
                            Nom = string.IsNullOrWhiteSpace(row.Nom) ? serial : row.Nom.Trim(),
                            Categorie = string.IsNullOrWhiteSpace(row.Categorie) ? "Général" : row.Categorie.Trim(),
                            SiteId = site.Id,
                            Criticiticite = row.Criticite > 0 ? row.Criticite : 3,
                            ScoreSante = row.ScoreSante > 0 ? row.ScoreSante : 85,
                            Statut = string.IsNullOrWhiteSpace(row.Statut) ? "Opérationnel" : row.Statut.Trim(),
                            DateInstallation = row.DateInstallation != default ? row.DateInstallation : DateTime.UtcNow,
                            DerniereVisite = DateTime.UtcNow,
                            ProchaineVisitePrevue = DateTime.UtcNow.AddMonths(3)
                        };
                        newEq.ScoreRisque = _scoringService.CalculerScoreRisque(newEq);
                        _context.Equipements.Add(newEq);
                        existingEquipements.Add(newEq);
                        imported++;
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    imported,
                    updated,
                    skipped,
                    total = imported + updated,
                    errors
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { error = $"Erreur lors de l'enregistrement en base : {ex.Message}" });
            }
        }
    }
}
