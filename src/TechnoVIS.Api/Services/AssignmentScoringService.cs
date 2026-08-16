using TechnoVIS.Api.Models;

namespace TechnoVIS.Api.Services;

public sealed record TechnicianScore(Technician Technician, decimal Score, string[] Reasons);
public interface IAssignmentScoringService
{
    IReadOnlyList<TechnicianScore> Score(Visit visit, IEnumerable<Technician> technicians, IReadOnlyDictionary<Guid, int> plannedWorkloadMinutes);
}

public sealed class AssignmentScoringService : IAssignmentScoringService
{
    // Dynamic, auditable score: specialty 40, availability 30, workload 20, proximity 10.
    public IReadOnlyList<TechnicianScore> Score(Visit visit, IEnumerable<Technician> technicians, IReadOnlyDictionary<Guid, int> plannedWorkloadMinutes)
    {
        var requiredSpecialty = visit.Equipment.RequiredSpecialtyId;
        return technicians
            .Where(t => t.Status == TechnicianStatus.Active && t.Specialties.Any(s => s.SpecialtyId == requiredSpecialty))
            .Select(t =>
            {
                var plannedMinutes = plannedWorkloadMinutes.GetValueOrDefault(t.Id);
                var remainingMinutes = Math.Max(0, t.WeeklyWorkCapacityMinutes - plannedMinutes);
                var requiredMinutes = visit.EstimatedDurationMinutes;
                var availabilityScore = requiredMinutes == 0 ? 30m : Math.Min(30m, 30m * remainingMinutes / requiredMinutes);
                var workloadScore = Math.Max(0m, 20m * (1m - (decimal)plannedMinutes / t.WeeklyWorkCapacityMinutes));
                var proximityScore = string.Equals(t.BaseLocation, visit.Equipment.ClientSite.Name, StringComparison.OrdinalIgnoreCase) ? 10m : 0m;
                var reasons = new[]
                {
                    "spécialité requise validée (40/40)",
                    $"{remainingMinutes / 60m:0.#} h restantes ({availabilityScore:0.#}/30)",
                    $"charge planifiée : {plannedMinutes / 60m:0.#} h ({workloadScore:0.#}/20)",
                    proximityScore > 0 ? "même base que le site client (10/10)" : "base différente du site client (0/10)"
                };
                return new TechnicianScore(t, Math.Round(40m + availabilityScore + workloadScore + proximityScore, 2), reasons);
            })
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Technician.LastName)
            .ToList();
    }
}
