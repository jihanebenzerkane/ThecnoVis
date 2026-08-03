using Microsoft.EntityFrameworkCore;
using TechnoVIS.Models;
using System;
using System.Collections.Generic;

namespace TechnoVIS.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Marche> Marches => Set<Marche>();
        public DbSet<Client> Clients => Set<Client>();
        public DbSet<Site> Sites => Set<Site>();
        public DbSet<Equipement> Equipements => Set<Equipement>();
        public DbSet<Visite> Visites => Set<Visite>();
        public DbSet<Technicien> Techniciens => Set<Technicien>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seed Clients
            modelBuilder.Entity<Client>().HasData(
                new Client { Id = 1, CodeClient = "CL-001", NomSociete = "TotalEnergies Maroc", ContactPrincipal = "Karim Benali", Email = "k.benali@totalenergies.ma", Telephone = "+212 522 10 20 30", Adresse = "Bd Zerktouni, Casablanca" },
                new Client { Id = 2, CodeClient = "CL-002", NomSociete = "OCP Group Safi", ContactPrincipal = "Sarah Mansouri", Email = "s.mansouri@ocpgroup.ma", Telephone = "+212 524 88 99 00", Adresse = "Zone Industrielle, Safi" },
                new Client { Id = 3, CodeClient = "CL-003", NomSociete = "Attijariwafa Data Center", ContactPrincipal = "Youssef Tazi", Email = "y.tazi@attijariwafa.com", Telephone = "+212 522 45 67 89", Adresse = "Sidi Maârouf, Casablanca" }
            );

            // Seed Sites
            modelBuilder.Entity<Site>().HasData(
                new Site { Id = 1, CodeSite = "ST-CAS-01", NomSite = "Siège Social Casablanca", ClientId = 1, Adresse = "Tour Total, Anfa", Ville = "Casablanca", CodePostal = "20000", Latitude = 33.589886, Longitude = -7.603869 },
                new Site { Id = 2, CodeSite = "ST-SAF-02", NomSite = "Complexe Chimique Safi", ClientId = 2, Adresse = "Km 9 Route d'El Jadida", Ville = "Safi", CodePostal = "46000", Latitude = 32.299389, Longitude = -9.237181 },
                new Site { Id = 3, CodeSite = "ST-CAS-02", NomSite = "Datacenter Tit Mellil", ClientId = 3, Adresse = "Parc Technologique", Ville = "Casablanca", CodePostal = "20650", Latitude = 33.550000, Longitude = -7.483333 }
            );

            // Seed Marches
            modelBuilder.Entity<Marche>().HasData(
                new Marche { Id = 1, CodeMarche = "MAR-2026-089", Libelle = "Maintenance Préventive HVAC & Groupes Électrogènes", ClientId = 1, DateDebut = new DateTime(2026, 1, 1), DateFin = new DateTime(2026, 12, 31), SlaHeures = 12, VisitesAnnuellesPrevues = 24, VisitesRealisees = 14, Statut = "Actif" },
                new Marche { Id = 2, CodeMarche = "MAR-2026-112", Libelle = "Maintenance Haute Tension & Transformateurs", ClientId = 2, DateDebut = new DateTime(2025, 6, 1), DateFin = new DateTime(2027, 5, 31), SlaHeures = 4, VisitesAnnuellesPrevues = 48, VisitesRealisees = 32, Statut = "Actif" },
                new Marche { Id = 3, CodeMarche = "MAR-2026-045", Libelle = "Audit & Maintenance Datacenter", ClientId = 3, DateDebut = new DateTime(2026, 3, 15), DateFin = new DateTime(2027, 3, 14), SlaHeures = 2, VisitesAnnuellesPrevues = 52, VisitesRealisees = 20, Statut = "Actif" }
            );

            // Seed Equipements
            modelBuilder.Entity<Equipement>().HasData(
                new Equipement { Id = 1, SerialNumber = "EQ-HVAC-901", Nom = "Groupe Froid Trane Centravac", Categorie = "HVAC", SiteId = 1, DateInstallation = new DateTime(2020, 4, 12), Criticiticite = 5, ScoreSante = 78, ScoreRisque = 38, Statut = "Opérationnel", DerniereVisite = new DateTime(2026, 7, 3), ProchaineVisitePrevue = new DateTime(2026, 8, 3) },
                new Equipement { Id = 2, SerialNumber = "EQ-GE-404", Nom = "Groupe Électrogène Caterpillar 1500kVA", Categorie = "Groupe Électrogène", SiteId = 3, DateInstallation = new DateTime(2019, 11, 5), Criticiticite = 5, ScoreSante = 62, ScoreRisque = 74, Statut = "Maintenance Requise", DerniereVisite = new DateTime(2026, 6, 16), ProchaineVisitePrevue = new DateTime(2026, 7, 28) },
                new Equipement { Id = 3, SerialNumber = "EQ-TRF-208", Nom = "Transformateur Schneider Triphasé 20kV", Categorie = "Transformateur", SiteId = 2, DateInstallation = new DateTime(2018, 6, 20), Criticiticite = 4, ScoreSante = 91, ScoreRisque = 18, Statut = "Opérationnel", DerniereVisite = new DateTime(2026, 7, 16), ProchaineVisitePrevue = new DateTime(2026, 8, 15) },
                new Equipement { Id = 4, SerialNumber = "EQ-CMP-302", Nom = "Compresseur Atlas Copco GA75", Categorie = "Compresseur", SiteId = 2, DateInstallation = new DateTime(2021, 8, 30), Criticiticite = 3, ScoreSante = 85, ScoreRisque = 22, Statut = "Opérationnel", DerniereVisite = new DateTime(2026, 7, 21), ProchaineVisitePrevue = new DateTime(2026, 8, 20) },
                new Equipement { Id = 5, SerialNumber = "EQ-TGBT-101", Nom = "Armoire TGBT Principal Masterpact", Categorie = "TGBT", SiteId = 1, DateInstallation = new DateTime(2017, 2, 14), Criticiticite = 5, ScoreSante = 55, ScoreRisque = 82, Statut = "En Révision", DerniereVisite = new DateTime(2026, 6, 1), ProchaineVisitePrevue = new DateTime(2026, 8, 1) }
            );

            // Seed Visites
            modelBuilder.Entity<Visite>().HasData(
                new Visite { Id = 1, Reference = "VIS-2026-1001", TypeVisite = "Préventive", EquipementId = 1, TechnicienAssigne = "Amine El Amrani", DatePrevue = new DateTime(2026, 8, 2, 10, 0, 0), DureeEstimeeMinutes = 120, Statut = "Planifiée", ScorePriorite = 65.5, RapportTechnique = "", ActionsCorrectives = "" },
                new Visite { Id = 2, Reference = "VIS-2026-1002", TypeVisite = "Curative", EquipementId = 2, TechnicienAssigne = "Hassan Chraibi", DatePrevue = new DateTime(2026, 7, 28, 14, 30, 0), DureeEstimeeMinutes = 180, Statut = "En retard", ScorePriorite = 92.0, RapportTechnique = "Alerte pression huile moteur au démarrage.", ActionsCorrectives = "Remplacement filtre huile et purge système." },
                new Visite { Id = 3, Reference = "VIS-2026-1003", TypeVisite = "Audit", EquipementId = 3, TechnicienAssigne = "Nadia Berrada", DatePrevue = new DateTime(2026, 7, 26, 9, 0, 0), DateRealisee = new DateTime(2026, 7, 26, 11, 30, 0), DureeEstimeeMinutes = 90, Statut = "Validée", ScorePriorite = 45.0, RapportTechnique = "Analyse diélectrique huile conforme.", ActionsCorrectives = "Rien à signaler." },
                new Visite { Id = 4, Reference = "VIS-2026-1004", TypeVisite = "Préventive", EquipementId = 5, TechnicienAssigne = "Amine El Amrani", DatePrevue = new DateTime(2026, 8, 1, 15, 0, 0), DureeEstimeeMinutes = 150, Statut = "Planifiée", ScorePriorite = 88.5, RapportTechnique = "", ActionsCorrectives = "" }
            );

            // Seed Techniciens
            modelBuilder.Entity<Technicien>().HasData(
                new Technicien { Id = 1, Nom = "El Amrani", Prenom = "Amine", Specialites = "HVAC,TGBT", SiteRattacheId = 1, ChargeActuelle = 3, Disponible = true },
                new Technicien { Id = 2, Nom = "Chraibi", Prenom = "Hassan", Specialites = "Groupe Électrogène,Compresseur", SiteRattacheId = 3, ChargeActuelle = 5, Disponible = true },
                new Technicien { Id = 3, Nom = "Berrada", Prenom = "Nadia", Specialites = "Transformateur,TGBT", SiteRattacheId = 2, ChargeActuelle = 1, Disponible = true },
                new Technicien { Id = 4, Nom = "Mansouri", Prenom = "Youssef", Specialites = "HVAC", SiteRattacheId = 1, ChargeActuelle = 0, Disponible = false },
                new Technicien { Id = 5, Nom = "Tazi", Prenom = "Othmane", Specialites = "Groupe Électrogène", SiteRattacheId = 2, ChargeActuelle = 2, Disponible = true }
            );
        }
    }
}
