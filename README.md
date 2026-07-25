# TechnoVIS

Système interne ECS de planification et suivi des visites de maintenance (PFA).

## Stack

- ASP.NET Core (.NET 10) + EF Core
- SQL Server
- Docker / docker-compose
- Razor Pages (frontend) + API Controllers (endpoints JSON) — à venir

## Lancer le projet en local

1. Copier le fichier d'environnement :
   ```bash
   cp .env.example .env
   ```
   puis renseigner un mot de passe SQL Server valide dans `.env`.

2. Lancer l'API + la base de données :
   ```bash
   docker-compose up --build
   ```

3. Vérifier que l'API répond :
   ```
   GET http://localhost:8080/health
   ```

## Structure du projet

```
Controllers/   endpoints API
Models/        entités du domaine (Marche, Client, Site, Equipement, Visite...)
Data/          AppDbContext + configuration EF Core
Services/      logique métier (scoring, planification...)
```

`Data/AppDbContext.cs` est actuellement vide (pas de `DbSet`) — les entités sont ajoutées au fur et à mesure de la définition du modèle de données.

## Migrations EF Core

Une fois les premières entités ajoutées dans `Models/` :

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

## CI

`.github/workflows/ci.yml` : restore + build à chaque push/PR sur `main`, plus une vérification que l'image Docker se construit correctement.
