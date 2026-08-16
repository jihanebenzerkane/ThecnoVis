using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechnoVIS.Api.Migrations
{
    /// <inheritdoc />
    public partial class RefineResourcePlanning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Equipment_MaintenanceContracts_MaintenanceContractId",
                table: "Equipment");

            migrationBuilder.DropForeignKey(
                name: "FK_Equipment_Skills_RequiredSkillId",
                table: "Equipment");

            migrationBuilder.DropTable(
                name: "VisitSuggestions");

            migrationBuilder.RenameTable(name: "Skills", newName: "Specialties");
            migrationBuilder.RenameTable(name: "TechnicianSkills", newName: "TechnicianSpecialties");
            migrationBuilder.RenameColumn(name: "SkillId", table: "TechnicianSpecialties", newName: "SpecialtyId");
            migrationBuilder.RenameIndex(name: "IX_Skills_Name", table: "Specialties", newName: "IX_Specialties_Name");
            migrationBuilder.RenameIndex(name: "IX_TechnicianSkills_SkillId", table: "TechnicianSpecialties", newName: "IX_TechnicianSpecialties_SpecialtyId");

            migrationBuilder.DropIndex(
                name: "IX_Visits_EquipmentId",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Equipment");

            migrationBuilder.RenameColumn(name: "Sector", table: "Technicians", newName: "BaseLocation");
            migrationBuilder.RenameColumn(name: "Type", table: "Equipment", newName: "Category");

            migrationBuilder.RenameColumn(
                name: "RequiredSkillId",
                table: "Equipment",
                newName: "RequiredSpecialtyId");

            migrationBuilder.RenameColumn(
                name: "MaintenanceContractId",
                table: "Equipment",
                newName: "ClientSiteId");

            migrationBuilder.RenameIndex(
                name: "IX_Equipment_RequiredSkillId",
                table: "Equipment",
                newName: "IX_Equipment_RequiredSpecialtyId");

            migrationBuilder.RenameIndex(
                name: "IX_Equipment_MaintenanceContractId",
                table: "Equipment",
                newName: "IX_Equipment_ClientSiteId");

            migrationBuilder.AddColumn<int>(
                name: "ActualDurationMinutes",
                table: "Visits",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Visits",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EstimatedDurationMinutes",
                table: "Visits",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "OtherType",
                table: "Visits",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Visits",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Technicians",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmployeeNumber",
                table: "Technicians",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateOnly>(
                name: "HireDate",
                table: "Technicians",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Technicians",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Technicians",
                type: "int",
                nullable: false,
                defaultValue: 120);

            migrationBuilder.AddColumn<int>(
                name: "WeeklyWorkCapacityMinutes",
                table: "Technicians",
                type: "int",
                nullable: false,
                defaultValue: 2400);

            migrationBuilder.AddColumn<string>(
                name: "Comment",
                table: "MaintenanceContracts",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompletedVisitCount",
                table: "MaintenanceContracts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "InvoiceAvailable",
                table: "MaintenanceContracts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PvAvailable",
                table: "MaintenanceContracts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Reference",
                table: "MaintenanceContracts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Site",
                table: "MaintenanceContracts",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "VisitsPerYear",
                table: "MaintenanceContracts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Criticality",
                table: "Equipment",
                type: "int",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<DateOnly>(
                name: "InstallationDate",
                table: "Equipment",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Equipment",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "Equipment",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "Reference",
                table: "Equipment",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Equipment",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE Technicians SET EmployeeNumber = CONCAT('MIGRATED-', CONVERT(varchar(36), Id)) WHERE EmployeeNumber = '';
                UPDATE Technicians SET Email = CONCAT('migrated-', CONVERT(varchar(36), Id), '@local.invalid') WHERE Email = '';
                UPDATE MaintenanceContracts SET Reference = CONCAT('MIGRATED-', CONVERT(varchar(36), Id)) WHERE Reference = '';
                UPDATE Equipment SET Reference = CONCAT('MIGRATED-', CONVERT(varchar(36), Id)) WHERE Reference = '';
                UPDATE Equipment SET Name = Category WHERE Name = '';
                """);

            migrationBuilder.CreateTable(
                name: "Clients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClientSites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientSites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientSites_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Visits_EquipmentId_ScheduledDate",
                table: "Visits",
                columns: new[] { "EquipmentId", "ScheduledDate" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Visit_ActualDuration",
                table: "Visits",
                sql: "[ActualDurationMinutes] IS NULL OR [ActualDurationMinutes] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Visit_EstimatedDuration",
                table: "Visits",
                sql: "[EstimatedDurationMinutes] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Visit_OtherType",
                table: "Visits",
                sql: "[Type] <> 4 OR NULLIF(LTRIM(RTRIM([OtherType])), '') IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Technicians_Email",
                table: "Technicians",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Technicians_EmployeeNumber",
                table: "Technicians",
                column: "EmployeeNumber",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Technician_WeeklyCapacity",
                table: "Technicians",
                sql: "[WeeklyWorkCapacityMinutes] > 0");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceContracts_Reference",
                table: "MaintenanceContracts",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_Reference",
                table: "Equipment",
                column: "Reference",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Equipment_Criticality",
                table: "Equipment",
                sql: "[Criticality] BETWEEN 1 AND 5");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_Name",
                table: "Clients",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClientSites_ClientId_Name",
                table: "ClientSites",
                columns: new[] { "ClientId", "Name" },
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO Clients (Id, Name)
                SELECT NEWID(), source.Client
                FROM (SELECT DISTINCT Client FROM MaintenanceContracts) source;

                INSERT INTO ClientSites (Id, ClientId, Name)
                SELECT NEWID(), c.Id, mc.Site
                FROM (SELECT DISTINCT Client, Site FROM MaintenanceContracts) mc
                INNER JOIN Clients c ON c.Name = mc.Client;

                UPDATE e SET ClientSiteId = cs.Id
                FROM Equipment e
                INNER JOIN MaintenanceContracts mc ON mc.Id = e.ClientSiteId
                INNER JOIN Clients c ON c.Name = mc.Client
                INNER JOIN ClientSites cs ON cs.ClientId = c.Id AND cs.Name = mc.Site;

                """);

            migrationBuilder.AddForeignKey(
                name: "FK_Equipment_ClientSites_ClientSiteId",
                table: "Equipment",
                column: "ClientSiteId",
                principalTable: "ClientSites",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Equipment_Specialties_RequiredSpecialtyId",
                table: "Equipment",
                column: "RequiredSpecialtyId",
                principalTable: "Specialties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Equipment_ClientSites_ClientSiteId",
                table: "Equipment");

            migrationBuilder.DropForeignKey(
                name: "FK_Equipment_Specialties_RequiredSpecialtyId",
                table: "Equipment");

            migrationBuilder.DropTable(
                name: "ClientSites");

            migrationBuilder.DropTable(
                name: "TechnicianSpecialties");

            migrationBuilder.DropTable(
                name: "Clients");

            migrationBuilder.DropTable(
                name: "Specialties");

            migrationBuilder.DropIndex(
                name: "IX_Visits_EquipmentId_ScheduledDate",
                table: "Visits");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Visit_ActualDuration",
                table: "Visits");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Visit_EstimatedDuration",
                table: "Visits");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Visit_OtherType",
                table: "Visits");

            migrationBuilder.DropIndex(
                name: "IX_Technicians_Email",
                table: "Technicians");

            migrationBuilder.DropIndex(
                name: "IX_Technicians_EmployeeNumber",
                table: "Technicians");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Technician_WeeklyCapacity",
                table: "Technicians");

            migrationBuilder.DropIndex(
                name: "IX_MaintenanceContracts_Reference",
                table: "MaintenanceContracts");

            migrationBuilder.DropIndex(
                name: "IX_Equipment_Reference",
                table: "Equipment");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Equipment_Criticality",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "ActualDurationMinutes",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "EstimatedDurationMinutes",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "OtherType",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "BaseLocation",
                table: "Technicians");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Technicians");

            migrationBuilder.DropColumn(
                name: "EmployeeNumber",
                table: "Technicians");

            migrationBuilder.DropColumn(
                name: "HireDate",
                table: "Technicians");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Technicians");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Technicians");

            migrationBuilder.DropColumn(
                name: "WeeklyWorkCapacityMinutes",
                table: "Technicians");

            migrationBuilder.DropColumn(
                name: "Comment",
                table: "MaintenanceContracts");

            migrationBuilder.DropColumn(
                name: "CompletedVisitCount",
                table: "MaintenanceContracts");

            migrationBuilder.DropColumn(
                name: "InvoiceAvailable",
                table: "MaintenanceContracts");

            migrationBuilder.DropColumn(
                name: "PvAvailable",
                table: "MaintenanceContracts");

            migrationBuilder.DropColumn(
                name: "Reference",
                table: "MaintenanceContracts");

            migrationBuilder.DropColumn(
                name: "Site",
                table: "MaintenanceContracts");

            migrationBuilder.DropColumn(
                name: "VisitsPerYear",
                table: "MaintenanceContracts");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "Criticality",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "InstallationDate",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "Reference",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Equipment");

            migrationBuilder.RenameColumn(
                name: "RequiredSpecialtyId",
                table: "Equipment",
                newName: "RequiredSkillId");

            migrationBuilder.RenameColumn(
                name: "ClientSiteId",
                table: "Equipment",
                newName: "MaintenanceContractId");

            migrationBuilder.RenameIndex(
                name: "IX_Equipment_RequiredSpecialtyId",
                table: "Equipment",
                newName: "IX_Equipment_RequiredSkillId");

            migrationBuilder.RenameIndex(
                name: "IX_Equipment_ClientSiteId",
                table: "Equipment",
                newName: "IX_Equipment_MaintenanceContractId");

            migrationBuilder.AddColumn<string>(
                name: "Sector",
                table: "Technicians",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Equipment",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Equipment",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Skills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Skills", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VisitSuggestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SuggestedTechnicianId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VisitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Explanation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitSuggestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VisitSuggestions_Technicians_SuggestedTechnicianId",
                        column: x => x.SuggestedTechnicianId,
                        principalTable: "Technicians",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VisitSuggestions_Visits_VisitId",
                        column: x => x.VisitId,
                        principalTable: "Visits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TechnicianSkills",
                columns: table => new
                {
                    TechnicianId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkillId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CertificationLevel = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnicianSkills", x => new { x.TechnicianId, x.SkillId });
                    table.ForeignKey(
                        name: "FK_TechnicianSkills_Skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TechnicianSkills_Technicians_TechnicianId",
                        column: x => x.TechnicianId,
                        principalTable: "Technicians",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Visits_EquipmentId",
                table: "Visits",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Skills_Name",
                table: "Skills",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TechnicianSkills_SkillId",
                table: "TechnicianSkills",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitSuggestions_SuggestedTechnicianId",
                table: "VisitSuggestions",
                column: "SuggestedTechnicianId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitSuggestions_VisitId",
                table: "VisitSuggestions",
                column: "VisitId");

            migrationBuilder.AddForeignKey(
                name: "FK_Equipment_MaintenanceContracts_MaintenanceContractId",
                table: "Equipment",
                column: "MaintenanceContractId",
                principalTable: "MaintenanceContracts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Equipment_Skills_RequiredSkillId",
                table: "Equipment",
                column: "RequiredSkillId",
                principalTable: "Skills",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
