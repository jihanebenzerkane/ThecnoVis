using TechnoVIS.Api.Models;

namespace TechnoVIS.Api.Services;

public sealed class AzureAiOptions
{
    public const string SectionName = "AzureAi";
    public string DocumentIntelligenceEndpoint { get; init; } = string.Empty;
    public string DocumentIntelligenceKey { get; init; } = string.Empty;
    public string OpenAiEndpoint { get; init; } = string.Empty;
    public string OpenAiKey { get; init; } = string.Empty;
    public string OpenAiDeployment { get; init; } = string.Empty;
}

public sealed record OcrExtractionResult(string? RawResponseJson, string ExtractedFieldsJson, decimal? ConfidenceScore, bool RequiresManualReview = true);
public interface IOcrService { Task<OcrExtractionResult> ExtractAsync(Stream document, string fileName, CancellationToken cancellationToken); }
public interface IAssignmentExplanationService { Task<string> ExplainAsync(Visit visit, Technician technician, decimal score, CancellationToken cancellationToken); }

public sealed class ManualReviewOcrService : IOcrService
{
    public Task<OcrExtractionResult> ExtractAsync(Stream document, string fileName, CancellationToken cancellationToken) => Task.FromResult(new OcrExtractionResult(null, "{}", null));
}

public sealed class TemplateAssignmentExplanationService : IAssignmentExplanationService
{
    public Task<string> ExplainAsync(Visit visit, Technician technician, decimal score, CancellationToken cancellationToken)
    {
        var specialty = visit.Equipment.RequiredSpecialty.Name;
        var sameBase = string.Equals(visit.Equipment.ClientSite.Name, technician.BaseLocation, StringComparison.OrdinalIgnoreCase);
        return Task.FromResult($"{technician.FirstName} {technician.LastName} est recommandé (score {score:0.##}/100) : qualifié en {specialty} et {(sameBase ? "sa base correspond au site client" : "ses compétences correspondent au besoin")}.");
    }
}
