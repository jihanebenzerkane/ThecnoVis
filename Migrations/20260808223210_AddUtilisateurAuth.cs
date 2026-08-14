using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TechnoVIS.Migrations
{
    /// <inheritdoc />
    public partial class AddUtilisateurAuth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Utilisateurs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TechnicienId = table.Column<int>(type: "int", nullable: true),
                    DateCreation = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Utilisateurs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Utilisateurs_Techniciens_TechnicienId",
                        column: x => x.TechnicienId,
                        principalTable: "Techniciens",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "Utilisateurs",
                columns: new[] { "Id", "DateCreation", "Email", "PasswordHash", "Role", "TechnicienId" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "admin@ecs.ma", "AQAAAAIAAYagAAAAEO+dWw0R11BMEpWQwdRA2GvI/Zz0i0CAl1kAe/4g4TsfQw8i+q7VbCGVBivemERBnQ==", "Responsable", null },
                    { 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "amine@ecs.ma", "AQAAAAIAAYagAAAAEAfwmjkhDbFnKoVkIN3/eyxqVcCgiydyrEv7Ot8HZTJkriLYh4HbGk8rmV5g+ypAZw==", "Technicien", 1 },
                    { 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "hassan@ecs.ma", "AQAAAAIAAYagAAAAEFtvyOGBhUA4pT7e1vf0SkfhDfl5A/pNpt8BT8ZEf7GYtAWS0kq94L5DNqqgIx1dQw==", "Technicien", 2 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Utilisateurs_Email",
                table: "Utilisateurs",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Utilisateurs_TechnicienId",
                table: "Utilisateurs",
                column: "TechnicienId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Utilisateurs");
        }
    }
}
