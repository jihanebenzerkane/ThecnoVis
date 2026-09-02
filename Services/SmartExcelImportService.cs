using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using TechnoVIS.Data;
using TechnoVIS.Models;

namespace TechnoVIS.Services;

/// <summary>
/// Import Excel "intelligent" sans imposer un modèle strict.
/// 
/// Principe:
/// 1) cherche automatiquement la ligne d'en-têtes dans les 50 premières lignes;
/// 2) reconnaît les colonnes grâce à des alias + normalisation;
/// 3) utilise aussi le contenu des cellules pour identifier les données;
/// 4) transforme chaque ligne en données Client/Site/Marché/Équipement/Technicien;
/// 5) laisse l'utilisateur vérifier le résultat avant l'insertion en base.
/// 
/// L'IA n'est volontairement pas obligatoire dans cette première version.
/// Elle pourra être ajoutée plus tard pour les colonnes réellement ambiguës.
/// </summary>
public class SmartExcelImportService
{
    private static readonly Dictionary<string, string[]> Aliases =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["client"] = new[] { "client", "clients", "société", "societe", "nom client", "raison sociale", "entreprise", "customer", "company" },
            ["site"] = new[] { "site", "sites", "ville", "villes", "localisation", "lieu", "site client", "nom site", "agence" },
            ["marche"] = new[] { "marché", "marche", "n° marché", "numero marche", "numéro marché", "référence marché", "reference marche", "ref marché", "ref marche", "code marché", "contrat", "contract" },
            ["dateDebut"] = new[] { "date début", "date debut", "début", "debut", "start date", "date commencement" },
            ["dateFin"] = new[] { "date fin", "fin", "end date", "date expiration", "date échéance", "date echeance" },
            ["typeContrat"] = new[] { "type contrat", "type de contrat", "contrat type", "nature contrat", "type" },
            ["serial"] = new[] { "n° série", "n° de série", "numero serie", "numéro série", "numero de serie", "serial", "serial number", "serialnumber", "s/n", "sn", "matériel n°" },
            ["equipement"] = new[] { "équipement", "equipement", "nom équipement", "nom equipement", "désignation", "designation", "matériel", "materiel", "asset", "machine", "appareil" },
            ["categorie"] = new[] { "catégorie", "categorie", "famille", "type équipement", "type equipement", "family", "equipment type" },
            ["criticite"] = new[] { "criticité", "criticite", "niveau criticité", "niveau criticite", "criticité niveau", "criticality" },
            ["sante"] = new[] { "score santé", "score sante", "santé", "sante", "health score", "health", "état santé", "etat sante" },
            ["dateInstallation"] = new[] { "date installation", "date d'installation", "date mise en service", "mise en service", "installation", "commissioning date" },
            ["statut"] = new[] { "statut", "état", "etat", "status", "état opérationnel", "etat operationnel" },
            ["matricule"] = new[] { "matricule", "id technicien", "identifiant technicien", "code technicien", "employee id", "employee number" },
            ["nom"] = new[] { "nom", "nom de famille", "lastname", "last name" },
            ["prenom"] = new[] { "prénom", "prenom", "firstname", "first name" },
            ["nomComplet"] = new[] { "nom complet", "nom et prénom", "nom et prenom", "technicien", "technicien nom", "full name", "employee name" },
            ["email"] = new[] { "email", "e-mail", "mail", "courriel", "email technicien", "employee email" },
            ["telephone"] = new[] { "téléphone", "telephone", "tel", "gsm", "mobile", "phone" },
            ["base"] = new[] { "base technicien", "base", "agence technicien", "agence", "ville technicien", "localisation technicien" },
            ["specialites"] = new[] { "spécialités", "specialites", "spécialité", "specialite", "compétences", "competences", "skills", "specialities" },
            ["heures"] = new[] { "heures hebdo", "heures", "heures semaine", "capacité", "capacite", "heures/semaine", "weekly hours" },
            ["visitesAnnuelles"] = new[] { "nb visite", "nb visite / an", "visites/an", "visites annuelles", "nombre visites annuelles" },
            ["visitesRealisees"] = new[] { "nb visite réalisé", "nb visites réalisées", "visites réalisées", "visites realisees" },
            ["pv"] = new[] { "pv", "pv requis", "pv o/n", "pv oui non" },
            ["facture"] = new[] { "facture", "facture requise", "f o/n", "facture oui non" },
            ["pc"] = new[] { "nombre pc", "nb pc", "pc fixes", "pc" },
            ["pcPortable"] = new[] { "nombre pc portable", "nb pc portable", "pc portable", "portables" },
            ["imprimante"] = new[] { "nombre imprimante", "nb imprimante", "imprimante", "imprimantes" },
            ["serveur"] = new[] { "nombre serveur", "nb serveur", "serveur", "serveurs" },
            ["divers"] = new[] { "équipement divers", "equipement divers", "autres équipements", "autres equipements", "divers", "autres" },
            ["commentaire"] = new[] { "commentaire", "commentaires", "remarque", "remarques", "observation", "observations", "obs" }
        };

    private readonly AppDbContext _db;

    public SmartExcelImportService(AppDbContext db) => _db = db;

    public SmartImportAnalysis Analyze(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var analysis = new SmartImportAnalysis();

        foreach (var worksheet in workbook.Worksheets)
        {
            var used = worksheet.RangeUsed();
            if (used == null) continue;

            var header = DetectHeaderRow(worksheet);
            if (header.RowNumber <= 0) continue;

            var map = BuildColumnMap(header.Row);
            var rows = new List<SmartImportRow>();

            for (int r = header.RowNumber + 1; r <= used.LastRow().RowNumber(); r++)
            {
                var excelRow = worksheet.Row(r);
                if (excelRow.IsEmpty()) continue;

                var item = ParseRow(excelRow, map, r, worksheet.Name);
                if (item.IsEmpty) continue;

                rows.Add(item);
            }

            if (rows.Count > 0)
            {
                analysis.Sheets.Add(new SmartSheetAnalysis
                {
                    SheetName = worksheet.Name,
                    HeaderRow = header.RowNumber,
                    DetectedColumns = map.ToDictionary(x => x.Key, x => x.Value.Header),
                    Rows = rows
                });
            }
        }

        analysis.Rows = analysis.Sheets.SelectMany(s => s.Rows).ToList();
        analysis.TotalRows = analysis.Rows.Count;
        analysis.Clients = analysis.Rows.Count(x => !string.IsNullOrWhiteSpace(x.ClientNom));
        analysis.Sites = analysis.Rows.Count(x => !string.IsNullOrWhiteSpace(x.SiteNom));
        analysis.Marches = analysis.Rows.Count(x => !string.IsNullOrWhiteSpace(x.ReferenceMarche));
        analysis.Equipements = analysis.Rows.Count(x => !string.IsNullOrWhiteSpace(x.SerialNumber) || !string.IsNullOrWhiteSpace(x.EquipementNom));
        analysis.Techniciens = analysis.Rows.Count(x => !string.IsNullOrWhiteSpace(x.Matricule) || !string.IsNullOrWhiteSpace(x.TechnicienNomComplet) || !string.IsNullOrWhiteSpace(x.Email));
        analysis.Mappings = BuildMappingSummary(analysis.Sheets);

        return analysis;
    }

    public async Task<SmartImportResult> ImportAsync(SmartImportConfirmRequest request, CancellationToken ct = default)
    {
        if (request.Rows == null || request.Rows.Count == 0)
            throw new InvalidOperationException("Aucune donnée à importer.");

        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(ct);

            try
            {
                var clients = await _db.Clients.ToListAsync(ct);
                var sites = await _db.Sites.ToListAsync(ct);
                var marches = await _db.Marches.ToListAsync(ct);
                var equipements = await _db.Equipements.ToListAsync(ct);
                var techniciens = await _db.Techniciens.Include(t => t.Specialites).ToListAsync(ct);
                var specialites = await _db.Specialites.ToListAsync(ct);

                var result = new SmartImportResult();

            foreach (var row in request.Rows)
            {
                // 1. Client
                Client? client = null;
                if (!string.IsNullOrWhiteSpace(row.ClientNom))
                {
                    var clientName = row.ClientNom.Trim();
                    client = clients.FirstOrDefault(c =>
                        Normalize(c.NomSociete) == Normalize(clientName));

                    if (client == null)
                    {
                        client = new Client
                        {
                            NomSociete = clientName,
                            CodeClient = await UniqueClientCodeAsync(clientName, ct),
                            ContactPrincipal = string.Empty,
                            Email = string.Empty,
                            Telephone = string.Empty,
                            Adresse = string.Empty
                        };
                        _db.Clients.Add(client);
                        await _db.SaveChangesAsync(ct);
                        clients.Add(client);
                        result.ClientsCreated++;
                    }
                }

                // 2. Site
                Site? site = null;
                if (client != null && !string.IsNullOrWhiteSpace(row.SiteNom))
                {
                    var siteName = row.SiteNom.Trim();
                    var city = ToTitle(siteName);

                    site = sites.FirstOrDefault(s =>
                        s.ClientId == client.Id &&
                        Normalize(s.Ville) == Normalize(city));

                    if (site == null)
                    {
                        site = new Site
                        {
                            ClientId = client.Id,
                            NomSite = $"Site {city}",
                            Ville = city,
                            Adresse = string.Empty,
                            CodePostal = string.Empty,
                            CodeSite = await UniqueSiteCodeAsync(city, ct)
                        };
                        _db.Sites.Add(site);
                        await _db.SaveChangesAsync(ct);
                        sites.Add(site);
                        result.SitesCreated++;
                    }
                }

                // 3. Marché
                if (client != null && !string.IsNullOrWhiteSpace(row.ReferenceMarche))
                {
                    var reference = row.ReferenceMarche.Trim();
                    var marche = marches.FirstOrDefault(m =>
                        Normalize(m.CodeMarche) == Normalize(reference));

                    if (marche == null)
                    {
                        var debut = row.DateDebut ?? DateTime.Today;
                        var fin = row.DateFin ?? debut.AddYears(1);
                        if (fin < debut) fin = debut.AddYears(1);

                        marche = new Marche
                        {
                            CodeMarche = reference,
                            Libelle = reference,
                            ClientId = client.Id,
                            DateDebut = debut,
                            DateFin = fin,
                            TypeContrat = row.TypeContrat,
                            VisitesAnnuellesPrevues = row.VisitesAnnuellesPrevues ?? 12,
                            VisitesRealisees = row.VisitesRealisees ?? 0,
                            Statut = fin.Date >= DateTime.Today ? "Actif" : "Expiré",
                            PvRequis = row.PvRequis ?? false,
                            FactureRequise = row.FactureRequise ?? false,
                            NombrePC = row.NombrePC ?? 0,
                            NombrePCPortable = row.NombrePCPortable ?? 0,
                            NombreImprimante = row.NombreImprimante ?? 0,
                            NombreServeur = row.NombreServeur ?? 0,
                            EquipementsDivers = row.EquipementsDivers,
                            CommentaireImport = row.CommentaireImport,
                            SlaHeures = 24
                        };
                        _db.Marches.Add(marche);
                        await _db.SaveChangesAsync(ct);
                        marches.Add(marche);
                        result.MarchesCreated++;
                    }
                    else
                    {
                        // Mise à jour non destructive: on remplit surtout les champs importés.
                        marche.ClientId = client.Id;
                        if (row.DateDebut.HasValue) marche.DateDebut = row.DateDebut.Value;
                        if (row.DateFin.HasValue) marche.DateFin = row.DateFin.Value;
                        if (!string.IsNullOrWhiteSpace(row.TypeContrat)) marche.TypeContrat = row.TypeContrat;
                        if (row.VisitesAnnuellesPrevues.HasValue) marche.VisitesAnnuellesPrevues = row.VisitesAnnuellesPrevues.Value;
                        if (row.VisitesRealisees.HasValue) marche.VisitesRealisees = row.VisitesRealisees.Value;
                        if (row.PvRequis.HasValue) marche.PvRequis = row.PvRequis.Value;
                        if (row.FactureRequise.HasValue) marche.FactureRequise = row.FactureRequise.Value;
                        result.MarchesUpdated++;
                    }
                }

                // 4. Équipement réel (pas de faux "parc de 20 PC")
                if (site != null && (!string.IsNullOrWhiteSpace(row.SerialNumber) || !string.IsNullOrWhiteSpace(row.EquipementNom)))
                {
                    var serial = string.IsNullOrWhiteSpace(row.SerialNumber)
                        ? await UniqueEquipmentSerialAsync(ct)
                        : row.SerialNumber.Trim();

                    var existing = equipements.FirstOrDefault(e =>
                        Normalize(e.SerialNumber) == Normalize(serial));

                    if (existing == null)
                    {
                        var health = Math.Clamp(row.ScoreSante ?? 85, 0, 100);
                        var criticality = Math.Clamp(row.Criticite ?? 3, 1, 5);

                        existing = new Equipement
                        {
                            SerialNumber = serial,
                            Nom = string.IsNullOrWhiteSpace(row.EquipementNom) ? $"Équipement {serial}" : row.EquipementNom.Trim(),
                            Categorie = string.IsNullOrWhiteSpace(row.Categorie) ? "Général" : row.Categorie.Trim(),
                            SiteId = site.Id,
                            DateInstallation = row.DateInstallation ?? DateTime.Today,
                            Criticite = criticality,
                            ScoreSante = health,
                            ScoreRisque = CalculateRisk(criticality, health),
                            Statut = string.IsNullOrWhiteSpace(row.Statut) ? "Opérationnel" : row.Statut.Trim(),
                            DerniereVisite = DateTime.Today,
                            ProchaineVisitePrevue = DateTime.Today.AddMonths(3)
                        };

                        _db.Equipements.Add(existing);
                        equipements.Add(existing);
                        result.EquipementsCreated++;
                    }
                    else
                    {
                        existing.SiteId = site.Id;
                        if (!string.IsNullOrWhiteSpace(row.EquipementNom)) existing.Nom = row.EquipementNom.Trim();
                        if (!string.IsNullOrWhiteSpace(row.Categorie)) existing.Categorie = row.Categorie.Trim();
                        if (row.Criticite.HasValue) existing.Criticite = Math.Clamp(row.Criticite.Value, 1, 5);
                        if (row.ScoreSante.HasValue) existing.ScoreSante = Math.Clamp(row.ScoreSante.Value, 0, 100);
                        if (row.DateInstallation.HasValue) existing.DateInstallation = row.DateInstallation.Value;
                        if (!string.IsNullOrWhiteSpace(row.Statut)) existing.Statut = row.Statut.Trim();
                        existing.ScoreRisque = CalculateRisk(existing.Criticite, existing.ScoreSante);
                        result.EquipementsUpdated++;
                    }
                }

                // 5. Technicien
                if (!string.IsNullOrWhiteSpace(row.Matricule) ||
                    !string.IsNullOrWhiteSpace(row.TechnicienNomComplet) ||
                    !string.IsNullOrWhiteSpace(row.Email))
                {
                    var matricule = row.Matricule?.Trim();
                    var email = row.Email?.Trim();

                    var tech = techniciens.FirstOrDefault(t =>
                        (!string.IsNullOrWhiteSpace(matricule) && Normalize(t.Matricule) == Normalize(matricule)) ||
                        (!string.IsNullOrWhiteSpace(email) && Normalize(t.Email) == Normalize(email)));

                    var (prenom, nom) = SplitName(row.TechnicienNomComplet, row.Prenom, row.Nom);

                    if (tech == null)
                    {
                        tech = new Technicien
                        {
                            Matricule = string.IsNullOrWhiteSpace(matricule) ? await UniqueTechnicianCodeAsync(ct) : matricule,
                            Prenom = prenom,
                            Nom = nom,
                            Email = string.IsNullOrWhiteSpace(email) ? $"{Normalize(prenom)}.{Normalize(nom)}@technovis.ma" : email,
                            Telephone = row.Telephone ?? string.Empty,
                            Base = string.IsNullOrWhiteSpace(row.Base) ? "Casablanca" : row.Base.Trim(),
                            Statut = string.IsNullOrWhiteSpace(row.Statut) ? "Actif" : row.Statut.Trim(),
                            HeuresHebdo = row.HeuresHebdo ?? 40,
                            Disponible = true,
                            DateEmbauche = DateTime.Today
                        };

                        _db.Techniciens.Add(tech);
                        await _db.SaveChangesAsync(ct);
                        techniciens.Add(tech);
                        result.TechniciensCreated++;
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(prenom)) tech.Prenom = prenom;
                        if (!string.IsNullOrWhiteSpace(nom)) tech.Nom = nom;
                        if (!string.IsNullOrWhiteSpace(email)) tech.Email = email;
                        if (!string.IsNullOrWhiteSpace(row.Telephone)) tech.Telephone = row.Telephone.Trim();
                        if (!string.IsNullOrWhiteSpace(row.Base)) tech.Base = row.Base.Trim();
                        if (row.HeuresHebdo.HasValue) tech.HeuresHebdo = row.HeuresHebdo.Value;
                        result.TechniciensUpdated++;
                    }

                    foreach (var specialtyName in SplitMulti(row.Specialites))
                    {
                        var normalized = Normalize(specialtyName);
                        var specialty = specialites.FirstOrDefault(s => Normalize(s.Nom) == normalized);
                        if (specialty == null)
                        {
                            specialty = new Specialite { Nom = specialtyName.Trim(), Description = string.Empty };
                            _db.Specialites.Add(specialty);
                            await _db.SaveChangesAsync(ct);
                            specialites.Add(specialty);
                        }

                        if (!tech.Specialites.Any(s => s.Id == specialty.Id))
                            tech.Specialites.Add(specialty);
                    }
                }
            }

            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
        });
    }

    private static SmartImportRow ParseRow(
    IXLRow row,
    Dictionary<string, ColumnInfo> map,
    int rowNumber,
    string sheet)
{
    string Get(string key)
    {
        return map.TryGetValue(key, out var c)
            ? CellText(row.Cell(c.Column))
            : string.Empty;
    }

    var item = new SmartImportRow
    {
        Sheet = sheet,
        RowNumber = rowNumber,

        ClientNom = Get("client"),
        SiteNom = Get("site"),
        ReferenceMarche = Get("marche"),

        TypeContrat = Get("typeContrat"),

        SerialNumber = Get("serial"),
        EquipementNom = Get("equipement"),
        Categorie = Get("categorie"),

        Statut = Get("statut"),

        Matricule = Get("matricule"),
        Nom = Get("nom"),
        Prenom = Get("prenom"),
        TechnicienNomComplet = Get("nomComplet"),

        Email = Get("email"),
        Telephone = Get("telephone"),
        Base = Get("base"),
        Specialites = Get("specialites"),

        EquipementsDivers = Get("divers"),
        CommentaireImport = Get("commentaire")
    };

    // Dates
    item.DateDebut = GetDate(row, map, "dateDebut");
    item.DateFin = GetDate(row, map, "dateFin");
    item.DateInstallation = GetDate(row, map, "dateInstallation");

    // Nombres
    item.Criticite = GetInt(row, map, "criticite");
    item.ScoreSante = GetInt(row, map, "sante");

    item.VisitesAnnuellesPrevues =
        GetInt(row, map, "visitesAnnuelles");

    item.VisitesRealisees =
        GetInt(row, map, "visitesRealisees");

    item.NombrePC =
        GetInt(row, map, "pc");

    item.NombrePCPortable =
        GetInt(row, map, "pcPortable");

    item.NombreImprimante =
        GetInt(row, map, "imprimante");

    item.NombreServeur =
        GetInt(row, map, "serveur");

    item.HeuresHebdo =
        GetInt(row, map, "heures");

    // Oui / Non
    var pv = Get("pv");
    var facture = Get("facture");

    if (!string.IsNullOrWhiteSpace(pv))
        item.PvRequis = IsTrue(pv);

    if (!string.IsNullOrWhiteSpace(facture))
        item.FactureRequise = IsTrue(facture);

    // Si le fichier contient Nom + Prénom
    // mais pas de colonne "Nom complet"
    if (string.IsNullOrWhiteSpace(item.TechnicienNomComplet) &&
        (!string.IsNullOrWhiteSpace(item.Nom) ||
         !string.IsNullOrWhiteSpace(item.Prenom)))
    {
        item.TechnicienNomComplet =
            $"{item.Prenom} {item.Nom}".Trim();
    }

    return item;
}

    private static (int RowNumber, IXLRow Row) DetectHeaderRow(IXLWorksheet ws)
    {
        var used = ws.RangeUsed();
        if (used == null) return (-1, ws.Row(1));

        int bestRow = -1;
        int bestScore = 0;

        var last = Math.Min(used.LastRow().RowNumber(), used.FirstRow().RowNumber() + 49);

        for (int r = used.FirstRow().RowNumber(); r <= last; r++)
        {
            var row = ws.Row(r);
            int score = 0;

            foreach (var cell in row.CellsUsed())
            {
                var text = Normalize(cell.GetString());
                if (string.IsNullOrWhiteSpace(text)) continue;

                foreach (var aliases in Aliases.Values)
                {
                    if (aliases.Any(a => Normalize(a) == text))
                    {
                        score++;
                        break;
                    }
                }
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestRow = r;
            }
        }

        return bestScore >= 1 ? (bestRow, ws.Row(bestRow)) : (-1, ws.Row(1));
    }

    private static Dictionary<string, ColumnInfo> BuildColumnMap(IXLRow headerRow)
    {
        var map = new Dictionary<string, ColumnInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (var cell in headerRow.CellsUsed())
        {
            var raw = cell.GetString().Trim();
            if (string.IsNullOrWhiteSpace(raw)) continue;

            var normalized = Normalize(raw);
            string? bestKey = null;
            int bestScore = 0;

            foreach (var pair in Aliases)
            {
                foreach (var alias in pair.Value)
                {
                    var a = Normalize(alias);
                    int score = Similarity(normalized, a);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestKey = pair.Key;
                    }
                }
            }

            if (bestKey != null && bestScore >= 65 && !map.ContainsKey(bestKey))
                map[bestKey] = new ColumnInfo(cell.Address.ColumnNumber, raw, bestScore);
        }

        return map;
    }

    private static List<SmartMapping> BuildMappingSummary(IEnumerable<SmartSheetAnalysis> sheets)
    {
        var result = new Dictionary<string, SmartMapping>(StringComparer.OrdinalIgnoreCase);

        foreach (var sheet in sheets)
        {
            foreach (var pair in sheet.DetectedColumns)
            {
                if (!result.ContainsKey(pair.Key))
                {
                    result[pair.Key] = new SmartMapping
                    {
                        ExcelColumn = pair.Value,
                        TargetField = TargetLabel(pair.Key),
                        Confidence = 90
                    };
                }
            }
        }

        return result.Values.OrderBy(x => x.TargetField).ToList();
    }

    private static string TargetLabel(string key) => key switch
    {
        "client" => "Client.NomSociete",
        "site" => "Site.Ville",
        "marche" => "Marche.CodeMarche",
        "dateDebut" => "Marche.DateDebut",
        "dateFin" => "Marche.DateFin",
        "typeContrat" => "Marche.TypeContrat",
        "serial" => "Equipement.SerialNumber",
        "equipement" => "Equipement.Nom",
        "categorie" => "Equipement.Categorie",
        "criticite" => "Equipement.Criticite",
        "sante" => "Equipement.ScoreSante",
        "dateInstallation" => "Equipement.DateInstallation",
        "statut" => "Statut (selon contexte)",
        "matricule" => "Technicien.Matricule",
        "nom" => "Technicien.Nom",
        "prenom" => "Technicien.Prenom",
        "nomComplet" => "Technicien.Nom/Prenom",
        "email" => "Technicien.Email",
        "telephone" => "Technicien.Telephone",
        "base" => "Technicien.Base",
        "specialites" => "Technicien.Specialites",
        _ => key
    };

    private static int Similarity(string a, string b)
    {
        if (a == b) return 100;
        if (a.Contains(b) || b.Contains(a)) return 85;

        var aa = a.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var bb = b.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        int common = aa.Intersect(bb).Count();
        if (common > 0) return 65 + Math.Min(common * 5, 20);

        return 0;
    }

    private static string CellText(IXLCell cell)
    {
        if (cell.DataType == XLDataType.DateTime)
            return cell.GetDateTime().ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

        if (cell.DataType == XLDataType.Number)
            return cell.GetDouble().ToString(CultureInfo.InvariantCulture);

        return cell.GetString().Trim();
    }

    private static DateTime? GetDate(IXLRow row, Dictionary<string, ColumnInfo> map, string key)
    {
        if (!map.TryGetValue(key, out var c)) return null;
        var cell = row.Cell(c.Column);

        if (cell.DataType == XLDataType.DateTime)
            return cell.GetDateTime().Date;

        var raw = CellText(cell);
        if (string.IsNullOrWhiteSpace(raw)) return null;

        if (double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var oa))
        {
            try { return DateTime.FromOADate(oa).Date; } catch { }
        }

        if (DateTime.TryParse(raw, CultureInfo.GetCultureInfo("fr-FR"), DateTimeStyles.None, out var fr))
            return fr.Date;

        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var inv))
            return inv.Date;

        return null;
    }

    private static int? GetInt(IXLRow row, Dictionary<string, ColumnInfo> map, string key)
    {
        if (!map.TryGetValue(key, out var c)) return null;
        var raw = CellText(row.Cell(c.Column));
        if (string.IsNullOrWhiteSpace(raw)) return null;

        if (int.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
            return value;

        if (double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var dbl))
            return Convert.ToInt32(dbl);

        return null;
    }

    private static bool IsTrue(string value)
    {
        var v = Normalize(value);
        return v is "o" or "oui" or "yes" or "y" or "1" or "vrai" or "true";
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var text = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (var ch in text)
        {
            var category = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }

        return Regex.Replace(sb.ToString().Normalize(NormalizationForm.FormC), @"[^a-z0-9]+", " ").Trim();
    }

    private static string ToTitle(string value)
        => CultureInfo.GetCultureInfo("fr-FR").TextInfo.ToTitleCase(value.Trim().ToLower());

    private static IEnumerable<string> SplitMulti(string? raw)
        => string.IsNullOrWhiteSpace(raw)
            ? Enumerable.Empty<string>()
            : raw.Split(new[] { ',', ';', '/', '|', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                 .Select(x => x.Trim())
                 .Where(x => x.Length > 0);

    private static (string prenom, string nom) SplitName(string? full, string? prenom, string? nom)
    {
        if (!string.IsNullOrWhiteSpace(prenom) || !string.IsNullOrWhiteSpace(nom))
            return (prenom?.Trim() ?? "", nom?.Trim() ?? "");

        var parts = (full ?? "").Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return ("", "");
        if (parts.Length == 1) return (parts[0], parts[0]);
        return (parts[0], string.Join(" ", parts.Skip(1)));
    }

    private static int CalculateRisk(int criticite, int health)
        => Math.Clamp((criticite * 20) + (100 - health) / 2, 0, 100);

    private async Task<string> UniqueClientCodeAsync(string name, CancellationToken ct)
    {
        var prefix = new string(Normalize(name).Replace(" ", "").Take(3).ToArray()).ToUpperInvariant();
        if (prefix.Length < 2) prefix = "CLI";

        for (int i = 1; i <= 9999; i++)
        {
            var code = $"CL-{prefix}-{i:D4}";
            if (!await _db.Clients.AnyAsync(c => c.CodeClient == code, ct)) return code;
        }

        return $"CL-{prefix}-{Guid.NewGuid():N}"[..18];
    }

    private async Task<string> UniqueSiteCodeAsync(string city, CancellationToken ct)
    {
        var prefix = new string(Normalize(city).Replace(" ", "").Take(3).ToArray()).ToUpperInvariant();
        if (prefix.Length < 2) prefix = "ST";

        for (int i = 1; i <= 9999; i++)
        {
            var code = $"ST-{prefix}-{i:D4}";
            if (!await _db.Sites.AnyAsync(s => s.CodeSite == code, ct)) return code;
        }

        return $"ST-{prefix}-{Guid.NewGuid():N}"[..18];
    }

    private async Task<string> UniqueEquipmentSerialAsync(CancellationToken ct)
    {
        for (int i = 1; i <= 999999; i++)
        {
            var serial = $"EQ-IMP-{i:D6}";
            if (!await _db.Equipements.AnyAsync(e => e.SerialNumber == serial, ct)) return serial;
        }

        return $"EQ-{Guid.NewGuid():N}"[..18];
    }

    private async Task<string> UniqueTechnicianCodeAsync(CancellationToken ct)
    {
        for (int i = 1; i <= 9999; i++)
        {
            var code = $"TECH-IMP-{i:D4}";
            if (!await _db.Techniciens.AnyAsync(t => t.Matricule == code, ct)) return code;
        }

        return $"TECH-{Guid.NewGuid():N}"[..18];
    }

    public class SmartImportAnalysis
    {
        public int TotalRows { get; set; }
        public int Clients { get; set; }
        public int Sites { get; set; }
        public int Marches { get; set; }
        public int Equipements { get; set; }
        public int Techniciens { get; set; }
        public List<SmartSheetAnalysis> Sheets { get; set; } = new();
        public List<SmartImportRow> Rows { get; set; } = new();
        public List<SmartMapping> Mappings { get; set; } = new();
    }

    public class SmartSheetAnalysis
    {
        public string SheetName { get; set; } = "";
        public int HeaderRow { get; set; }
        public Dictionary<string, string> DetectedColumns { get; set; } = new();
        public List<SmartImportRow> Rows { get; set; } = new();
    }

    public class SmartImportRow
    {
        public string Sheet { get; set; } = "";
        public int RowNumber { get; set; }
        public string ClientNom { get; set; } = "";
        public string SiteNom { get; set; } = "";
        public string ReferenceMarche { get; set; } = "";
        public DateTime? DateDebut { get; set; }
        public DateTime? DateFin { get; set; }
        public string TypeContrat { get; set; } = "";
        public int? VisitesAnnuellesPrevues { get; set; }
        public int? VisitesRealisees { get; set; }
        public bool? PvRequis { get; set; }
        public bool? FactureRequise { get; set; }
        public int? NombrePC { get; set; }
        public int? NombrePCPortable { get; set; }
        public int? NombreImprimante { get; set; }
        public int? NombreServeur { get; set; }
        public string EquipementsDivers { get; set; } = "";
        public string CommentaireImport { get; set; } = "";
        public string SerialNumber { get; set; } = "";
        public string EquipementNom { get; set; } = "";
        public string Categorie { get; set; } = "";
        public int? Criticite { get; set; }
        public int? ScoreSante { get; set; }
        public DateTime? DateInstallation { get; set; }
        public string Statut { get; set; } = "";
        public string Matricule { get; set; } = "";
        public string Nom { get; set; } = "";
        public string Prenom { get; set; } = "";
        public string TechnicienNomComplet { get; set; } = "";
        public string Email { get; set; } = "";
        public string Telephone { get; set; } = "";
        public string Base { get; set; } = "";
        public int? HeuresHebdo { get; set; }
        public string Specialites { get; set; } = "";

        public bool IsEmpty =>
            string.IsNullOrWhiteSpace(ClientNom) &&
            string.IsNullOrWhiteSpace(SiteNom) &&
            string.IsNullOrWhiteSpace(ReferenceMarche) &&
            string.IsNullOrWhiteSpace(SerialNumber) &&
            string.IsNullOrWhiteSpace(EquipementNom) &&
            string.IsNullOrWhiteSpace(Matricule) &&
            string.IsNullOrWhiteSpace(TechnicienNomComplet) &&
            string.IsNullOrWhiteSpace(Email);
    }

    public class SmartMapping
    {
        public string ExcelColumn { get; set; } = "";
        public string TargetField { get; set; } = "";
        public int Confidence { get; set; }
    }

    private record ColumnInfo(int Column, string Header, int Confidence);

    public class SmartImportConfirmRequest
    {
        public List<SmartImportRow> Rows { get; set; } = new();
    }

    public class SmartImportResult
    {
        public int ClientsCreated { get; set; }
        public int SitesCreated { get; set; }
        public int MarchesCreated { get; set; }
        public int MarchesUpdated { get; set; }
        public int EquipementsCreated { get; set; }
        public int EquipementsUpdated { get; set; }
        public int TechniciensCreated { get; set; }
        public int TechniciensUpdated { get; set; }
    }
}
