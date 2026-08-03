using System;

namespace TechnoVIS.Models
{
    public class Technicien
    {
        public int Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string Prenom { get; set; } = string.Empty;
        
        // Comma-separated categories (e.g., "HVAC,Groupe Électrogène")
        public string Specialites { get; set; } = string.Empty;
        
        public int SiteRattacheId { get; set; }
        public Site? SiteRattache { get; set; }
        
        // Number of visites assigned this week
        public int ChargeActuelle { get; set; } = 0;
        
        public bool Disponible { get; set; } = true;
    }
}
