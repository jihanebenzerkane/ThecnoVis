using Microsoft.AspNetCore.Identity;

namespace TechnoVIS.Api.Models;

public sealed class ApplicationUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
}

public enum VisitFrequency { Quarterly, SemiAnnual, Annual }
public enum ContractStatus { Active, Expired, Suspended }
public enum VisitStatus { Planned, InProgress, Completed, Overdue, Cancelled }
public enum VisitType { Preventive, Curative, Audit, Diagnostic, Other }
public enum EquipmentStatus { Operational, OutOfService, Decommissioned }
public enum TechnicianStatus { Active, OnLeave, Unavailable, Inactive }
public enum ExtractionStatus { PendingReview, Validated, Rejected }

public sealed class Client
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<ClientSite> Sites { get; set; } = new List<ClientSite>();
}

public sealed class ClientSite
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public Client Client { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public ICollection<Equipment> Equipment { get; set; } = new List<Equipment>();
}

public sealed class MaintenanceContract
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Client { get; set; } = string.Empty;
    public string Site { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public VisitFrequency VisitFrequency { get; set; }
    public int VisitsPerYear { get; set; }
    public int CompletedVisitCount { get; set; }
    public bool PvAvailable { get; set; }
    public bool InvoiceAvailable { get; set; }
    public string? Comment { get; set; }
    public ContractStatus Status { get; set; } = ContractStatus.Active;
}

public sealed class Equipment
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public Guid ClientSiteId { get; set; }
    public ClientSite ClientSite { get; set; } = null!;
    public DateOnly? InstallationDate { get; set; }
    public int Criticality { get; set; } = 3;
    public EquipmentStatus Status { get; set; } = EquipmentStatus.Operational;
    public int Quantity { get; set; } = 1;
    public Guid RequiredSpecialtyId { get; set; }
    public Specialty RequiredSpecialty { get; set; } = null!;
    public ICollection<Visit> Visits { get; set; } = new List<Visit>();
}

public sealed class Visit
{
    public Guid Id { get; set; }
    public Guid EquipmentId { get; set; }
    public Equipment Equipment { get; set; } = null!;
    public DateOnly ScheduledDate { get; set; }
    public DateOnly? ActualDate { get; set; }
    public VisitType Type { get; set; } = VisitType.Preventive;
    public string? OtherType { get; set; }
    public string? Description { get; set; }
    public int EstimatedDurationMinutes { get; set; } = 120;
    public int? ActualDurationMinutes { get; set; }
    public Guid? TechnicianId { get; set; }
    public Technician? Technician { get; set; }
    public VisitStatus Status { get; set; } = VisitStatus.Planned;
    public PvExtraction? PvExtraction { get; set; }
}

public sealed class Technician
{
    public Guid Id { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateOnly? HireDate { get; set; }
    public TechnicianStatus Status { get; set; } = TechnicianStatus.Active;
    public string BaseLocation { get; set; } = string.Empty;
    public int WeeklyWorkCapacityMinutes { get; set; } = 2400;
    public ICollection<TechnicianSpecialty> Specialties { get; set; } = new List<TechnicianSpecialty>();
    public ICollection<Visit> Visits { get; set; } = new List<Visit>();
}

public sealed class Specialty
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<TechnicianSpecialty> Technicians { get; set; } = new List<TechnicianSpecialty>();
}

public sealed class TechnicianSpecialty
{
    public Guid TechnicianId { get; set; }
    public Technician Technician { get; set; } = null!;
    public Guid SpecialtyId { get; set; }
    public Specialty Specialty { get; set; } = null!;
    public string CertificationLevel { get; set; } = string.Empty;
}

public sealed class PvExtraction
{
    public Guid Id { get; set; }
    public Guid VisitId { get; set; }
    public Visit Visit { get; set; } = null!;
    public string OriginalFilePath { get; set; } = string.Empty;
    public string? RawResponseJson { get; set; }
    public string? ExtractedFieldsJson { get; set; }
    public decimal? ConfidenceScore { get; set; }
    public ExtractionStatus Status { get; set; } = ExtractionStatus.PendingReview;
    public string? ValidatedByUserId { get; set; }
    public DateTimeOffset? ValidatedAt { get; set; }
}
