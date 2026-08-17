using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TechnoVIS.Data;
using TechnoVIS.Models;
using TechnoVIS.Services;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Collections.Generic;

namespace TechnoVIS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VisitesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ScoringService _scoringService;
        private readonly ILogger<VisitesController> _logger;

        public VisitesController(AppDbContext context, ScoringService scoringService, ILogger<VisitesController> logger)
        {
            _context = context;
            _scoringService = scoringService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetVisites([FromQuery] string? statut, [FromQuery] int? technicienId, [FromQuery] string? technicien)
        {
            var query = _context.Visites
                .Include(v => v.Equipement)
                .ThenInclude(e => e!.Site)
                .ThenInclude(s => s!.Client)
                .Include(v => v.Technicien)
                .Include(v => v.Marche)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(statut))
            {
                query = query.Where(v => v.Statut == statut);
            }
            if (technicienId.HasValue)
            {
                query = query.Where(v => v.TechnicienId == technicienId.Value);
            }
            if (!string.IsNullOrWhiteSpace(technicien))
            {
                query = query.Where(v => v.Technicien != null &&
                    (v.Technicien.Nom.Contains(technicien) || v.Technicien.Prenom.Contains(technicien)));
            }

            var result = await query
                .OrderByDescending(v => v.ScorePriorite)
                .Select(v => new
                {
                    v.Id,
                    v.Reference,
                    v.TypeVisite,
                    v.TypeVisiteAutre,
                    v.Description,
                    TypeVisiteAffiche = v.TypeVisite == "Autre" && !string.IsNullOrWhiteSpace(v.TypeVisiteAutre) ? $"Autre ({v.TypeVisiteAutre})" : v.TypeVisite,
                    v.EquipementId,
                    EquipementNom = v.Equipement != null ? v.Equipement.Nom : "Inconnu",
                    EquipementSerial = v.Equipement != null ? v.Equipement.SerialNumber : "",
                    EquipementCategorie = v.Equipement != null ? v.Equipement.Categorie : "",
                    SiteNom = v.Equipement != null && v.Equipement.Site != null ? v.Equipement.Site.NomSite : "",
                    ClientNom = v.Equipement != null && v.Equipement.Site != null && v.Equipement.Site.Client != null ? v.Equipement.Site.Client.NomSociete : "",
                    v.TechnicienId,
                    TechnicienNom = v.Technicien != null ? $"{v.Technicien.Prenom} {v.Technicien.Nom}".Trim() : "Non assigné",
                    TechnicienMatricule = v.Technicien != null ? v.Technicien.Matricule : "",
                    v.MarcheId,
                    MarcheCode = v.Marche != null ? v.Marche.CodeMarche : "",
                    v.DatePrevue,
                    v.DateRealisee,
                    v.DureeEstimeeMinutes,
                    v.DureeReelleMinutes,
                    v.Statut,
                    v.ScorePriorite,
                    v.RapportTechnique,
                    v.ActionsCorrectives
                })
                .ToListAsync();

            return Ok(result);
        }

        [HttpGet("mes-visites")]
        public async Task<IActionResult> GetMesVisites([FromQuery] int? technicienId)
        {
            var techIdClaim = User.FindFirst("technicienId")?.Value;
            var emailClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

            int? targetTechId = technicienId;
            if (!targetTechId.HasValue && int.TryParse(techIdClaim, out var parsedId) && parsedId > 0)
            {
                targetTechId = parsedId;
            }
            else if (!targetTechId.HasValue && !string.IsNullOrWhiteSpace(emailClaim))
            {
                var tech = await _context.Techniciens.FirstOrDefaultAsync(t => t.Email == emailClaim);
                targetTechId = tech?.Id;
            }

            var query = _context.Visites
                .Include(v => v.Equipement)
                .ThenInclude(e => e!.Site)
                .ThenInclude(s => s!.Client)
                .Include(v => v.Technicien)
                .Include(v => v.Marche)
                .AsQueryable();

            if (targetTechId.HasValue)
            {
                query = query.Where(v => v.TechnicienId == targetTechId.Value);
            }

            var result = await query
                .OrderBy(v => v.DatePrevue)
                .Select(v => new
                {
                    v.Id,
                    v.Reference,
                    v.TypeVisite,
                    v.TypeVisiteAutre,
                    v.Description,
                    TypeVisiteAffiche = v.TypeVisite == "Autre" && !string.IsNullOrWhiteSpace(v.TypeVisiteAutre) ? $"Autre ({v.TypeVisiteAutre})" : v.TypeVisite,
                    v.EquipementId,
                    EquipementNom = v.Equipement != null ? v.Equipement.Nom : "Inconnu",
                    EquipementSerial = v.Equipement != null ? v.Equipement.SerialNumber : "",
                    SiteNom = v.Equipement != null && v.Equipement.Site != null ? v.Equipement.Site.NomSite : "",
                    ClientNom = v.Equipement != null && v.Equipement.Site != null && v.Equipement.Site.Client != null ? v.Equipement.Site.Client.NomSociete : "",
                    v.TechnicienId,
                    TechnicienNom = v.Technicien != null ? $"{v.Technicien.Prenom} {v.Technicien.Nom}".Trim() : "Non assigné",
                    v.MarcheId,
                    MarcheCode = v.Marche != null ? v.Marche.CodeMarche : "",
                    v.DatePrevue,
                    v.DateRealisee,
                    v.DureeEstimeeMinutes,
                    v.DureeReelleMinutes,
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
                .ThenInclude(s => s!.Client)
                .Include(v => v.Technicien)
                .Include(v => v.Marche)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (visite == null) return NotFound(new { message = "Visite non trouvée." });
            return Ok(visite);
        }

        [HttpPost]
        public async Task<IActionResult> CreateVisite([FromBody] Visite model)
        {
            if (model == null) return BadRequest(new { message = "Données invalides." });

            // Validation règle métier : si TypeVisite == "Autre", TypeVisiteAutre est obligatoire
            if (string.Equals(model.TypeVisite, "Autre", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(model.TypeVisiteAutre))
                {
                    return BadRequest(new { message = "Le champ 'Précisez le type de visite' est obligatoire lorsque le type 'Autre' est sélectionné." });
                }
            }

            var equipement = await _context.Equipements.Include(e => e.Site).FirstOrDefaultAsync(e => e.Id == model.EquipementId);
            if (equipement != null)
            {
                model.ScorePriorite = _scoringService.CalculerPrioriteVisite(equipement, model.TypeVisite, model.DatePrevue);
            }

            if (string.IsNullOrWhiteSpace(model.Reference))
            {
                var year = DateTime.Now.Year;
                var prefix = $"VIS-{year}-";
                var maxNum = await _context.Visites
                    .Where(v => v.Reference.StartsWith(prefix))
                    .CountAsync();

                model.Reference = $"{prefix}{(maxNum + 1):D4}";
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
            if (update.DureeReelleMinutes.HasValue && update.DureeReelleMinutes.Value > 0)
            {
                visite.DureeReelleMinutes = update.DureeReelleMinutes.Value;
            }

            if (update.Statut == "Validée")
            {
                var dateRealisee = DateTime.Now;
                visite.DateRealisee = dateRealisee;

                var equipement = await _context.Equipements
                    .Include(e => e.Site)
                    .FirstOrDefaultAsync(e => e.Id == visite.EquipementId);

                if (equipement != null && equipement.Site != null)
                {
                    equipement.DerniereVisite = dateRealisee;

                    var activeMarche = await _context.Marches
                        .Where(m => m.ClientId == equipement.Site.ClientId && m.Statut == "Actif")
                        .OrderByDescending(m => m.DateFin)
                        .FirstOrDefaultAsync();

                    if (activeMarche != null && activeMarche.VisitesAnnuellesPrevues > 0)
                    {
                        int intervalleJours = 365 / activeMarche.VisitesAnnuellesPrevues;
                        equipement.ProchaineVisitePrevue = dateRealisee.AddDays(intervalleJours);
                        activeMarche.VisitesRealisees += 1;
                        _logger.LogInformation("Prochaine visite pour l'équipement {EquipementId} recalculée au {ProchaineDate}.",
                            equipement.Id, equipement.ProchaineVisitePrevue);
                    }
                }
            }

            await _context.SaveChangesAsync();
            return Ok(visite);
        }

        /// <summary>
        /// Moteur de recommandation dynamique de techniciens pour un équipement donné :
        /// Retourne la liste classée par score décroissant avec le podium 🥇🥈🥉 et le détail 40/30/20/10.
        /// </summary>
        [HttpGet("recommandations-techniciens")]
        public async Task<IActionResult> GetRecommandationsTechniciens(
            [FromQuery] int equipementId, 
            [FromQuery] DateTime? datePrevue = null, 
            [FromQuery] int dureeMinutes = 120)
        {
            var equipement = await _context.Equipements
                .Include(e => e.Site)
                .FirstOrDefaultAsync(e => e.Id == equipementId);

            if (equipement == null)
            {
                return NotFound(new { message = "Équipement non trouvé." });
            }

            var targetDate = datePrevue ?? DateTime.Now;

            // Load all technicians with their specialties and visits to compute real-time capacity
            var techniciens = await _context.Techniciens
                .Include(t => t.Specialites)
                .Include(t => t.Visites)
                .ToListAsync();

            // Calculate weekly load
            var startOfWeek = targetDate.Date.AddDays(-(int)targetDate.DayOfWeek + (int)DayOfWeek.Monday);
            var endOfWeek = startOfWeek.AddDays(7);

            var scoredList = techniciens.Select(t =>
            {
                // Dynamic planned load calculation for that week
                var visitesSemaine = t.Visites.Where(v => v.DatePrevue >= startOfWeek && v.DatePrevue < endOfWeek && (v.Statut == "Planifiée" || v.Statut == "En cours")).ToList();
                t.HeuresPlanifiees = (int)Math.Ceiling(visitesSemaine.Sum(v => v.DureeEstimeeMinutes) / 60.0);

                var evaluation = _scoringService.EvaluerTechnicien(t, equipement, targetDate, dureeMinutes);

                int heuresRestantes = Math.Max(0, t.HeuresHebdo - t.HeuresPlanifiees);

                return new
                {
                    TechnicienId = t.Id,
                    t.Matricule,
                    NomComplet = $"{t.Prenom} {t.Nom}".Trim(),
                    t.Base,
                    t.Disponible,
                    t.Statut,
                    t.HeuresHebdo,
                    t.HeuresPlanifiees,
                    HeuresRestantes = heuresRestantes,
                    Specialites = t.Specialites.Select(s => s.Nom).ToList(),
                    Score = evaluation.ScoreTotal,
                    evaluation.ScoreCompetence,
                    evaluation.ScoreDisponibilite,
                    evaluation.ScoreCharge,
                    evaluation.ScoreProximite,
                    evaluation.DetailsCompetence,
                    evaluation.DetailsDisponibilite,
                    evaluation.DetailsCharge,
                    evaluation.DetailsProximite
                };
            })
            .OrderByDescending(s => s.Score)
            .ThenByDescending(s => s.HeuresRestantes)
            .ToList();

            return Ok(scoredList);
        }

        // ── EXPORTS ─────────────────────────────────────────────────────────

        [HttpGet("export")]
        public async Task<IActionResult> ExportVisites([FromQuery] string? statut, [FromQuery] string format = "excel",
            [FromServices] PdfExportService? pdfService = null, [FromServices] CsvExportService? csvService = null)
        {
            var query = _context.Visites
                .Include(v => v.Equipement)
                .ThenInclude(e => e!.Site)
                .ThenInclude(s => s!.Client)
                .Include(v => v.Technicien)
                .AsQueryable();

            if (!string.IsNullOrEmpty(statut))
            {
                query = query.Where(v => v.Statut == statut);
            }

            var visites = await query.OrderBy(v => v.DatePrevue).ToListAsync();

            var headers = new string[] { "Référence", "Type", "Équipement", "Client / Site", "Technicien", "Date Prévue", "Statut" };
            var data = visites.Select(v => new string[]
            {
                v.Reference,
                v.TypeVisite == "Autre" && !string.IsNullOrWhiteSpace(v.TypeVisiteAutre) ? $"Autre ({v.TypeVisiteAutre})" : v.TypeVisite,
                v.Equipement?.Nom ?? "",
                $"{v.Equipement?.Site?.Client?.NomSociete} / {v.Equipement?.Site?.NomSite}",
                v.Technicien != null ? $"{v.Technicien.Prenom} {v.Technicien.Nom}" : "Non assigné",
                v.DatePrevue.ToString("dd/MM/yyyy"),
                v.Statut
            }).ToArray();

            if (format == "pdf")
            {
                if (pdfService == null) return StatusCode(500, "PDF service unavailable");
                var pdfBytes = pdfService.GenerateTablePdf("Planning des Visites", headers, data);
                return File(pdfBytes, "application/pdf", $"Visites_{DateTime.Now:yyyyMMdd}.pdf");
            }
            else if (format == "csv")
            {
                if (csvService == null) return StatusCode(500, "CSV service unavailable");
                var csvBytes = csvService.GenerateCsv(headers, data);
                return File(csvBytes, "text/csv; charset=utf-8", $"Visites_{DateTime.Now:yyyyMMdd}.csv");
            }
            else // default: excel
            {
                using var workbook = new ClosedXML.Excel.XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Planification");
                for (int c = 0; c < headers.Length; c++)
                    worksheet.Cell(1, c + 1).Value = headers[c];
                var headerRange = worksheet.Range(1, 1, 1, headers.Length);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
                for (int i = 0; i < data.Length; i++)
                    for (int c = 0; c < data[i].Length; c++)
                        worksheet.Cell(i + 2, c + 1).Value = data[i][c];
                worksheet.Columns().AdjustToContents();
                using var stream = new System.IO.MemoryStream();
                workbook.SaveAs(stream);
                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Visites_{DateTime.Now:yyyyMMdd}.xlsx");
            }
        }

        [HttpGet("{id}/pv-pdf")]
        public async Task<IActionResult> ExportPvPdf(int id, [FromServices] PdfExportService pdfService)
        {
            var visite = await _context.Visites
                .Include(v => v.Equipement)
                .ThenInclude(e => e!.Site)
                .ThenInclude(s => s!.Client)
                .Include(v => v.Technicien)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (visite == null) return NotFound(new { message = "Visite introuvable." });
            if (visite.Statut != "Validée") return BadRequest(new { message = "Le PV ne peut être généré que pour une visite validée." });

            var pdfBytes = pdfService.GeneratePvPdf(visite);
            return File(pdfBytes, "application/pdf", $"PV_{visite.Reference}.pdf");
        }
    }

    public class StatutUpdateRequest
    {
        public string Statut { get; set; } = string.Empty;
        public string? RapportTechnique { get; set; }
        public string? ActionsCorrectives { get; set; }
        public int? DureeReelleMinutes { get; set; }
    }
}
