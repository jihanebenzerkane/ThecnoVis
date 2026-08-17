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
        public DbSet<Specialite> Specialites => Set<Specialite>();
        public DbSet<Utilisateur> Utilisateurs => Set<Utilisateur>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── Unique Index constraints ───────────────────────────────
            modelBuilder.Entity<Utilisateur>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Client>()
                .HasIndex(c => c.CodeClient)
                .IsUnique();

            modelBuilder.Entity<Site>()
                .HasIndex(s => s.CodeSite)
                .IsUnique();

            modelBuilder.Entity<Marche>()
                .HasIndex(m => m.CodeMarche)
                .IsUnique();

            modelBuilder.Entity<Equipement>()
                .HasIndex(e => e.SerialNumber)
                .IsUnique();

            modelBuilder.Entity<Visite>()
                .HasIndex(v => v.Reference)
                .IsUnique();

            modelBuilder.Entity<Specialite>()
                .HasIndex(s => s.Nom)
                .IsUnique();

            modelBuilder.Entity<Technicien>()
                .HasIndex(t => t.Matricule)
                .IsUnique();

            // ── Many-to-Many Technicien ↔ Specialite ──────────────────
            modelBuilder.Entity<Technicien>()
                .HasMany(t => t.Specialites)
                .WithMany(s => s.Techniciens)
                .UsingEntity(j => j.ToTable("TechnicienSpecialites"));

            // ── Visite → Technicien (FK, nullable) ─────────────────────
            modelBuilder.Entity<Visite>()
                .HasOne(v => v.Technicien)
                .WithMany(t => t.Visites)
                .HasForeignKey(v => v.TechnicienId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Visite → Equipement (FK) ───────────────────────────────
            modelBuilder.Entity<Visite>()
                .HasOne(v => v.Equipement)
                .WithMany(e => e.Visites)
                .HasForeignKey(v => v.EquipementId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── Visite → Marche (FK, nullable) ─────────────────────────
            modelBuilder.Entity<Visite>()
                .HasOne(v => v.Marche)
                .WithMany(m => m.Visites)
                .HasForeignKey(v => v.MarcheId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── NO SEED DATA IN ONMODELCREATING ────────────────────────
            // Initial reference data (e.g. standard Specialites, default Admin) is initialized safely in Program.cs.
        }
    }
}
