using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using ClosedXML.Excel;

namespace TechnoVIS.Services
{
    /// <summary>
    /// Represents a single row parsed from the Excel import file, before DB persistence.
    /// </summary>
    public class MarcheImportRow
    {
        public int RowIndex { get; set; }          // 1-based row number in Excel
        public string Reference { get; set; } = string.Empty;
        public string ClientNom { get; set; } = string.Empty;
        public DateTime DateDebut { get; set; }
        public DateTime DateFin { get; set; }
        public string TypeContrat { get; set; } = string.Empty;
        public int VisitesAnnuellesPrevues { get; set; }
        public int VisitesRealisees { get; set; }
        public int NbVisiteGlobal { get; set; }
        public string Sites { get; set; } = string.Empty; // title-cased, comma-separated city names
        public bool PvRequis { get; set; }
        public bool FactureRequise { get; set; }
        public int NombrePC { get; set; }
        public int NombrePCPortable { get; set; }
        public int NombreImprimante { get; set; }
        public int NombreServeur { get; set; }
        public string EquipementsDivers { get; set; } = string.Empty;
        public string CommentaireImport { get; set; } = string.Empty;
        public string? ParseWarning { get; set; }  // non-fatal issues recorded for display
    }

    public class ExcelImportService
    {
        // French month names → month number
        private static readonly Dictionary<string, int> FrenchMonths = new(StringComparer.OrdinalIgnoreCase)
        {
            { "janvier",   1 }, { "jan",  1 },
            { "février",   2 }, { "fev",  2 }, { "fevrier", 2 },
            { "mars",      3 },
            { "avril",     4 }, { "avr",  4 },
            { "mai",       5 },
            { "juin",      6 },
            { "juillet",   7 }, { "juil", 7 },
            { "août",      8 }, { "aout", 8 },
            { "septembre", 9 }, { "sep",  9 }, { "sept", 9 },
            { "octobre",  10 }, { "oct", 10 },
            { "novembre", 11 }, { "nov", 11 },
            { "décembre", 12 }, { "dec", 12 }, { "decembre", 12 }
        };

