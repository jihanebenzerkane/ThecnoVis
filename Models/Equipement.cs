using System;

namespace TechnoVIS.Models
{
    public class Equipement
    {
        public int Id { get; set; }
        public string SerialNumber { get; set; } = string.Empty;
        public string Nom { get; set; } = string.Empty;
        public string Categorie { get; set; } = string.Empty; // HVAC, Groupe Électrogène, Transformateur, Compresseur, TGBT
        public int SiteId { get; set; }
        public Site? Site { get; set; }
        public DateTime DateInstallation { get; set; }
        public int Criticiticite { get; set; } = 3; // 1 (Faible) à 5 (Critique)
        public int ScoreSante { get; set; } = 85; // 0-100%
        public int ScoreRisque { get; set; } = 15; // 0-100
        public string Statut { get; set; } = "Opérationnel"; // Opérationnel, En Panne, Maintenance Requise, En Révision
        public DateTime DerniereVisite { get; set; }
        public DateTime ProchaineVisitePrevue { get; set; }
    }
}
