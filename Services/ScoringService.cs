using System;
using TechnoVIS.Models;

namespace TechnoVIS.Services
{
    public class ScoringService
    {
        /// <summary>
        /// Calcul du score de risque d'un équipement (0 à 100).
        /// Basé sur l'âge de l'équipement, la criticité, et le temps écoulé depuis la dernière visite.
        /// </summary>
        public int CalculerScoreRisque(Equipement equipement)
        {
            if (equipement == null) return 0;

            double scoreAge = Math.Min(40, (DateTime.Now - equipement.DateInstallation).TotalDays / 365.25 * 4);
            double scoreCriticiticite = equipement.Criticiticite * 8.0; // 8 à 40
            double joursSansVisite = (DateTime.Now - equipement.DerniereVisite).TotalDays;
            double scoreVusterite = Math.Min(20, joursSansVisite / 15.0);

            int total = (int)Math.Round(scoreAge + scoreCriticiticite + scoreVusterite);
            return Math.Clamp(total, 5, 98);
        }

        /// <summary>
        /// Calcul de la priorité d'une visite de maintenance.
        /// </summary>
        public double CalculerPrioriteVisite(Equipement equipement, string typeVisite, DateTime datePrevue)
        {
            double baseScore = CalculerScoreRisque(equipement);

            if (typeVisite == "Curative")
            {
                baseScore += 35.0;
            }
            else if (typeVisite == "Audit")
            {
                baseScore += 15.0;
            }

            // Majoration si la date prévue est dépassée
            if (datePrevue < DateTime.Now.Date)
            {
                double retardJours = (DateTime.Now.Date - datePrevue.Date).TotalDays;
                baseScore += Math.Min(30, retardJours * 5.0);
            }

            return Math.Round(Math.Clamp(baseScore, 10.0, 100.0), 1);
        }

        /// <summary>
        /// Calcule un score de pertinence (0-100) pour l'affectation d'un technicien à une visite.
        /// Basé sur la compétence (40), la proximité géographique (30), la disponibilité (20), et la charge de travail (10).
        /// </summary>
        public int CalculerScoreAffectationTechnicien(Technicien technicien, Visite visite, Equipement equipement)
        {
            if (technicien == null || visite == null || equipement == null) return 0;

            int score = 0;

            // 1. Compétence (40 points)
            if (!string.IsNullOrEmpty(technicien.Specialites) && 
                !string.IsNullOrEmpty(equipement.Categorie) &&
                technicien.Specialites.Contains(equipement.Categorie, StringComparison.OrdinalIgnoreCase))
            {
                score += 40;
            }

            // 2. Proximité (30 points)
            if (technicien.SiteRattacheId == equipement.SiteId)
            {
                score += 30;
            }
            // Add +15 if same client? The prompt says "+15 if same client, else 0". 
            // We need to know the client. Site has ClientId. If we can get ClientId of both sites...
            // Let's rely on SiteRattacheId for exact site (+30).
            // Actually, we need to handle "same client". If equipement.Site.ClientId == technicien.SiteRattache.ClientId
            // Let's implement that if the data is available. If not loaded, we just use the ID check we can do.
            else if (equipement.Site != null && technicien.SiteRattache != null && 
                     equipement.Site.ClientId == technicien.SiteRattache.ClientId)
            {
                score += 15;
            }

            // 3. Disponibilité (20 points)
            if (technicien.Disponible)
            {
                score += 20;
            }

            // 4. Charge de travail (10 points max)
            int scoreCharge = 10 - (technicien.ChargeActuelle * 2);
            if (scoreCharge < 0) scoreCharge = 0;
            score += scoreCharge;

            return Math.Clamp(score, 0, 100);
        }
    }
}
