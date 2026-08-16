# TechnoVIS

TechnoVIS est une API ASP.NET Core destinée à la planification et au suivi des visites de maintenance. Elle structure les ressources de maintenance — clients, sites, équipements, techniciens et spécialités — puis recommande les techniciens les plus adaptés à une visite.

Le projet expose aujourd’hui le socle métier et les endpoints d’authentification et de recommandation. Les écrans de gestion, l’import Excel et l’automatisation OCR sont des évolutions prévues ; ils ne sont pas encore exposés par l’API.

## Objectifs métier

- Centraliser les équipements par client et site client.
- Gérer les techniciens ECS indépendamment des sites clients où ils interviennent.
- Associer les techniciens à des spécialités normalisées plutôt qu’à une liste de texte libre.
- Planifier une visite, proposer des techniciens qualifiés, puis laisser le responsable affecter librement le technicien retenu.
- Calculer les recommandations à partir de données courantes, sans enregistrer un « score technicien » obsolète en base.

## Architecture

```text
Client
  └─ Site client
       └─ Équipement
            └─ Visite ──────────────┐
                                  Technicien
                                      └─ Spécialités
```

Le projet se trouve dans `src/TechnoVIS.Api` et s’appuie sur :

- ASP.NET Core 10 et contrôleurs REST ;
- Entity Framework Core 10 avec SQL Server ;
- ASP.NET Core Identity et JWT pour l’authentification ;
- Swagger en environnement Development ;
- ClosedXML, déjà référencé pour le futur import Excel.

## Modèle métier

| Entité | Rôle |
| --- | --- |
| `Client` | Organisation cliente propriétaire de sites. |
| `ClientSite` | Site client, avec adresse et coordonnées géographiques optionnelles. |
| `Equipment` | Équipement identifié par référence, lié à un site et à une spécialité requise. |
| `Technician` | Salarié ECS : matricule, coordonnées, statut, base et capacité de travail hebdomadaire. |
| `Specialty` | Compétence normalisée (HVAC, TGBT, haute tension, etc.). |
| `TechnicianSpecialty` | Relation plusieurs-à-plusieurs entre techniciens et spécialités, avec niveau de certification. |
| `Visit` | Intervention planifiée ou réalisée sur un équipement. |
| `MaintenanceContract` | Informations contractuelles historiques et de suivi. |

### Règles de données importantes

- Une référence d’équipement, un matricule, un e-mail de technicien, un nom de client et une spécialité sont uniques.
- La criticité d’un équipement est comprise entre 1 et 5.
- La capacité hebdomadaire d’un technicien et la durée estimée d’une visite doivent être strictement positives.
- Les types de visite sont `Preventive`, `Curative`, `Audit`, `Diagnostic` et `Other`. Pour `Other`, `OtherType` est obligatoire.
- Une visite peut être affectée à un seul technicien, mais un technicien peut réaliser plusieurs visites.
- Les données de démonstration ne sont pas injectées avec `HasData`. La migration de refonte préserve et adapte les anciennes données déjà présentes.

## Recommandation des techniciens

`AssignmentScoringService` calcule un score à la demande, pour la semaine de la visite. Le score n’est pas stocké dans la base.

| Critère | Pondération | Règle actuelle |
| --- | ---: | --- |
| Spécialité | 40 points | Le technicien doit posséder la spécialité requise par l’équipement. |
| Disponibilité | 30 points | Calculée à partir des minutes restantes dans la capacité hebdomadaire. |
| Charge | 20 points | Favorise les techniciens ayant le moins de temps planifié. |
| Proximité | 10 points | Accordée lorsque la base déclarée du technicien correspond au nom du site client. |

Seuls les techniciens au statut `Active` et possédant la spécialité demandée sont recommandés. La proximité est volontairement simple à ce stade ; les coordonnées de `ClientSite` permettront de la remplacer par un calcul de distance ultérieur.

## Prérequis

