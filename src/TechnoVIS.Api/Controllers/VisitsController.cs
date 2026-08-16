using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TechnoVIS.Api.Contracts;
using TechnoVIS.Api.Data;
using TechnoVIS.Api.Models;
using TechnoVIS.Api.Services;

namespace TechnoVIS.Api.Controllers;

[ApiController]
[Route("api/visits")]
[Authorize(Roles = "Admin,Planner")]
public sealed class VisitsController(AppDbContext db, IAssignmentScoringService scoring, IAssignmentExplanationService explanations) : ControllerBase
{
    [HttpGet("{id:guid}/suggestions")]
    public async Task<ActionResult<IReadOnlyList<AssignmentSuggestionResponse>>> GetSuggestions(Guid id, CancellationToken cancellationToken)
    {
        var visit = await db.Visits
            .Include(v => v.Equipment).ThenInclude(e => e.RequiredSpecialty)
            .Include(v => v.Equipment).ThenInclude(e => e.ClientSite)
            .SingleOrDefaultAsync(v => v.Id == id, cancellationToken);
        if (visit is null) return NotFound();

        var technicians = await db.Technicians.Include(t => t.Specialties).ToListAsync(cancellationToken);
        var weekStart = visit.ScheduledDate.AddDays(-((int)visit.ScheduledDate.DayOfWeek + 6) % 7);
        var weekEnd = weekStart.AddDays(7);
        var workloads = await db.Visits
            .Where(v => v.ScheduledDate >= weekStart && v.ScheduledDate < weekEnd
                && (v.Status == VisitStatus.Planned || v.Status == VisitStatus.InProgress) && v.TechnicianId != null)
            .GroupBy(v => v.TechnicianId!.Value)
            .ToDictionaryAsync(x => x.Key, x => x.Sum(v => v.EstimatedDurationMinutes), cancellationToken);
        var results = scoring.Score(visit, technicians, workloads);
        var response = new List<AssignmentSuggestionResponse>();
        foreach (var result in results)
        {
            var explanation = await explanations.ExplainAsync(visit, result.Technician, result.Score, cancellationToken);
            response.Add(new AssignmentSuggestionResponse(result.Technician.Id, $"{result.Technician.FirstName} {result.Technician.LastName}", result.Score, explanation, result.Reasons));
        }
        return Ok(response);
    }

    [HttpPut("{id:guid}/technician")]
    public async Task<IActionResult> Assign(Guid id, AssignTechnicianRequest request, CancellationToken cancellationToken)
    {
        var visit = await db.Visits.SingleOrDefaultAsync(v => v.Id == id, cancellationToken);
        if (visit is null) return NotFound();
        if (!await db.Technicians.AnyAsync(t => t.Id == request.TechnicianId && t.Status == TechnicianStatus.Active, cancellationToken)) return BadRequest("Technicien actif introuvable.");
        visit.TechnicianId = request.TechnicianId;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
