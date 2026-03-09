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
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                // 1. Header
                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text(titulo).FontSize(18).SemiBold().FontColor(Colors.Blue.Darken3);
                        col.Item().Text("Sistema de Gestión de Llaves").FontSize(10).Italic().FontColor(Colors.Grey.Medium);
                    });

                    row.RelativeItem().AlignRight().Column(col =>
                    {
                        col.Item().Text($"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(10);
                        col.Item().Text("Reporte Administrativo").FontSize(10).SemiBold();
                    });
                });

                // 2. Content (Table)
                page.Content().PaddingTop(15).Table(table =>
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
                        
                        // We assume the DTO properties match the header count or 
                        // we use a specific selection if needed.
                        // For simplicity in this implementation, we take all public properties.
                        
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
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Página ");
                    x.CurrentPageNumber();
                    x.Span(" de ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    private static IContainer CellStyleHeader(IContainer container)
    {
        return container
            .Background(Colors.Blue.Darken2)
            .PaddingVertical(8)
            .PaddingHorizontal(5)
            .DefaultTextStyle(x => x.SemiBold().FontColor(Colors.White))
            .AlignCenter();
    }

    private static IContainer CellStyleBody(IContainer container, bool isEven)
    {
        return container
            .Background(isEven ? Colors.White : Colors.Grey.Lighten4)
            .PaddingVertical(6)
            .PaddingHorizontal(5)
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten2)
            .AlignMiddle();
    }
}
