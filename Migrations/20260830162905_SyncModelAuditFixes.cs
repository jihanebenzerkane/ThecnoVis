using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechnoVIS.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelAuditFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Visites_Equipements_EquipementId",
                table: "Visites");

            migrationBuilder.RenameColumn(
                name: "Criticiticite",
                table: "Equipements",
                newName: "Criticite");

            migrationBuilder.AddForeignKey(
                name: "FK_Visites_Equipements_EquipementId",
                table: "Visites",
                column: "EquipementId",
                principalTable: "Equipements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Visites_Equipements_EquipementId",
                table: "Visites");

            migrationBuilder.RenameColumn(
                name: "Criticite",
                table: "Equipements",
                newName: "Criticiticite");

            migrationBuilder.AddForeignKey(
                name: "FK_Visites_Equipements_EquipementId",
                table: "Visites",
                column: "EquipementId",
                principalTable: "Equipements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
