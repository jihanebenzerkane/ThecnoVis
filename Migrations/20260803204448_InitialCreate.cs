using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TechnoVIS.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodeClient = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NomSociete = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactPrincipal = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Telephone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Adresse = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Marches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodeMarche = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Libelle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    DateDebut = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateFin = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SlaHeures = table.Column<int>(type: "int", nullable: false),
                    VisitesAnnuellesPrevues = table.Column<int>(type: "int", nullable: false),
                    VisitesRealisees = table.Column<int>(type: "int", nullable: false),
                    Statut = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Marches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Marches_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodeSite = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NomSite = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClientId = table.Column<int>(type: "int", nullable: false),
                    Adresse = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ville = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CodePostal = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sites_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Equipements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SerialNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nom = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Categorie = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SiteId = table.Column<int>(type: "int", nullable: false),
                    DateInstallation = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Criticiticite = table.Column<int>(type: "int", nullable: false),
                    ScoreSante = table.Column<int>(type: "int", nullable: false),
                    ScoreRisque = table.Column<int>(type: "int", nullable: false),
                    Statut = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DerniereVisite = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProchaineVisitePrevue = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Equipements_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Techniciens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nom = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Prenom = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Specialites = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SiteRattacheId = table.Column<int>(type: "int", nullable: false),
                    ChargeActuelle = table.Column<int>(type: "int", nullable: false),
                    Disponible = table.Column<bool>(type: "bit", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "Visites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Reference = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TypeVisite = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EquipementId = table.Column<int>(type: "int", nullable: false),
                    TechnicienAssigne = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DatePrevue = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateRealisee = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DureeEstimeeMinutes = table.Column<int>(type: "int", nullable: false),
                    Statut = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScorePriorite = table.Column<double>(type: "float", nullable: false),
                    RapportTechnique = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActionsCorrectives = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Visites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Visites_Equipements_EquipementId",
                        column: x => x.EquipementId,
                        principalTable: "Equipements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Clients",
                columns: new[] { "Id", "Adresse", "CodeClient", "ContactPrincipal", "Email", "NomSociete", "Telephone" },
                values: new object[,]
                {
                    { 1, "Bd Zerktouni, Casablanca", "CL-001", "Karim Benali", "k.benali@totalenergies.ma", "TotalEnergies Maroc", "+212 522 10 20 30" },
                    { 2, "Zone Industrielle, Safi", "CL-002", "Sarah Mansouri", "s.mansouri@ocpgroup.ma", "OCP Group Safi", "+212 524 88 99 00" },
                    { 3, "Sidi Maârouf, Casablanca", "CL-003", "Youssef Tazi", "y.tazi@attijariwafa.com", "Attijariwafa Data Center", "+212 522 45 67 89" }
                });

            migrationBuilder.InsertData(
                table: "Marches",
                columns: new[] { "Id", "ClientId", "CodeMarche", "DateDebut", "DateFin", "Libelle", "SlaHeures", "Statut", "VisitesAnnuellesPrevues", "VisitesRealisees" },
                values: new object[,]
                {
                    { 1, 1, "MAR-2026-089", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 12, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), "Maintenance Préventive HVAC & Groupes Électrogènes", 12, "Actif", 24, 14 },
                    { 2, 2, "MAR-2026-112", new DateTime(2025, 6, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2027, 5, 31, 0, 0, 0, 0, DateTimeKind.Unspecified), "Maintenance Haute Tension & Transformateurs", 4, "Actif", 48, 32 },
                    { 3, 3, "MAR-2026-045", new DateTime(2026, 3, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2027, 3, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), "Audit & Maintenance Datacenter", 2, "Actif", 52, 20 }
                });

            migrationBuilder.InsertData(
                table: "Sites",
                columns: new[] { "Id", "Adresse", "ClientId", "CodePostal", "CodeSite", "Latitude", "Longitude", "NomSite", "Ville" },
                values: new object[,]
                {
                    { 1, "Tour Total, Anfa", 1, "20000", "ST-CAS-01", 33.589886, -7.6038690000000004, "Siège Social Casablanca", "Casablanca" },
                    { 2, "Km 9 Route d'El Jadida", 2, "46000", "ST-SAF-02", 32.299388999999998, -9.2371809999999996, "Complexe Chimique Safi", "Safi" },
                    { 3, "Parc Technologique", 3, "20650", "ST-CAS-02", 33.549999999999997, -7.483333, "Datacenter Tit Mellil", "Casablanca" }
                });

            migrationBuilder.InsertData(
                table: "Equipements",
                columns: new[] { "Id", "Categorie", "Criticiticite", "DateInstallation", "DerniereVisite", "Nom", "ProchaineVisitePrevue", "ScoreRisque", "ScoreSante", "SerialNumber", "SiteId", "Statut" },
                values: new object[,]
                {
                    { 1, "HVAC", 5, new DateTime(2020, 4, 12, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 7, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), "Groupe Froid Trane Centravac", new DateTime(2026, 8, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), 38, 78, "EQ-HVAC-901", 1, "Opérationnel" },
                    { 2, "Groupe Électrogène", 5, new DateTime(2019, 11, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 6, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), "Groupe Électrogène Caterpillar 1500kVA", new DateTime(2026, 7, 28, 0, 0, 0, 0, DateTimeKind.Unspecified), 74, 62, "EQ-GE-404", 3, "Maintenance Requise" },
                    { 3, "Transformateur", 4, new DateTime(2018, 6, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 7, 16, 0, 0, 0, 0, DateTimeKind.Unspecified), "Transformateur Schneider Triphasé 20kV", new DateTime(2026, 8, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), 18, 91, "EQ-TRF-208", 2, "Opérationnel" },
                    { 4, "Compresseur", 3, new DateTime(2021, 8, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 7, 21, 0, 0, 0, 0, DateTimeKind.Unspecified), "Compresseur Atlas Copco GA75", new DateTime(2026, 8, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), 22, 85, "EQ-CMP-302", 2, "Opérationnel" },
                    { 5, "TGBT", 5, new DateTime(2017, 2, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 6, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Armoire TGBT Principal Masterpact", new DateTime(2026, 8, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 82, 55, "EQ-TGBT-101", 1, "En Révision" }
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

            migrationBuilder.InsertData(
                table: "Visites",
                columns: new[] { "Id", "ActionsCorrectives", "DatePrevue", "DateRealisee", "DureeEstimeeMinutes", "EquipementId", "RapportTechnique", "Reference", "ScorePriorite", "Statut", "TechnicienAssigne", "TypeVisite" },
                values: new object[,]
                {
                    { 1, "", new DateTime(2026, 8, 2, 10, 0, 0, 0, DateTimeKind.Unspecified), null, 120, 1, "", "VIS-2026-1001", 65.5, "Planifiée", "Amine El Amrani", "Préventive" },
                    { 2, "Remplacement filtre huile et purge système.", new DateTime(2026, 7, 28, 14, 30, 0, 0, DateTimeKind.Unspecified), null, 180, 2, "Alerte pression huile moteur au démarrage.", "VIS-2026-1002", 92.0, "En retard", "Hassan Chraibi", "Curative" },
                    { 3, "Rien à signaler.", new DateTime(2026, 7, 26, 9, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 7, 26, 11, 30, 0, 0, DateTimeKind.Unspecified), 90, 3, "Analyse diélectrique huile conforme.", "VIS-2026-1003", 45.0, "Validée", "Nadia Berrada", "Audit" },
                    { 4, "", new DateTime(2026, 8, 1, 15, 0, 0, 0, DateTimeKind.Unspecified), null, 150, 5, "", "VIS-2026-1004", 88.5, "Planifiée", "Amine El Amrani", "Préventive" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Equipements_SiteId",
                table: "Equipements",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_Marches_ClientId",
                table: "Marches",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Sites_ClientId",
                table: "Sites",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Techniciens_SiteRattacheId",
                table: "Techniciens",
                column: "SiteRattacheId");

            migrationBuilder.CreateIndex(
                name: "IX_Visites_EquipementId",
                table: "Visites",
                column: "EquipementId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Marches");

            migrationBuilder.DropTable(
                name: "Techniciens");

            migrationBuilder.DropTable(
                name: "Visites");

            migrationBuilder.DropTable(
                name: "Equipements");

            migrationBuilder.DropTable(
                name: "Sites");

            migrationBuilder.DropTable(
                name: "Clients");
        }
    }
}
