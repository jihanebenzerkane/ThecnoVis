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

            // ── Visite → Technicien (FK, nullable) ─────────────────────
            modelBuilder.Entity<Visite>()
                .HasOne(v => v.Technicien)
                .WithMany(t => t.Visites)
                .HasForeignKey(v => v.TechnicienId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Visite → Marche (FK, nullable) ─────────────────────────
            modelBuilder.Entity<Visite>()
                .HasOne(v => v.Marche)
                .WithMany(m => m.Visites)
                .HasForeignKey(v => v.MarcheId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── NO SEED DATA ───────────────────────────────────────────
            // All data is imported via Excel or created via CRUD endpoints.
            // Admin account is initialized conditionally in Program.cs.
        }
    }
}
