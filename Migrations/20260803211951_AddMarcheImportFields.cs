using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TechnoVIS.Migrations
{
    /// <inheritdoc />
    public partial class AddMarcheImportFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CommentaireImport",
                table: "Marches",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EquipementsDivers",
                table: "Marches",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FactureRequise",
                table: "Marches",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "NombreImprimante",
                table: "Marches",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NombrePC",
                table: "Marches",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NombrePCPortable",
                table: "Marches",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NombreServeur",
                table: "Marches",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "PvRequis",
                table: "Marches",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TypeContrat",
                table: "Marches",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Marches",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CommentaireImport", "EquipementsDivers", "FactureRequise", "NombreImprimante", "NombrePC", "NombrePCPortable", "NombreServeur", "PvRequis", "TypeContrat" },
                values: new object[] { null, null, false, 0, 0, 0, 0, false, null });

            migrationBuilder.UpdateData(
                table: "Marches",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CommentaireImport", "EquipementsDivers", "FactureRequise", "NombreImprimante", "NombrePC", "NombrePCPortable", "NombreServeur", "PvRequis", "TypeContrat" },
                values: new object[] { null, null, false, 0, 0, 0, 0, false, null });

            migrationBuilder.UpdateData(
                table: "Marches",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CommentaireImport", "EquipementsDivers", "FactureRequise", "NombreImprimante", "NombrePC", "NombrePCPortable", "NombreServeur", "PvRequis", "TypeContrat" },
                values: new object[] { null, null, false, 0, 0, 0, 0, false, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CommentaireImport",
                table: "Marches");

            migrationBuilder.DropColumn(
                name: "EquipementsDivers",
                table: "Marches");

            migrationBuilder.DropColumn(
                name: "FactureRequise",
                table: "Marches");

            migrationBuilder.DropColumn(
                name: "NombreImprimante",
                table: "Marches");

            migrationBuilder.DropColumn(
                name: "NombrePC",
                table: "Marches");

            migrationBuilder.DropColumn(
                name: "NombrePCPortable",
                table: "Marches");

            migrationBuilder.DropColumn(
                name: "NombreServeur",
                table: "Marches");

            migrationBuilder.DropColumn(
                name: "PvRequis",
                table: "Marches");

            migrationBuilder.DropColumn(
                name: "TypeContrat",
                table: "Marches");
        }
    }
}
