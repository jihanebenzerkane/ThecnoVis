using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TechnoVIS.Models;
using Microsoft.AspNetCore.Hosting;

namespace TechnoVIS.Services
{
    public class PdfExportService
    {
        private readonly IWebHostEnvironment _env;

        public PdfExportService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public byte[] GeneratePvPdf(Visite visite)
        {
            // Try to load the logo if available in wwwroot/logo.png
            string? logoPath = null;
            if (_env.WebRootPath != null)
            {
                var candidate = Path.Combine(_env.WebRootPath, "logo.png");
                if (File.Exists(candidate))
                {
                    logoPath = candidate;
                }
            }

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily(Fonts.Arial));

                    page.Header().Element(c => ComposeHeader(c, visite, logoPath));
                    page.Content().Element(c => ComposeContent(c, visite));
                    page.Footer().Element(ComposeFooter);
                });
            });

            return document.GeneratePdf();
        }

        private void ComposeHeader(IContainer container, Visite visite, string? logoPath)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text($"PV D'INTERVENTION").FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);
                    column.Item().Text($"Réf : {visite.Reference}").FontSize(14).FontColor(Colors.Grey.Darken2);
                    column.Item().Text($"Date d'édition : {System.DateTime.Now:dd/MM/yyyy}");
                });

                if (logoPath != null)
                {
                    row.ConstantItem(100).Image(logoPath);
                }
                else
                {
                    row.ConstantItem(100).Text("TechnoVIS").FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);
                }
            });
        }

        private void ComposeContent(IContainer container, Visite visite)
        {
            container.PaddingVertical(1, Unit.Centimetre).Column(column =>
            {
                column.Spacing(20);

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(2);
                    });

                    table.Cell().Element(BlockHeader).Text("Client :");
                    table.Cell().Element(BlockValue).Text(visite.Equipement?.Site?.Client?.NomSociete ?? "N/A");

                    table.Cell().Element(BlockHeader).Text("Site :");
                    table.Cell().Element(BlockValue).Text(visite.Equipement?.Site?.NomSite ?? "N/A");

                    table.Cell().Element(BlockHeader).Text("Équipement :");
                    table.Cell().Element(BlockValue).Text(visite.Equipement?.Nom ?? "N/A");

                    table.Cell().Element(BlockHeader).Text("S/N :");
                    table.Cell().Element(BlockValue).Text(visite.Equipement?.SerialNumber ?? "N/A");

                    table.Cell().Element(BlockHeader).Text("Technicien :");
                    table.Cell().Element(BlockValue).Text(visite.TechnicienAssigne ?? "N/A");

                    table.Cell().Element(BlockHeader).Text("Date réalisée :");
                    table.Cell().Element(BlockValue).Text(visite.DateRealisee?.ToString("dd/MM/yyyy HH:mm") ?? "N/A");
                });

                column.Item().Column(c =>
                {
                    c.Item().PaddingBottom(5).Text("Rapport Technique").FontSize(14).SemiBold();
                    c.Item().Background(Colors.Grey.Lighten4).Padding(10).Text(string.IsNullOrWhiteSpace(visite.RapportTechnique) ? "Aucun rapport fourni." : visite.RapportTechnique);
                });

                column.Item().Column(c =>
                {
                    c.Item().PaddingBottom(5).Text("Actions Correctives").FontSize(14).SemiBold();
                    c.Item().Background(Colors.Grey.Lighten4).Padding(10).Text(string.IsNullOrWhiteSpace(visite.ActionsCorrectives) ? "Aucune action signalée." : visite.ActionsCorrectives);
                });

                column.Item().PaddingTop(25).Row(row =>
                {
                    row.RelativeItem().AlignCenter().Text("Signature Client").SemiBold();
                    row.RelativeItem().AlignCenter().Text("Signature Technicien").SemiBold();
                });
            });
        }

        private void ComposeFooter(IContainer container)
        {
            container.AlignCenter().Text(x =>
            {
                x.Span("Page ");
                x.CurrentPageNumber();
                x.Span(" / ");
                x.TotalPages();
            });
        }

        private static IContainer BlockHeader(IContainer container)
        {
            return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5).AlignLeft();
        }

        private static IContainer BlockValue(IContainer container)
        {
            return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5).AlignLeft();
        }

        public byte[] GenerateTablePdf(string title, string[] headers, string[][] data)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                    page.Header().Element(c => c.Row(row =>
                    {
                        row.RelativeItem().Text(title).FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);
                        row.ConstantItem(150).AlignRight().Text($"Date: {System.DateTime.Now:dd/MM/yyyy HH:mm}");
                    }));

                    page.Content().PaddingVertical(1, Unit.Centimetre).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            for (int i = 0; i < headers.Length; i++)
                            {
                                columns.RelativeColumn();
                            }
                        });

                        table.Header(header =>
                        {
                            foreach (var h in headers)
                            {
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text(h).SemiBold();
                            }
                        });

                        foreach (var row in data)
                        {
                            foreach (var cell in row)
                            {
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(4).Text(cell ?? "");
                            }
                        }
                    });

                    page.Footer().Element(ComposeFooter);
                });
            });

            return document.GeneratePdf();
        }
    }
}
