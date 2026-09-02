using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechnoVIS.Services;

namespace TechnoVIS.Controllers;

[ApiController]
[Route("api/import-global")]
[Authorize(Roles = "Responsable")]
public class SmartImportController : ControllerBase
{
    private readonly IEquipmentImporter _importer;
    private readonly SmartExcelImportService _service;
    private readonly ILogger<SmartImportController> _logger;

    public SmartImportController(
        IEquipmentImporter importer,
        SmartExcelImportService service,
        ILogger<SmartImportController> logger)
    {
        _importer = importer;
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Analyse un ou plusieurs fichiers (Excel, PDF, Image) via la stratégie dynamique
    /// (Azure AI Document Intelligence ou Parseur Algorithmique Local avec Polly Circuit Breaker).
    /// </summary>
    [HttpPost("analyze")]
    [RequestSizeLimit(50 * 1024 * 1024)] // 50MB pour supporter les uploads multi-fichiers
    public async Task<IActionResult> Analyze(List<IFormFile> files)
    {
        if (files == null || files.Count == 0)
            return BadRequest(new { error = "Aucun fichier fourni." });

        try
        {
            var results = new List<ImportResultDto>();

            foreach (var file in files)
            {
                if (file.Length == 0) continue;

                // Execution de la stratégie d'import (Azure AI ou Standard avec auto-guérison Polly)
                var result = await _importer.ProcessFileAsync(file);
                results.Add(result);
            }

            return Ok(new
            {
                success = true,
                totalFiles = results.Count,
                data = results
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur pendant l'analyse globale d'importation.");
            return BadRequest(new
            {
                error = "Impossible d'analyser les fichiers fournis.",
                details = ex.Message
            });
        }
    }

    /// <summary>
    /// Valide et insère définitivement les données vérifiées par l'utilisateur dans SQL Server.
    /// </summary>
    [HttpPost("confirm")]
    public async Task<IActionResult> Confirm(
        [FromBody] SmartExcelImportService.SmartImportConfirmRequest request,
        CancellationToken ct)
    {
        if (request?.Rows == null || request.Rows.Count == 0)
            return BadRequest(new { error = "Aucune ligne à importer." });

        try
        {
            var result = await _service.ImportAsync(request, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur pendant la confirmation du Smart Import dans SQL Server.");
            return StatusCode(500, new
            {
                error = "L'importation a été annulée. Aucune donnée partielle n'a été enregistrée (Rollback strict).",
                details = ex.Message
            });
        }
    }
}