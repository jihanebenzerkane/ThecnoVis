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
            int skipped = 0;
            var errors = new List<string>();

            // Pre-load all existing clients once
            var existingClients = await _context.Clients.ToListAsync();

            foreach (var row in rows)
            {
                try
                {
                    // ── 1. Resolve or create Client ────────────────────────
                    var client = existingClients
                        .FirstOrDefault(c => string.Equals(c.NomSociete, row.ClientNom, StringComparison.OrdinalIgnoreCase));

                    if (client == null)
                    {
                        client = new Client
                        {
                            NomSociete = row.ClientNom,
                            CodeClient = $"CL-{DateTime.Now.Year}-{new Random().Next(100, 999)}",
                            ContactPrincipal = string.Empty,
                            Email = string.Empty,
                            Telephone = string.Empty,
                            Adresse = string.Empty
                        };
                        _context.Clients.Add(client);
                        await _context.SaveChangesAsync(); // get the new Id
                        existingClients.Add(client);       // cache for next iterations
                    }

                    // ── 2. Calculate Statut from DateFin vs today ──────────
                    var statut = row.DateFin.Date >= DateTime.Today ? "Actif" : "Expiré";

                    // ── 3. Build Marche entity ─────────────────────────────
                    var marche = new Marche
                    {
                        CodeMarche           = string.IsNullOrWhiteSpace(row.Reference)
                                               ? $"MAR-{DateTime.Now.Year}-{new Random().Next(100, 999)}"
                                               : row.Reference,
                        Libelle              = row.Reference, // best available label
                        ClientId             = client.Id,
                        DateDebut            = row.DateDebut,
                        DateFin              = row.DateFin,
                        TypeContrat          = row.TypeContrat,
                        VisitesAnnuellesPrevues = row.VisitesAnnuellesPrevues,
                        VisitesRealisees     = row.VisitesRealisees,
                        Statut               = statut,
                        PvRequis             = row.PvRequis,
                        FactureRequise       = row.FactureRequise,
                        NombrePC             = row.NombrePC,
                        NombrePCPortable     = row.NombrePCPortable,
                        NombreImprimante     = row.NombreImprimante,
                        NombreServeur        = row.NombreServeur,
                        EquipementsDivers    = row.EquipementsDivers,
                        CommentaireImport    = row.CommentaireImport,
                        SlaHeures            = 24  // default — not in Excel source
                    };

                    _context.Marches.Add(marche);

                    // ── 4. Create Site record if Sites is specified ────────
                    if (!string.IsNullOrWhiteSpace(row.Sites))
                    {
                        // Sites may be comma-separated
                        var cities = row.Sites.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var city in cities)
                        {
                            var normalized = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(city.Trim().ToLower());
                            var existingSite = await _context.Sites
                                .FirstOrDefaultAsync(s => s.ClientId == client.Id &&
                                                          s.Ville == normalized);
                            if (existingSite == null)
                            {
                                _context.Sites.Add(new Site
                                {
                                    NomSite  = $"Site {normalized}",
                                    CodeSite = $"ST-{normalized.ToUpper()[..Math.Min(3, normalized.Length)]}-{new Random().Next(10, 99)}",
                                    ClientId = client.Id,
                                    Ville    = normalized,
                                    Adresse  = string.Empty,
                                    CodePostal = string.Empty
                                });
                            }
                        }
                    }

                    imported++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Ligne {row.RowIndex} ({row.Reference}): {ex.Message}");
                    skipped++;
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { imported, skipped, errors });
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
