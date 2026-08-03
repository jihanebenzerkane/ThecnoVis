using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TechnoVIS.Migrations
{
    /// <inheritdoc />
    public partial class AddTechniciens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Techniciens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nom = table.Column<string>(type: "TEXT", nullable: false),
                    Prenom = table.Column<string>(type: "TEXT", nullable: false),
                    Specialites = table.Column<string>(type: "TEXT", nullable: false),
                    SiteRattacheId = table.Column<int>(type: "INTEGER", nullable: false),
                    ChargeActuelle = table.Column<int>(type: "INTEGER", nullable: false),
                    Disponible = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Techniciens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Techniciens_Sites_SiteRattacheId",
                        column: x => x.SiteRattacheId,
                        principalTable: "Sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Techniciens",
                columns: new[] { "Id", "ChargeActuelle", "Disponible", "Nom", "Prenom", "SiteRattacheId", "Specialites" },
                values: new object[,]
                {
                    { 1, 3, true, "El Amrani", "Amine", 1, "HVAC,TGBT" },
                    { 2, 5, true, "Chraibi", "Hassan", 3, "Groupe Électrogène,Compresseur" },
                    { 3, 1, true, "Berrada", "Nadia", 2, "Transformateur,TGBT" },
                    { 4, 0, false, "Mansouri", "Youssef", 1, "HVAC" },
                    { 5, 2, true, "Tazi", "Othmane", 2, "Groupe Électrogène" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Techniciens_SiteRattacheId",
                table: "Techniciens",
                column: "SiteRattacheId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Techniciens");
        }
    }
}
