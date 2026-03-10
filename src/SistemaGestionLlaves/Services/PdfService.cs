using System;
using System.Collections.Generic;
using System.Reflection;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace SistemaGestionLlaves.Services;

public class PdfService : IPdfService
{
    public byte[] GenerateReportPdf<T>(string titulo, List<string> headers, List<T> data, List<float> columnWidths)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                // 1. Header
                page.Header().PaddingBottom(20).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("KeyControl").FontSize(24).Bold().FontColor(Colors.Blue.Darken3);
                        col.Item().Text("Sistema de Gestión de Llaves").FontSize(11).FontColor(Colors.Grey.Darken2);
                        col.Item().PaddingTop(10).Text(titulo).FontSize(16).Bold().FontColor(Colors.Black);
                    });

                    row.RelativeItem().AlignRight().Column(col =>
                    {
                        col.Item().Text($"Fecha de Generación: {DateTime.Now:dd/MM/yyyy}").FontSize(10);
                        col.Item().Text($"Hora: {DateTime.Now:HH:mm}").FontSize(10);
                        col.Item().PaddingTop(10).Text("Reporte Institucional").FontSize(11).Bold().FontColor(Colors.Blue.Darken2);
                    });
                });

                // 2. Content (Table)
                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        foreach (var width in columnWidths)
                        {
                            columns.RelativeColumn(width);
                        }
                    });

                    // Table Header
                    table.Header(header =>
                    {
                        foreach (var h in headers)
                        {
                            header.Cell().Element(CellStyleHeader).Text(h);
                        }
                    });

                    // Table Body with Zebra rows
                    int index = 0;
                    foreach (var item in data)
                    {
                        var properties = item!.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        
                        foreach (var prop in properties)
                        {
                            var value = prop.GetValue(item)?.ToString() ?? "-";
                            if (prop.PropertyType == typeof(DateTime) || prop.PropertyType == typeof(DateTime?))
                            {
                                if (prop.GetValue(item) is DateTime dt)
                                    value = dt.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
                            }

                            table.Cell().Element(c => CellStyleBody(c, index % 2 == 0)).Text(value);
                        }
                        index++;
                    }
                });

                // 3. Footer
                page.Footer().PaddingTop(10).BorderTop(1).BorderColor(Colors.Grey.Lighten2).Row(row =>
                {
                    row.RelativeItem().AlignLeft().Text($"KeyControl - Sistema de Gestión de Llaves © {DateTime.Now.Year}").FontSize(9).FontColor(Colors.Grey.Darken1);
                    
                    row.RelativeItem().AlignCenter().Text(x =>
                    {
                        x.Span("Página ").FontSize(9).FontColor(Colors.Grey.Darken1);
                        x.CurrentPageNumber().FontSize(9).FontColor(Colors.Grey.Darken1);
                        x.Span(" de ").FontSize(9).FontColor(Colors.Grey.Darken1);
                        x.TotalPages().FontSize(9).FontColor(Colors.Grey.Darken1);
                    });
                    
                    row.RelativeItem().AlignRight().Text("Uso Interno").FontSize(9).FontColor(Colors.Grey.Darken1);
                });
            });
        }).GeneratePdf();
    }

    private static IContainer CellStyleHeader(IContainer container)
    {
        return container
            .Background(Colors.Blue.Darken3)
            .BorderBottom(2)
            .BorderColor(Colors.Blue.Darken4)
            .PaddingVertical(8)
            .PaddingHorizontal(8)
            .DefaultTextStyle(x => x.Bold().FontColor(Colors.White).FontSize(10))
            .AlignLeft()
            .AlignMiddle();
    }

    private static IContainer CellStyleBody(IContainer container, bool isEven)
    {
        return container
            .Background(isEven ? Colors.White : Colors.Grey.Lighten4)
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(6)
            .PaddingHorizontal(8)
            .DefaultTextStyle(x => x.FontSize(9).FontColor(Colors.Black))
            .AlignLeft()
            .AlignMiddle();
    }
}
