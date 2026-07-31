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
    }
}