- [.NET SDK 10](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server ou SQL Server Express accessible depuis votre poste
- Outil EF Core CLI (`dotnet-ef`) : `dotnet tool install --global dotnet-ef`

## Installation et démarrage

Depuis la racine du dépôt :

```powershell
dotnet restore .\src\TechnoVIS.Api\TechnoVIS.Api.csproj
Copy-Item .\src\TechnoVIS.Api\appsettings.Development.example.json .\src\TechnoVIS.Api\appsettings.Development.json
```

Modifiez ensuite `src/TechnoVIS.Api/appsettings.Development.json` :

- `ConnectionStrings:DefaultConnection` : chaîne de connexion SQL Server de développement ;
- `Jwt:SigningKey` : secret aléatoire long (au moins 32 caractères) ;
- `Jwt:Issuer` et `Jwt:Audience` : ajoutez-les si vous partez du fichier example ;
- `AzureAi:*` : laissez vide tant qu’aucune intégration Azure n’est activée.

Créez ou mettez à niveau la base, puis lancez l’API :

```powershell
dotnet ef database update --project .\src\TechnoVIS.Api --startup-project .\src\TechnoVIS.Api
dotnet run --project .\src\TechnoVIS.Api
```

L’API écoute localement sur `http://127.0.0.1:5278`. En environnement `Development`, Swagger est disponible à l’adresse `http://127.0.0.1:5278/swagger`.

> Ne versionnez jamais `appsettings.Development.json`, ni une clé JWT réelle, ni des secrets Azure. Préférez les variables d’environnement ou le gestionnaire de secrets .NET pour les environnements partagés.

## Migrations Entity Framework Core

Les migrations sont situées dans `src/TechnoVIS.Api/Migrations`.

```powershell
# Créer une migration après une modification du modèle
dotnet ef migrations add NomDeLaMigration --project .\src\TechnoVIS.Api --startup-project .\src\TechnoVIS.Api

# Vérifier que le modèle est synchronisé avec les migrations
dotnet ef migrations has-pending-model-changes --project .\src\TechnoVIS.Api --startup-project .\src\TechnoVIS.Api

# Générer un script SQL pour revue ou déploiement
dotnet ef migrations script --idempotent --project .\src\TechnoVIS.Api --startup-project .\src\TechnoVIS.Api --output .\technovis.sql
```

La migration `RefineResourcePlanning` renomme les tables de compétences existantes et crée les clients/sites à partir des contrats historiques. Relisez le script généré avant de l’appliquer à une base de production et réalisez une sauvegarde préalable.

## Authentification et rôles

Au démarrage, l’application crée les rôles `Admin`, `Planner` et `Technician` s’ils n’existent pas. Elle ne crée pas de compte administrateur par défaut.

| Endpoint | Accès | Description |
| --- | --- | --- |
| `POST /api/auth/register` | Public | Crée un compte et lui attribue le rôle `Technician`. |
| `POST /api/auth/login` | Public | Retourne un jeton JWT pour un compte valide. |
| `GET /api/visits/{id}/suggestions` | `Admin`, `Planner` | Retourne les techniciens recommandés pour une visite. |
| `PUT /api/visits/{id}/technician` | `Admin`, `Planner` | Affecte manuellement un technicien actif. |

Pour les endpoints protégés, transmettez le jeton avec l’en-tête :

```http
Authorization: Bearer <access-token>
```

Exemple de connexion :

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "planner@example.com",
  "password": "VotreMotDePasse"
}
```

## Vérification locale

```powershell
dotnet build .\src\TechnoVIS.Api\TechnoVIS.Api.csproj --no-restore
dotnet ef migrations has-pending-model-changes --project .\src\TechnoVIS.Api --startup-project .\src\TechnoVIS.Api --no-build
```

## Feuille de route

1. Ajouter les endpoints et l’interface de gestion des clients, sites, équipements, spécialités et techniciens.
2. Implémenter l’import Excel avec validation et aperçu avant confirmation.
3. Ajouter la création et la modification de visites, avec validation applicative complémentaire.
4. Remplacer la proximité textuelle par une distance calculée depuis les coordonnées.
5. Mettre en place les tableaux de bord d’heures réellement travaillées et de suivi des visites.
6. Brancher les services OCR et d’explication IA derrière leurs interfaces existantes.

## Licence

Ce dépôt ne contient pas encore de licence explicite. Ajoutez-en une avant toute redistribution ou utilisation externe.
