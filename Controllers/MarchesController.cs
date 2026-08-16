using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechnoVIS.Data;
using TechnoVIS.Models;
using TechnoVIS.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace TechnoVIS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MarchesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ExcelImportService _excelService;

        public MarchesController(AppDbContext context, ExcelImportService excelService)
        {
            _context = context;
            _excelService = excelService;
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
                    m.Statut,
                    m.TypeContrat,
                    m.PvRequis,
                    m.FactureRequise
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

        // ── Excel Import ────────────────────────────────────────────────────

        /// <summary>
        /// POST /api/marches/import/preview
        /// Parses an .xlsx file and returns { rowCount, preview } without writing to DB.
        /// </summary>
        [HttpPost("import/preview")]
        [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB max
        public IActionResult ImportPreview(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "Aucun fichier fourni." });

            if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { error = "Le fichier doit être au format .xlsx." });

            try
            {
                using var stream = file.OpenReadStream();
                var rows = _excelService.ParseExcel(stream);

                return Ok(new
                {
                    rowCount = rows.Count,
                    preview = rows.Take(5).Select(r => new
                    {
                        r.RowIndex,
                        r.Reference,
                        r.ClientNom,
                        DateDebut = r.DateDebut.ToString("dd/MM/yyyy"),
                        DateFin = r.DateFin.ToString("dd/MM/yyyy"),
                        r.TypeContrat,
                        r.VisitesAnnuellesPrevues,
                        r.VisitesRealisees,
                        r.Sites,
                        r.PvRequis,
                        r.FactureRequise,
                        r.NombrePC,
                        r.NombrePCPortable,
                        r.NombreImprimante,
                        r.NombreServeur,
                        r.EquipementsDivers,
                        r.CommentaireImport,
                        r.ParseWarning
                    }),
                    // Return all rows serialized so the frontend can post them back for confirm
                    allRows = rows
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = $"Erreur lors de la lecture du fichier : {ex.Message}" });
            }
        }

        /// <summary>
        /// POST /api/marches/import/confirm
        /// Receives the parsed rows from preview and writes them to DB.
        /// </summary>
        [HttpPost("import/confirm")]
        public async Task<IActionResult> ImportConfirm([FromBody] List<MarcheImportRow> rows)
        {
            if (rows == null || rows.Count == 0)
                return BadRequest(new { error = "Aucune ligne à importer." });

            int imported = 0;
            int updated = 0;
            int skipped = 0;
            var errors = new List<string>();

            // Pre-load all existing clients, sites, and marches
            var existingClients = await _context.Clients.Include(c => c.Sites).ToListAsync();
            var existingMarches = await _context.Marches.ToListAsync();

            foreach (var row in rows)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(row.ClientNom) && string.IsNullOrWhiteSpace(row.Reference))
                    {
                        skipped++;
                        continue;
                    }

                    // ── 1. Resolve or create Client ────────────────────────
                    var clientNom = string.IsNullOrWhiteSpace(row.ClientNom) ? "Client Inconnu" : row.ClientNom.Trim();
                    var client = existingClients
                        .FirstOrDefault(c => string.Equals(c.NomSociete, clientNom, StringComparison.OrdinalIgnoreCase));

                    if (client == null)
                    {
                        var clientCodePrefix = clientNom.Length >= 3 
                            ? clientNom[..3].ToUpper() 
                            : "CLI";

                        client = new Client
                        {
                            NomSociete = clientNom,
                            CodeClient = $"CL-{clientCodePrefix}-{new Random().Next(100, 999)}",
                            ContactPrincipal = string.Empty,
                            Email = string.Empty,
                            Telephone = string.Empty,
                            Adresse = string.Empty
                        };
                        _context.Clients.Add(client);
                        await _context.SaveChangesAsync(); // get new Id
                        existingClients.Add(client);
                    }

                    // ── 2. Calculate Statut from DateFin vs today ──────────
                    var statut = row.DateFin.Date >= DateTime.Today ? "Actif" : "Expiré";
                    var refCode = string.IsNullOrWhiteSpace(row.Reference)
                        ? $"MAR-{DateTime.Now.Year}-{new Random().Next(100, 999)}"
                        : row.Reference.Trim();

                    // ── 3. Resolve or update Marche ────────────────────────
                    var existingMarche = existingMarches
                        .FirstOrDefault(m => string.Equals(m.CodeMarche, refCode, StringComparison.OrdinalIgnoreCase));

                    Marche marche;
                    if (existingMarche != null)
                    {
                        // Update existing
                        marche = existingMarche;
                        marche.ClientId = client.Id;
                        marche.Libelle = refCode;
                        marche.DateDebut = row.DateDebut;
                        marche.DateFin = row.DateFin;
                        marche.TypeContrat = row.TypeContrat;
                        marche.VisitesAnnuellesPrevues = row.VisitesAnnuellesPrevues;
                        marche.VisitesRealisees = row.VisitesRealisees;
                        marche.Statut = statut;
                        marche.PvRequis = row.PvRequis;
                        marche.FactureRequise = row.FactureRequise;
                        marche.NombrePC = row.NombrePC;
                        marche.NombrePCPortable = row.NombrePCPortable;
                        marche.NombreImprimante = row.NombreImprimante;
                        marche.NombreServeur = row.NombreServeur;
                        marche.EquipementsDivers = row.EquipementsDivers;
                        marche.CommentaireImport = row.CommentaireImport;
                        updated++;
                    }
                    else
                    {
                        marche = new Marche
                        {
                            CodeMarche = refCode,
                            Libelle = refCode,
                            ClientId = client.Id,
                            DateDebut = row.DateDebut,
                            DateFin = row.DateFin,
                            TypeContrat = row.TypeContrat,
                            VisitesAnnuellesPrevues = row.VisitesAnnuellesPrevues,
                            VisitesRealisees = row.VisitesRealisees,
                            Statut = statut,
                            PvRequis = row.PvRequis,
                            FactureRequise = row.FactureRequise,
                            NombrePC = row.NombrePC,
                            NombrePCPortable = row.NombrePCPortable,
                            NombreImprimante = row.NombreImprimante,
                            NombreServeur = row.NombreServeur,
                            EquipementsDivers = row.EquipementsDivers,
                            CommentaireImport = row.CommentaireImport,
                            SlaHeures = 24
                        };
                        _context.Marches.Add(marche);
                        existingMarches.Add(marche);
                        imported++;
                    }

                    // ── 4. Create Site record if Sites is specified ────────
                    Site? primarySite = null;
                    if (!string.IsNullOrWhiteSpace(row.Sites))
                    {
                        var cities = row.Sites.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var city in cities)
                        {
                            var normalized = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(city.Trim().ToLower());
                            var existingSite = await _context.Sites
                                .FirstOrDefaultAsync(s => s.ClientId == client.Id && s.Ville == normalized);
                            if (existingSite == null)
                            {
                                var siteCode = normalized.Length >= 3 ? normalized[..3].ToUpper() : "ST";
                                existingSite = new Site
                                {
                                    NomSite = $"Site {normalized}",
                                    CodeSite = $"ST-{siteCode}-{new Random().Next(10, 99)}",
                                    ClientId = client.Id,
                                    Ville = normalized,
                                    Adresse = string.Empty,
                                    CodePostal = string.Empty
                                };
                                _context.Sites.Add(existingSite);
                                await _context.SaveChangesAsync();
                            }
                            primarySite ??= existingSite;
                        }
                    }

                    // ── 5. Generate Equipment entries if specified ─────────
                    if (primarySite != null)
                    {
                        void AddEquipIfAbsent(string nom, string categorie, int count)
                        {
                            if (count <= 0) return;
                            var serial = $"EQ-{categorie[..Math.Min(3, categorie.Length)].ToUpper()}-{client.Id}-{primarySite.Id}";
                            var exists = _context.Equipements.Any(e => e.SiteId == primarySite.Id && e.SerialNumber == serial);
                            if (!exists)
                            {
                                _context.Equipements.Add(new Equipement
                                {
                                    SerialNumber = serial,
                                    Nom = $"{nom} ({count} unités)",
                                    Categorie = categorie,
                                    SiteId = primarySite.Id,
                                    DateInstallation = row.DateDebut,
                                    Criticiticite = 3,
                                    ScoreSante = 85,
                                    ScoreRisque = 25,
                                    Statut = "Opérationnel",
                                    DerniereVisite = row.DateDebut,
                                    ProchaineVisitePrevue = row.DateDebut.AddMonths(3)
                                });
                            }
                        }

                        AddEquipIfAbsent("Parc PC Fixes", "Poste Fixe", row.NombrePC);
                        AddEquipIfAbsent("Parc PC Portables", "Portable", row.NombrePCPortable);
                        AddEquipIfAbsent("Parc Imprimantes", "Imprimante", row.NombreImprimante);
                        AddEquipIfAbsent("Parc Serveurs", "Serveur", row.NombreServeur);
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Ligne {row.RowIndex} ({row.Reference}): {ex.Message}");
                    skipped++;
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { imported, updated, skipped, errors });
        }

        [HttpGet("export")]
        public async Task<IActionResult> ExportMarches([FromQuery] string format = "excel",
            [FromServices] PdfExportService? pdfService = null, [FromServices] CsvExportService? csvService = null)
        {
            var marches = await _context.Marches
                .Include(m => m.Client)
                .OrderBy(m => m.DateDebut)
                .ToListAsync();

            // Shared data
            var headers = new string[] {
                "Référence", "Client", "Date début", "Date fin", "Type de contrat",
                "Nb visite / An", "Nb visite réalisé", "PV", "Facture", "Statut", "Commentaire"
            };
            var data = marches.Select(m => new string[]
            {
                m.Libelle,
                m.Client?.NomSociete ?? "",
                m.DateDebut.ToString("dd/MM/yyyy"),
                m.DateFin.ToString("dd/MM/yyyy"),
                m.TypeContrat ?? "",
                m.VisitesAnnuellesPrevues.ToString(),
                m.VisitesRealisees.ToString(),
                m.PvRequis ? "OUI" : "NON",
                m.FactureRequise ? "OUI" : "NON",
                m.Statut ?? "",
                m.CommentaireImport ?? ""
            }).ToArray();

            if (format == "pdf")
            {
                if (pdfService == null) return StatusCode(500, "PDF service unavailable");
                var pdfBytes = pdfService.GenerateTablePdf("Marchés & Clients", headers, data);
                return File(pdfBytes, "application/pdf", $"Marches_{DateTime.Now:yyyyMMdd}.pdf");
            }
            else if (format == "csv")
            {
                if (csvService == null) return StatusCode(500, "CSV service unavailable");
                var csvBytes = csvService.GenerateCsv(headers, data);
                return File(csvBytes, "text/csv; charset=utf-8", $"Marches_{DateTime.Now:yyyyMMdd}.csv");
            }
            else // default: excel
            {
                using var workbook = new ClosedXML.Excel.XLWorkbook();
                var worksheet = workbook.Worksheets.Add("Marchés");
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
                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Marches_{DateTime.Now:yyyyMMdd}.xlsx");
            }
        }
    }
}
