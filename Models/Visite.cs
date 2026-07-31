using System;

namespace TechnoVIS.Models
{
    public class Visite
    {
        public int Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string TypeVisite { get; set; } = "Préventive"; // Préventive, Curative, Audit, Diagnostic
        public int EquipementId { get; set; }
        public Equipement? Equipement { get; set; }
        public string TechnicienAssigne { get; set; } = string.Empty;
        public DateTime DatePrevue { get; set; }
        public DateTime? DateRealisee { get; set; }
        public int DureeEstimeeMinutes { get; set; } = 120;
        public string Statut { get; set; } = "Planifiée"; // Planifiée, En cours, Validée, En retard, Annulée
        public double ScorePriorite { get; set; } = 50.0;
        public string RapportTechnique { get; set; } = string.Empty;
        public string ActionsCorrectives { get; set; } = string.Empty;
    }
}
