using System;

namespace TechnoVIS.Models
{
    public class Marche
    {
        public int Id { get; set; }
        public string CodeMarche { get; set; } = string.Empty;
        public string Libelle { get; set; } = string.Empty;
        public int ClientId { get; set; }
        public Client? Client { get; set; }
        public DateTime DateDebut { get; set; }
        public DateTime DateFin { get; set; }
        public int SlaHeures { get; set; } = 24;
        public int VisitesAnnuellesPrevues { get; set; } = 12;
        public int VisitesRealisees { get; set; } = 0;
        public string Statut { get; set; } = "Actif"; // Actif, En Renouvellement, Expiré
    }
}
