using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TechnoVIS.Api.Models;

namespace TechnoVIS.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<MaintenanceContract> MaintenanceContracts => Set<MaintenanceContract>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<ClientSite> ClientSites => Set<ClientSite>();
    public DbSet<Equipment> Equipment => Set<Equipment>();
    public DbSet<Visit> Visits => Set<Visit>();
    public DbSet<Technician> Technicians => Set<Technician>();
    public DbSet<Specialty> Specialties => Set<Specialty>();
    public DbSet<TechnicianSpecialty> TechnicianSpecialties => Set<TechnicianSpecialty>();
    public DbSet<PvExtraction> PvExtractions => Set<PvExtraction>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<MaintenanceContract>().Property(x => x.Reference).HasMaxLength(100).IsRequired();
        builder.Entity<MaintenanceContract>().HasIndex(x => x.Reference).IsUnique();
        builder.Entity<MaintenanceContract>().Property(x => x.Client).HasMaxLength(200).IsRequired();
        builder.Entity<MaintenanceContract>().Property(x => x.Site).HasMaxLength(150).IsRequired();
        builder.Entity<MaintenanceContract>().Property(x => x.Comment).HasMaxLength(2000);

        builder.Entity<Client>().Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Entity<Client>().HasIndex(x => x.Name).IsUnique();
        builder.Entity<ClientSite>().Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Entity<ClientSite>().Property(x => x.Address).HasMaxLength(500);
        builder.Entity<ClientSite>().Property(x => x.Latitude).HasPrecision(9, 6);
        builder.Entity<ClientSite>().Property(x => x.Longitude).HasPrecision(9, 6);
        builder.Entity<ClientSite>().HasIndex(x => new { x.ClientId, x.Name }).IsUnique();

        builder.Entity<Equipment>().Property(x => x.Reference).HasMaxLength(100).IsRequired();
        builder.Entity<Equipment>().HasIndex(x => x.Reference).IsUnique();
        builder.Entity<Equipment>().Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Entity<Equipment>().Property(x => x.Category).HasMaxLength(100).IsRequired();
        builder.Entity<Equipment>().ToTable(t => t.HasCheckConstraint("CK_Equipment_Criticality", "[Criticality] BETWEEN 1 AND 5"));

        builder.Entity<Technician>().Property(x => x.EmployeeNumber).HasMaxLength(50).IsRequired();
        builder.Entity<Technician>().HasIndex(x => x.EmployeeNumber).IsUnique();
        builder.Entity<Technician>().Property(x => x.Email).HasMaxLength(256).IsRequired();
        builder.Entity<Technician>().HasIndex(x => x.Email).IsUnique();
        builder.Entity<Technician>().Property(x => x.Phone).HasMaxLength(30);
        builder.Entity<Technician>().Property(x => x.BaseLocation).HasMaxLength(150).IsRequired();
        builder.Entity<Technician>().ToTable(t => t.HasCheckConstraint("CK_Technician_WeeklyCapacity", "[WeeklyWorkCapacityMinutes] > 0"));

        builder.Entity<Specialty>().Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Entity<Specialty>().HasIndex(x => x.Name).IsUnique();
        builder.Entity<TechnicianSpecialty>().HasKey(x => new { x.TechnicianId, x.SpecialtyId });
        builder.Entity<TechnicianSpecialty>().HasOne(x => x.Technician).WithMany(x => x.Specialties).HasForeignKey(x => x.TechnicianId);
        builder.Entity<TechnicianSpecialty>().HasOne(x => x.Specialty).WithMany(x => x.Technicians).HasForeignKey(x => x.SpecialtyId);

        builder.Entity<Visit>().HasOne(x => x.Technician).WithMany(x => x.Visits).HasForeignKey(x => x.TechnicianId).OnDelete(DeleteBehavior.NoAction);
        builder.Entity<Visit>().Property(x => x.OtherType).HasMaxLength(150);
        builder.Entity<Visit>().Property(x => x.Description).HasMaxLength(2000);
        builder.Entity<Visit>().ToTable(t => t.HasCheckConstraint("CK_Visit_OtherType", "[Type] <> 4 OR NULLIF(LTRIM(RTRIM([OtherType])), '') IS NOT NULL"));
        builder.Entity<Visit>().ToTable(t => t.HasCheckConstraint("CK_Visit_EstimatedDuration", "[EstimatedDurationMinutes] > 0"));
        builder.Entity<Visit>().ToTable(t => t.HasCheckConstraint("CK_Visit_ActualDuration", "[ActualDurationMinutes] IS NULL OR [ActualDurationMinutes] >= 0"));
        builder.Entity<Visit>().HasIndex(x => new { x.EquipmentId, x.ScheduledDate }).IsUnique();

        builder.Entity<PvExtraction>().HasOne(x => x.Visit).WithOne(x => x.PvExtraction).HasForeignKey<PvExtraction>(x => x.VisitId);
        builder.Entity<PvExtraction>().Property(x => x.ConfidenceScore).HasPrecision(5, 4);
    }
}