        /// <summary>
        /// Parses the given .xlsx stream and returns a list of MarcheImportRow.
        /// Never throws on dirty data — records warnings in ParseWarning instead.
        /// </summary>
        public List<MarcheImportRow> ParseExcel(Stream stream)
        {
            var result = new List<MarcheImportRow>();

            using var wb = new XLWorkbook(stream);
            var ws = wb.Worksheet(1);
            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
            if (lastRow < 2) return result; // empty or header-only

            // Build column index map from the header row (row 1)
            var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var headerRow = ws.Row(1);
            foreach (var cell in headerRow.CellsUsed())
            {
                var header = cell.GetString().Trim();
                if (!string.IsNullOrEmpty(header) && !headerMap.ContainsKey(header))
                    headerMap[header] = cell.Address.ColumnNumber;
            }

            int Col(string name) => headerMap.TryGetValue(name, out var c) ? c : -1;

            for (int rowNum = 2; rowNum <= lastRow; rowNum++)
            {
                var row = ws.Row(rowNum);

                // Skip fully empty rows
                if (row.IsEmpty()) continue;

                var warnings = new List<string>();
                var parsed = new MarcheImportRow { RowIndex = rowNum };

                // ── Reference ─────────────────────────────────────────────
                parsed.Reference = SafeString(row, Col("Référence"));

                // ── Client ────────────────────────────────────────────────
                parsed.ClientNom = SafeString(row, Col("Client"));

                // ── Dates ─────────────────────────────────────────────────
                parsed.DateDebut = ParseFrenchDate(row, Col("Date début"), warnings, "Date début");
                parsed.DateFin   = ParseFrenchDate(row, Col("Date fin"),   warnings, "Date fin");

                // ── Type de contrat ───────────────────────────────────────
                parsed.TypeContrat = SafeString(row, Col("Type de contrat"));

                // ── Nb visites / an  (e.g. "4 par an" → 4) ───────────────
                var visitesAnRaw = SafeString(row, Col("Nb visite / An"));
                parsed.VisitesAnnuellesPrevues = ExtractLeadingInt(visitesAnRaw, warnings, "Nb visite / An");

                // ── Nb visites réalisées ───────────────────────────────────
                parsed.VisitesRealisees = SafeInt(row, Col("Nb  visite réalisé"), warnings, "Nb visite réalisé");

                // ── Nb visites global ─────────────────────────────────────
                parsed.NbVisiteGlobal = SafeInt(row, Col("Nb visite global"), warnings, "Nb visite global");

                // ── Sites (title-case city names) ─────────────────────────
                var sitesRaw = SafeString(row, Col("Sites"));
                parsed.Sites = ToTitleCase(sitesRaw);

                // ── PV O/N ────────────────────────────────────────────────
                parsed.PvRequis = IsBoolOui(SafeString(row, Col("PV O/N")));

                // ── F O/N ─────────────────────────────────────────────────
                parsed.FactureRequise = IsBoolOui(SafeString(row, Col("F O/N")));

                // ── Equipment counts ─────────────────────────────────────
                parsed.NombrePC         = SafeInt(row, Col("Nombre de PC"),          warnings, "Nombre de PC");
                parsed.NombrePCPortable = SafeInt(row, Col("Nombre de PC Portable"), warnings, "Nombre de PC Portable");
                parsed.NombreImprimante = SafeInt(row, Col("Nombre Imprimante"),     warnings, "Nombre Imprimante");
                parsed.NombreServeur    = SafeInt(row, Col("Nombre Serveur"),        warnings, "Nombre Serveur");

                // ── Autres (raw, store as-is) ─────────────────────────────
                parsed.EquipementsDivers = SafeString(row, Col("Autres"));

                // ── Commentaire ───────────────────────────────────────────
                parsed.CommentaireImport = SafeString(row, Col("Commentaire"));

                if (warnings.Count > 0)
                    parsed.ParseWarning = string.Join("; ", warnings);

                result.Add(parsed);
            }

            return result;
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private static string SafeString(IXLRow row, int col)
        {
            if (col < 1) return string.Empty;
            return row.Cell(col).GetString().Trim();
        }

        /// <summary>
        /// Safely read an integer cell. Treats "__" and blanks as 0.
        /// </summary>
        private static int SafeInt(IXLRow row, int col, List<string> warnings, string colName)
        {
            if (col < 1) return 0;
            var raw = row.Cell(col).GetString().Trim();
            if (string.IsNullOrEmpty(raw) || raw == "__") return 0;
            if (int.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
                return val;
            warnings.Add($"{colName}: valeur non numérique '{raw}', remplacée par 0");
            return 0;
        }

        /// <summary>
        /// Extracts the leading integer from strings like "4 par an" or "12".
        /// </summary>
        private static int ExtractLeadingInt(string raw, List<string> warnings, string colName)
        {
            if (string.IsNullOrWhiteSpace(raw) || raw == "__") return 0;
            var match = Regex.Match(raw.Trim(), @"^\d+");
            if (match.Success) return int.Parse(match.Value);
            warnings.Add($"{colName}: impossible d'extraire un entier depuis '{raw}', remplacé par 0");
            return 0;
        }

        /// <summary>
        /// Parses a date cell that may be a real DateTime or a French "mois année" text string.
        /// </summary>
        private static DateTime ParseFrenchDate(IXLRow row, int col, List<string> warnings, string colName)
        {
            if (col < 1) return DateTime.Today;
            var cell = row.Cell(col);

            // Try reading as a proper DateTime cell first
            if (cell.DataType == XLDataType.DateTime)
                return cell.GetDateTime().Date;

            // Try parsing as a number (Excel serial date)
            var raw = cell.GetString().Trim();
            if (double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var serial))
            {
                try { return DateTime.FromOADate(serial).Date; } catch { }
            }

            // Try "août 2023", "Janvier 2022", etc.
            if (!string.IsNullOrEmpty(raw))
            {
                var parts = raw.Split(new[] { ' ', '-', '/' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                {
                    foreach (var part in parts)
                    {
                        if (FrenchMonths.TryGetValue(part, out var month))
                        {
                            // Find the year part (4-digit number)
                            foreach (var p in parts)
                            {
                                if (int.TryParse(p, out var year) && year > 1900 && year < 2100)
                                {
                                    return new DateTime(year, month, 1);
                                }
                            }
                        }
                    }
                }
                warnings.Add($"{colName}: date non reconnue '{raw}', date du jour utilisée");
            }

            return DateTime.Today;
        }

        /// <summary>
        /// "O", "o", "Oui", "oui", "YES" → true; anything else → false.
        /// </summary>
        private static bool IsBoolOui(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return false;
            var v = raw.Trim().ToUpperInvariant();
            return v == "O" || v == "OUI" || v == "YES" || v == "Y";
        }

        /// <summary>
        /// Converts a city/name string to Title Case.
        /// "RABAT" → "Rabat", "rabat" → "Rabat", "Ain sebaa" → "Ain Sebaa".
        /// </summary>
        private static string ToTitleCase(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(input.ToLower().Trim());
        }
    }
}
