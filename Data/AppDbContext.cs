using Microsoft.EntityFrameworkCore;

namespace TechnoVIS.Data;

// Squelette uniquement — les DbSet<...> (Marche, Client, Site, Equipement, Visite...)
// sont à ajouter au fur et à mesure que le modèle de données est défini.
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
}
