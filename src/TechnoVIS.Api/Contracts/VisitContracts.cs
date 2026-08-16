namespace TechnoVIS.Api.Contracts;

public sealed record AssignmentSuggestionResponse(Guid TechnicianId, string TechnicianName, decimal Score, string Explanation, string[] Reasons);
public sealed record AssignTechnicianRequest(Guid TechnicianId);
