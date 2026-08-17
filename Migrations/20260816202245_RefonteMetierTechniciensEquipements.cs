using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechnoVIS.Migrations
{
    /// <inheritdoc />
    public partial class RefonteMetierTechniciensEquipements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Techniciens_Sites_SiteRattacheId",
                table: "Techniciens");

            migrationBuilder.DropIndex(
                name: "IX_Techniciens_SiteRattacheId",
                table: "Techniciens");

            migrationBuilder.DropColumn(
                name: "SiteRattacheId",
                table: "Techniciens");

            migrationBuilder.RenameColumn(
                name: "Specialites",
                table: "Techniciens",
                newName: "Statut");

            migrationBuilder.RenameColumn(
                name: "ChargeActuelle",
                table: "Techniciens",
                newName: "HeuresTravaillees");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Visites",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DureeReelleMinutes",
                table: "Visites",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TypeVisiteAutre",
                table: "Visites",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Base",
                table: "Techniciens",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateEmbauche",
                table: "Techniciens",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "HeuresHebdo",
                table: "Techniciens",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HeuresPlanifiees",
                table: "Techniciens",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Matricule",
                table: "Techniciens",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Specialites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nom = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Specialites", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TechnicienSpecialites",
                columns: table => new
                {
                    SpecialitesId = table.Column<int>(type: "int", nullable: false),
                    TechniciensId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnicienSpecialites", x => new { x.SpecialitesId, x.TechniciensId });
                    table.ForeignKey(
                        name: "FK_TechnicienSpecialites_Specialites_SpecialitesId",
                        column: x => x.SpecialitesId,
                        principalTable: "Specialites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TechnicienSpecialites_Techniciens_TechniciensId",
                        column: x => x.TechniciensId,
                        principalTable: "Techniciens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Techniciens_Matricule",
                table: "Techniciens",
                column: "Matricule",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Specialites_Nom",
                table: "Specialites",
                column: "Nom",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TechnicienSpecialites_TechniciensId",
                table: "TechnicienSpecialites",
                column: "TechniciensId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TechnicienSpecialites");

            migrationBuilder.DropTable(
                name: "Specialites");

            migrationBuilder.DropIndex(
                name: "IX_Techniciens_Matricule",
                table: "Techniciens");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Visites");

            migrationBuilder.DropColumn(
                name: "DureeReelleMinutes",
                table: "Visites");

            migrationBuilder.DropColumn(
                name: "TypeVisiteAutre",
                table: "Visites");

            migrationBuilder.DropColumn(
                name: "Base",
                table: "Techniciens");

            migrationBuilder.DropColumn(
                name: "DateEmbauche",
                table: "Techniciens");

            migrationBuilder.DropColumn(
                name: "HeuresHebdo",
                table: "Techniciens");

            migrationBuilder.DropColumn(
                name: "HeuresPlanifiees",
                table: "Techniciens");

            migrationBuilder.DropColumn(
                name: "Matricule",
                table: "Techniciens");

            migrationBuilder.RenameColumn(
                name: "Statut",
                table: "Techniciens",
                newName: "Specialites");

            migrationBuilder.RenameColumn(
                name: "HeuresTravaillees",
                table: "Techniciens",
                newName: "ChargeActuelle");

            migrationBuilder.AddColumn<int>(
                name: "SiteRattacheId",
                table: "Techniciens",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Techniciens_SiteRattacheId",
                table: "Techniciens",
                column: "SiteRattacheId");

            migrationBuilder.AddForeignKey(
                name: "FK_Techniciens_Sites_SiteRattacheId",
                table: "Techniciens",
                column: "SiteRattacheId",
                principalTable: "Sites",
                principalColumn: "Id");
        }
    }
}
