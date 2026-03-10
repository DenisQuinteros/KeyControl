using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SistemaGestionLlaves.Data;

namespace SistemaGestionLlaves.Controllers;

[Authorize]
public class ReportesController : Controller
{
    private readonly ApplicationDbContext _context;

    private sealed class ReservaReporteItem
    {
        public int IdReserva { get; set; }
        public string Persona { get; set; } = string.Empty;
        public string Llave { get; set; } = string.Empty;
        public string Ambiente { get; set; } = "-";
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public string Estado { get; set; } = string.Empty;
    }

    private sealed class PrestamoReporteItem
    {
        public int IdPrestamo { get; set; }
        public string Persona { get; set; } = string.Empty;
        public string Llave { get; set; } = string.Empty;
        public string Ambiente { get; set; } = "-";
        public DateTime FechaHoraPrestamo { get; set; }
        public DateTime? FechaHoraDevolucionReal { get; set; }
        public string Estado { get; set; } = string.Empty;
    }

    private sealed class TopPersonaReporteItem
    {
        public int IdPersona { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int Prestamos { get; set; }
        public int Reservas { get; set; }
        public int Total { get; set; }
    }

    private sealed class InventarioLlaveItem
    {
        public int IdLlave { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Ambiente { get; set; } = "-";
        public bool EsMaestra { get; set; }
        public string Estado { get; set; } = string.Empty;
        public int TotalPrestamos { get; set; }
        public string UltimaPersona { get; set; } = "-";
        public DateTime? UltimaFecha { get; set; }
    }

    private sealed class PrestamoVencidoItem
    {
        public int IdPrestamo { get; set; }
        public string Persona { get; set; } = string.Empty;
        public string Ci { get; set; } = string.Empty;
        public string Celular { get; set; } = "-";
        public string Llave { get; set; } = string.Empty;
        public string Ambiente { get; set; } = "-";
        public DateTime FechaHoraPrestamo { get; set; }
        public DateTime FechaEsperada { get; set; }
        public int DiasVencido { get; set; }
    }

    private sealed class ActividadAmbienteItem
    {
        public int IdAmbiente { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Ambiente { get; set; } = string.Empty;
        public int TotalPrestamos { get; set; }
        public int TotalReservas { get; set; }
        public int TotalActividad { get; set; }
        public double PromedioHorasPrestamo { get; set; }
    }

    private sealed class TopLlaveItem
    {
        public int IdLlave { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Ambiente { get; set; } = "-";
        public int TotalPrestamos { get; set; }
    }

    public ReportesController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    public async Task<IActionResult> Reservas()
    {
        await CargarFiltrosAsync();
        return View();
    }

    public async Task<IActionResult> Prestamos()
    {
        await CargarFiltrosAsync();
        return View();
    }

    public IActionResult TopPersonas()
    {
        return View();
    }

    public async Task<IActionResult> InventarioLlaves()
    {
        await CargarFiltrosAsync();
        return View();
    }

    public async Task<IActionResult> TopLlaves()
    {
        await CargarFiltrosAsync();
        return View();
    }

    public async Task<IActionResult> PrestamosVencidos()
    {
        await CargarFiltrosAsync();
        return View();
    }

    public async Task<IActionResult> ActividadAmbiente()
    {
        await CargarFiltrosAsync();
        return View();
    }

    private async Task CargarFiltrosAsync()
    {
        ViewData["Ambientes"] = new SelectList(
            await _context.Ambientes
                .AsNoTracking()
                .Where(a => a.Estado == "A")
                .OrderBy(a => a.Nombre)
                .Select(a => new { a.IdAmbiente, Nombre = a.Codigo + " — " + a.Nombre })
                .ToListAsync(),
            "IdAmbiente", "Nombre");

        ViewData["Llaves"] = new SelectList(
            await _context.Llaves
                .AsNoTracking()
                .Where(l => l.Estado != "I")
                .OrderBy(l => l.Codigo)
                .Select(l => new { l.IdLlave, Nombre = l.Codigo })
                .ToListAsync(),
            "IdLlave", "Nombre");
    }

    [HttpGet]
    public async Task<IActionResult> ReservasData(DateTime? fechaDesde, DateTime? fechaHasta, int? idAmbiente)
    {
        var data = await ObtenerReservasAsync(fechaDesde, fechaHasta, idAmbiente);

        return Ok(new
        {
            total = data.Count,
            data
        });
    }

    [HttpGet]
    public async Task<IActionResult> PrestamosData(DateTime? fechaDesde, DateTime? fechaHasta, int? idAmbiente, string? estado, string? persona)
    {
        if (ValidarFechasInconsistentes(fechaDesde, fechaHasta, out var error))
            return BadRequest(new { error });

        var data = await ObtenerPrestamosAsync(fechaDesde, fechaHasta, idAmbiente, estado, persona);

        return Ok(new
        {
            total = data.Count,
            activos = data.Count(x => x.Estado == "A"),
            devueltos = data.Count(x => x.Estado == "D"),
            vencidos = data.Count(x => x.Estado == "V"),
            cancelados = data.Count(x => x.Estado == "C"),
            data
        });
    }

    [HttpGet]
    public async Task<IActionResult> TopPersonasData(DateTime? fechaDesde, DateTime? fechaHasta, int top = 10)
    {
        if (ValidarFechasInconsistentes(fechaDesde, fechaHasta, out var error))
            return BadRequest(new { error });

        var ranking = await ObtenerTopPersonasAsync(fechaDesde, fechaHasta, top);

        return Ok(new
        {
            totalSolicitudes = ranking.Sum(x => x.Total),
            data = ranking
        });
    }

    [HttpGet]
    public async Task<IActionResult> InventarioLlavesData(int? idAmbiente, string? estado)
    {
        var data = await ObtenerInventarioLlavesAsync(idAmbiente, estado);
        return Ok(new { total = data.Count, data });
    }

    [HttpGet]
    public async Task<IActionResult> PrestamosVencidosData(DateTime? fechaDesde, DateTime? fechaHasta, int? idAmbiente, string? persona)
    {
        if (ValidarFechasInconsistentes(fechaDesde, fechaHasta, out var error))
            return BadRequest(new { error });

        var data = await ObtenerPrestamosVencidosAsync(fechaDesde, fechaHasta, idAmbiente, persona);
        return Ok(new { total = data.Count, data });
    }

    [HttpGet]
    public async Task<IActionResult> TopLlavesData(DateTime? fechaDesde, DateTime? fechaHasta, int? idAmbiente, int top = 20)
    {
        if (ValidarFechasInconsistentes(fechaDesde, fechaHasta, out var error))
            return BadRequest(new { error });

        var data = await ObtenerTopLlavesAsync(fechaDesde, fechaHasta, idAmbiente, Math.Min(Math.Max(top, 1), 100));
        return Ok(new { total = data.Count, data });
    }

    [HttpGet]
    public async Task<IActionResult> ActividadAmbienteData(DateTime? fechaDesde, DateTime? fechaHasta, int? idAmbiente)
    {
        if (ValidarFechasInconsistentes(fechaDesde, fechaHasta, out var error))
            return BadRequest(new { error });

        var data = await ObtenerActividadAmbienteAsync(fechaDesde, fechaHasta, idAmbiente);
        return Ok(new { totalActividad = data.Sum(x => x.TotalActividad), data });
    }

    [HttpGet]
    public async Task<IActionResult> ExportarReservasPdf(DateTime? fechaDesde, DateTime? fechaHasta, int? idAmbiente)
    {
        var data = await ObtenerReservasAsync(fechaDesde, fechaHasta, idAmbiente);

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text("Reporte de Reservas").SemiBold().FontSize(16);
                    col.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}").FontColor(Colors.Grey.Darken1);
                });

                page.Content().PaddingTop(12).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(0.9f);
                        columns.RelativeColumn(2.2f);
                        columns.RelativeColumn(1.2f);
                        columns.RelativeColumn(1.8f);
                        columns.RelativeColumn(1.6f);
                        columns.RelativeColumn(1.6f);
                        columns.RelativeColumn(1.1f);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(CellHeader).Text("#");
                        header.Cell().Element(CellHeader).Text("Persona");
                        header.Cell().Element(CellHeader).Text("Llave");
                        header.Cell().Element(CellHeader).Text("Ambiente");
                        header.Cell().Element(CellHeader).Text("Inicio");
                        header.Cell().Element(CellHeader).Text("Fin");
                        header.Cell().Element(CellHeader).Text("Estado");
                    });

                    foreach (var item in data)
                    {
                        table.Cell().Element(CellBody).Text($"#{item.IdReserva}");
                        table.Cell().Element(CellBody).Text(item.Persona);
                        table.Cell().Element(CellBody).Text(item.Llave);
                        table.Cell().Element(CellBody).Text(item.Ambiente);
                        table.Cell().Element(CellBody).Text(item.FechaInicio.ToLocalTime().ToString("dd/MM/yyyy HH:mm"));
                        table.Cell().Element(CellBody).Text(item.FechaFin.ToLocalTime().ToString("dd/MM/yyyy HH:mm"));
                        table.Cell().Element(CellBody).Text(TraducirEstadoReserva(item.Estado));
                    }

                    if (!data.Any())
                    {
                        table.Cell().ColumnSpan(7).Element(CellBody).AlignCenter().Text("Sin datos para el filtro seleccionado.");
                    }
                });

                page.Footer().AlignRight().Text($"Total: {data.Count} reserva(s)").FontColor(Colors.Grey.Darken1);
            });
        }).GeneratePdf();

        return File(pdf, "application/pdf", $"reporte_reservas_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
    }

    [HttpGet]
    public async Task<IActionResult> ExportarPrestamosPdf(DateTime? fechaDesde, DateTime? fechaHasta, int? idAmbiente, string? estado, string? persona)
    {
        if (ValidarFechasInconsistentes(fechaDesde, fechaHasta, out var err))
            return BadRequest(new { error = err });

        var data = await ObtenerPrestamosAsync(fechaDesde, fechaHasta, idAmbiente, estado, persona);

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text("Reporte de Préstamos").SemiBold().FontSize(16);
                    col.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}").FontColor(Colors.Grey.Darken1);
                });

                page.Content().PaddingTop(12).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(0.9f);
                        columns.RelativeColumn(2.2f);
                        columns.RelativeColumn(1.2f);
                        columns.RelativeColumn(1.8f);
                        columns.RelativeColumn(1.7f);
                        columns.RelativeColumn(1.7f);
                        columns.RelativeColumn(1.1f);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(CellHeader).Text("#");
                        header.Cell().Element(CellHeader).Text("Persona");
                        header.Cell().Element(CellHeader).Text("Llave");
                        header.Cell().Element(CellHeader).Text("Ambiente");
                        header.Cell().Element(CellHeader).Text("Préstamo");
                        header.Cell().Element(CellHeader).Text("Devolución");
                        header.Cell().Element(CellHeader).Text("Estado");
                    });

                    foreach (var item in data)
                    {
                        table.Cell().Element(CellBody).Text($"#{item.IdPrestamo}");
                        table.Cell().Element(CellBody).Text(item.Persona);
                        table.Cell().Element(CellBody).Text(item.Llave);
                        table.Cell().Element(CellBody).Text(item.Ambiente);
                        table.Cell().Element(CellBody).Text(item.FechaHoraPrestamo.ToLocalTime().ToString("dd/MM/yyyy HH:mm"));
                        table.Cell().Element(CellBody).Text(item.FechaHoraDevolucionReal.HasValue ? item.FechaHoraDevolucionReal.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm") : "-");
                        table.Cell().Element(CellBody).Text(TraducirEstadoPrestamo(item.Estado));
                    }

                    if (!data.Any())
                    {
                        table.Cell().ColumnSpan(7).Element(CellBody).AlignCenter().Text("Sin datos para el filtro seleccionado.");
                    }
                });

                page.Footer().AlignRight().Text($"Total: {data.Count} préstamo(s)").FontColor(Colors.Grey.Darken1);
            });
        }).GeneratePdf();

        return File(pdf, "application/pdf", $"reporte_prestamos_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
    }

    [HttpGet]
    public async Task<IActionResult> ExportarTopPersonasPdf(DateTime? fechaDesde, DateTime? fechaHasta, int top = 10)
    {
        var data = await ObtenerTopPersonasAsync(fechaDesde, fechaHasta, top);

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text("Top Personas Solicitantes").SemiBold().FontSize(16);
                    col.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}").FontColor(Colors.Grey.Darken1);
                });

                page.Content().PaddingTop(12).Column(col =>
                {
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(0.8f);
                            columns.RelativeColumn(2.8f);
                            columns.RelativeColumn(1.2f);
                            columns.RelativeColumn(1.2f);
                            columns.RelativeColumn(1.2f);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CellHeader).Text("#");
                            header.Cell().Element(CellHeader).Text("Persona");
                            header.Cell().Element(CellHeader).Text("Préstamos");
                            header.Cell().Element(CellHeader).Text("Reservas");
                            header.Cell().Element(CellHeader).Text("Total");
                        });

                        for (var i = 0; i < data.Count; i++)
                        {
                            var item = data[i];
                            table.Cell().Element(CellBody).Text((i + 1).ToString());
                            table.Cell().Element(CellBody).Text(item.Nombre);
                            table.Cell().Element(CellBody).Text(item.Prestamos.ToString());
                            table.Cell().Element(CellBody).Text(item.Reservas.ToString());
                            table.Cell().Element(CellBody).Text(item.Total.ToString());
                        }

                        if (!data.Any())
                        {
                            table.Cell().ColumnSpan(5).Element(CellBody).AlignCenter().Text("Sin datos para el filtro seleccionado.");
                        }
                    });
                });

                page.Footer().AlignRight().Text($"Total solicitudes: {data.Sum(x => x.Total)}").FontColor(Colors.Grey.Darken1);
            });
        }).GeneratePdf();

        return File(pdf, "application/pdf", $"reporte_top_personas_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
    }

    [HttpGet]
    public async Task<IActionResult> ExportarInventarioLlavesPdf(int? idAmbiente, string? estado)
    {
        var data = await ObtenerInventarioLlavesAsync(idAmbiente, estado);

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text("Inventario de Llaves").SemiBold().FontSize(16);
                    col.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}").FontColor(Colors.Grey.Darken1);
                });

                page.Content().PaddingTop(12).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1f);
                        columns.RelativeColumn(2f);
                        columns.RelativeColumn(0.9f);
                        columns.RelativeColumn(1.2f);
                        columns.RelativeColumn(1f);
                        columns.RelativeColumn(2.2f);
                        columns.RelativeColumn(1.5f);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(CellHeader).Text("Código");
                        header.Cell().Element(CellHeader).Text("Ambiente");
                        header.Cell().Element(CellHeader).Text("Maestra");
                        header.Cell().Element(CellHeader).Text("Estado");
                        header.Cell().Element(CellHeader).Text("Préstamos");
                        header.Cell().Element(CellHeader).Text("Último Solicitante");
                        header.Cell().Element(CellHeader).Text("Última Fecha");
                    });

                    foreach (var item in data)
                    {
                        table.Cell().Element(CellBody).Text(item.Codigo);
                        table.Cell().Element(CellBody).Text(item.Ambiente);
                        table.Cell().Element(CellBody).Text(item.EsMaestra ? "Sí" : "No");
                        table.Cell().Element(CellBody).Text(TraducirEstadoLlave(item.Estado));
                        table.Cell().Element(CellBody).Text(item.TotalPrestamos.ToString());
                        table.Cell().Element(CellBody).Text(item.UltimaPersona);
                        table.Cell().Element(CellBody).Text(item.UltimaFecha.HasValue ? item.UltimaFecha.Value.ToLocalTime().ToString("dd/MM/yyyy") : "-");
                    }

                    if (!data.Any())
                        table.Cell().ColumnSpan(7).Element(CellBody).AlignCenter().Text("Sin datos para el filtro seleccionado.");
                });

                page.Footer().AlignRight().Text($"Total: {data.Count} llave(s)").FontColor(Colors.Grey.Darken1);
            });
        }).GeneratePdf();

        return File(pdf, "application/pdf", $"reporte_inventario_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
    }

    [HttpGet]
    public async Task<IActionResult> ExportarTopLlavesPdf(DateTime? fechaDesde, DateTime? fechaHasta, int? idAmbiente, int top = 20)
    {
        if (ValidarFechasInconsistentes(fechaDesde, fechaHasta, out var err))
            return BadRequest(new { error = err });

        var data = await ObtenerTopLlavesAsync(fechaDesde, fechaHasta, idAmbiente, Math.Min(Math.Max(top, 1), 100));

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text("Ranking de Llaves Más Utilizadas").SemiBold().FontSize(16);
                    col.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}").FontColor(Colors.Grey.Darken1);
                });

                page.Content().PaddingTop(12).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(0.8f);
                        columns.RelativeColumn(1.5f);
                        columns.RelativeColumn(2.5f);
                        columns.RelativeColumn(1.2f);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(CellHeader).Text("#");
                        header.Cell().Element(CellHeader).Text("Llave");
                        header.Cell().Element(CellHeader).Text("Ambiente");
                        header.Cell().Element(CellHeader).Text("Total Préstamos");
                    });

                    for (var i = 0; i < data.Count; i++)
                    {
                        var item = data[i];
                        table.Cell().Element(CellBody).Text((i + 1).ToString());
                        table.Cell().Element(CellBody).Text(item.Codigo);
                        table.Cell().Element(CellBody).Text(item.Ambiente);
                        table.Cell().Element(CellBody).Text(item.TotalPrestamos.ToString());
                    }

                    if (!data.Any())
                        table.Cell().ColumnSpan(4).Element(CellBody).AlignCenter().Text("Sin datos para el filtro seleccionado.");
                });

                page.Footer().AlignRight().Text($"Total: {data.Count} llave(s)").FontColor(Colors.Grey.Darken1);
            });
        }).GeneratePdf();

        return File(pdf, "application/pdf", $"reporte_ranking_llaves_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
    }

    [HttpGet]
    public async Task<IActionResult> ExportarPrestamosVencidosPdf(DateTime? fechaDesde, DateTime? fechaHasta, int? idAmbiente, string? persona)
    {
        if (ValidarFechasInconsistentes(fechaDesde, fechaHasta, out var err))
            return BadRequest(new { error = err });

        var data = await ObtenerPrestamosVencidosAsync(fechaDesde, fechaHasta, idAmbiente, persona);

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text("Préstamos Vencidos / No Devueltos").SemiBold().FontSize(16);
                    col.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}").FontColor(Colors.Grey.Darken1);
                });

                page.Content().PaddingTop(12).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(0.8f);
                        columns.RelativeColumn(2f);
                        columns.RelativeColumn(1.2f);
                        columns.RelativeColumn(1.2f);
                        columns.RelativeColumn(1f);
                        columns.RelativeColumn(1.8f);
                        columns.RelativeColumn(1.2f);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(CellHeader).Text("#");
                        header.Cell().Element(CellHeader).Text("Persona");
                        header.Cell().Element(CellHeader).Text("CI");
                        header.Cell().Element(CellHeader).Text("Celular");
                        header.Cell().Element(CellHeader).Text("Llave");
                        header.Cell().Element(CellHeader).Text("Vto. Esperado");
                        header.Cell().Element(CellHeader).Text("Días vencido");
                    });

                    foreach (var item in data)
                    {
                        table.Cell().Element(CellBody).Text($"#{item.IdPrestamo}");
                        table.Cell().Element(CellBody).Text(item.Persona);
                        table.Cell().Element(CellBody).Text(item.Ci);
                        table.Cell().Element(CellBody).Text(item.Celular);
                        table.Cell().Element(CellBody).Text(item.Llave);
                        table.Cell().Element(CellBody).Text(item.FechaEsperada.ToLocalTime().ToString("dd/MM/yyyy HH:mm"));
                        table.Cell().Element(CellBody).Text(item.DiasVencido == 0 ? "< 1 día" : $"{item.DiasVencido} día(s)");
                    }

                    if (!data.Any())
                        table.Cell().ColumnSpan(7).Element(CellBody).AlignCenter().Text("Sin préstamos vencidos.");
                });

                page.Footer().AlignRight().Text($"Total: {data.Count} préstamo(s) vencido(s)").FontColor(Colors.Grey.Darken1);
            });
        }).GeneratePdf();

        return File(pdf, "application/pdf", $"reporte_vencidos_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
    }

    [HttpGet]
    public async Task<IActionResult> ExportarActividadAmbientePdf(DateTime? fechaDesde, DateTime? fechaHasta, int? idAmbiente)
    {
        if (ValidarFechasInconsistentes(fechaDesde, fechaHasta, out var err))
            return BadRequest(new { error = err });

        var data = await ObtenerActividadAmbienteAsync(fechaDesde, fechaHasta, idAmbiente);

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text("Actividad por Ambiente").SemiBold().FontSize(16);
                    col.Item().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}").FontColor(Colors.Grey.Darken1);
                });

                page.Content().PaddingTop(12).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1.2f);
                        columns.RelativeColumn(2.5f);
                        columns.RelativeColumn(1.3f);
                        columns.RelativeColumn(1.3f);
                        columns.RelativeColumn(1.3f);
                        columns.RelativeColumn(2f);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(CellHeader).Text("Código");
                        header.Cell().Element(CellHeader).Text("Ambiente");
                        header.Cell().Element(CellHeader).Text("Préstamos");
                        header.Cell().Element(CellHeader).Text("Reservas");
                        header.Cell().Element(CellHeader).Text("Total");
                        header.Cell().Element(CellHeader).Text("Prom. Horas Préstamo");
                    });

                    foreach (var item in data)
                    {
                        table.Cell().Element(CellBody).Text(item.Codigo);
                        table.Cell().Element(CellBody).Text(item.Ambiente);
                        table.Cell().Element(CellBody).Text(item.TotalPrestamos.ToString());
                        table.Cell().Element(CellBody).Text(item.TotalReservas.ToString());
                        table.Cell().Element(CellBody).Text(item.TotalActividad.ToString());
                        table.Cell().Element(CellBody).Text(item.PromedioHorasPrestamo > 0 ? $"{item.PromedioHorasPrestamo} h" : "—");
                    }

                    if (!data.Any())
                        table.Cell().ColumnSpan(6).Element(CellBody).AlignCenter().Text("Sin actividad para el periodo seleccionado.");
                });

                page.Footer().AlignRight()
                    .Text($"Total actividad: {data.Sum(x => x.TotalActividad)} | Ambientes: {data.Count}")
                    .FontColor(Colors.Grey.Darken1);
            });
        }).GeneratePdf();

        return File(pdf, "application/pdf", $"reporte_actividad_ambiente_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
    }

    private async Task<List<ReservaReporteItem>> ObtenerReservasAsync(DateTime? fechaDesde, DateTime? fechaHasta, int? idAmbiente)
    {
        var query = _context.Reservas
            .AsNoTracking()
            .Include(r => r.Persona)
            .Include(r => r.Llave)
                .ThenInclude(l => l.Ambiente)
            .AsQueryable();

        if (fechaDesde.HasValue)
        {
            var desde = DateTime.SpecifyKind(fechaDesde.Value.Date, DateTimeKind.Utc);
            query = query.Where(r => r.FechaInicio >= desde);
        }

        if (fechaHasta.HasValue)
        {
            var hasta = DateTime.SpecifyKind(fechaHasta.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            query = query.Where(r => r.FechaInicio <= hasta);
        }

        if (idAmbiente.HasValue)
            query = query.Where(r => r.Llave.IdAmbiente == idAmbiente.Value);

        return await query
            .OrderByDescending(r => r.FechaInicio)
            .Select(r => new ReservaReporteItem
            {
                IdReserva = r.IdReserva,
                Persona = r.Persona.Nombres + " " + r.Persona.Apellidos,
                FechaInicio = r.FechaInicio,
                FechaFin = r.FechaFin,
                Estado = r.Estado,
                Llave = r.Llave.Codigo,
                Ambiente = r.Llave.Ambiente != null ? r.Llave.Ambiente.Nombre : "-"
            })
            .ToListAsync();
    }

    private async Task<List<PrestamoReporteItem>> ObtenerPrestamosAsync(DateTime? fechaDesde, DateTime? fechaHasta, int? idAmbiente, string? estado, string? persona)
    {
        var query = _context.Prestamos
            .AsNoTracking()
            .Include(p => p.Persona)
            .Include(p => p.Llave)
                .ThenInclude(l => l.Ambiente)
            .AsQueryable();

        if (fechaDesde.HasValue)
        {
            var desde = DateTime.SpecifyKind(fechaDesde.Value.Date, DateTimeKind.Utc);
            query = query.Where(p => p.FechaHoraPrestamo >= desde);
        }

        if (fechaHasta.HasValue)
        {
            var hasta = DateTime.SpecifyKind(fechaHasta.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            query = query.Where(p => p.FechaHoraPrestamo <= hasta);
        }

        if (idAmbiente.HasValue)
            query = query.Where(p => p.Llave.IdAmbiente == idAmbiente.Value);

        if (!string.IsNullOrWhiteSpace(estado))
            query = query.Where(p => p.Estado == estado);

        if (!string.IsNullOrWhiteSpace(persona))
        {
            var pLower = persona.Trim().ToLower();
            query = query.Where(p => (p.Persona.Nombres + " " + p.Persona.Apellidos).ToLower().Contains(pLower) || p.Persona.Ci.Contains(persona));
        }

        return await query
            .OrderByDescending(p => p.FechaHoraPrestamo)
            .Select(p => new PrestamoReporteItem
            {
                IdPrestamo = p.IdPrestamo,
                Persona = p.Persona.Nombres + " " + p.Persona.Apellidos,
                FechaHoraPrestamo = p.FechaHoraPrestamo,
                FechaHoraDevolucionReal = p.FechaHoraDevolucionReal,
                Estado = p.Estado,
                Llave = p.Llave.Codigo,
                Ambiente = p.Llave.Ambiente != null ? p.Llave.Ambiente.Nombre : "-"
            })
            .ToListAsync();
    }

    private async Task<List<TopPersonaReporteItem>> ObtenerTopPersonasAsync(DateTime? fechaDesde, DateTime? fechaHasta, int top)
    {
        var prestamos = _context.Prestamos.AsNoTracking().AsQueryable();
        var reservas = _context.Reservas.AsNoTracking().AsQueryable();

        if (fechaDesde.HasValue)
        {
            var desde = DateTime.SpecifyKind(fechaDesde.Value.Date, DateTimeKind.Utc);
            prestamos = prestamos.Where(p => p.FechaHoraPrestamo >= desde);
            reservas = reservas.Where(r => r.FechaInicio >= desde);
        }

        if (fechaHasta.HasValue)
        {
            var hasta = DateTime.SpecifyKind(fechaHasta.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            prestamos = prestamos.Where(p => p.FechaHoraPrestamo <= hasta);
            reservas = reservas.Where(r => r.FechaInicio <= hasta);
        }

        var topPrestamos = await prestamos
            .GroupBy(p => p.IdPersona)
            .Select(g => new { IdPersona = g.Key, Cantidad = g.Count() })
            .ToListAsync();

        var topReservas = await reservas
            .GroupBy(r => r.IdPersona)
            .Select(g => new { IdPersona = g.Key, Cantidad = g.Count() })
            .ToListAsync();

        var personas = await _context.Personas
            .AsNoTracking()
            .Select(p => new { p.IdPersona, Nombre = p.Nombres + " " + p.Apellidos })
            .ToDictionaryAsync(p => p.IdPersona, p => p.Nombre);

        return topPrestamos
            .Select(p => new TopPersonaReporteItem
            {
                IdPersona = p.IdPersona,
                Prestamos = p.Cantidad,
                Reservas = topReservas.FirstOrDefault(r => r.IdPersona == p.IdPersona)?.Cantidad ?? 0
            })
            .Concat(
                topReservas
                    .Where(r => !topPrestamos.Any(p => p.IdPersona == r.IdPersona))
                    .Select(r => new TopPersonaReporteItem
                    {
                        IdPersona = r.IdPersona,
                        Prestamos = 0,
                        Reservas = r.Cantidad
                    })
            )
            .Select(x => new TopPersonaReporteItem
            {
                IdPersona = x.IdPersona,
                Nombre = personas.ContainsKey(x.IdPersona) ? personas[x.IdPersona] : "Persona no encontrada",
                Prestamos = x.Prestamos,
                Reservas = x.Reservas,
                Total = x.Prestamos + x.Reservas
            })
            .OrderByDescending(x => x.Total)
            .ThenBy(x => x.Nombre)
            .Take(top)
            .ToList();
    }

    private async Task<List<InventarioLlaveItem>> ObtenerInventarioLlavesAsync(int? idAmbiente, string? estado)
    {
        var query = _context.Llaves
            .AsNoTracking()
            .Include(l => l.Ambiente)
            .AsQueryable();

        if (idAmbiente.HasValue)
            query = query.Where(l => l.IdAmbiente == idAmbiente.Value);

        if (!string.IsNullOrWhiteSpace(estado))
            query = query.Where(l => l.Estado == estado);
        else
            query = query.Where(l => l.Estado != "I");

        var llaves = await query.OrderBy(l => l.Codigo).ToListAsync();
        var idLlaves = llaves.Select(l => l.IdLlave).ToList();

        var conteoPrestamos = await _context.Prestamos
            .AsNoTracking()
            .Where(p => idLlaves.Contains(p.IdLlave))
            .GroupBy(p => p.IdLlave)
            .Select(g => new { IdLlave = g.Key, Total = g.Count() })
            .ToDictionaryAsync(x => x.IdLlave, x => x.Total);

        var ultimosPrestamos = await _context.Prestamos
            .AsNoTracking()
            .Include(p => p.Persona)
            .Where(p => idLlaves.Contains(p.IdLlave))
            .OrderByDescending(p => p.FechaHoraPrestamo)
            .ToListAsync();

        var ultimosPorLlave = ultimosPrestamos
            .GroupBy(p => p.IdLlave)
            .ToDictionary(g => g.Key, g => g.First());

        return llaves.Select(l =>
        {
            ultimosPorLlave.TryGetValue(l.IdLlave, out var ultimo);
            return new InventarioLlaveItem
            {
                IdLlave = l.IdLlave,
                Codigo = l.Codigo,
                Ambiente = l.Ambiente?.Nombre ?? "-",
                EsMaestra = l.EsMaestra,
                Estado = l.Estado,
                TotalPrestamos = conteoPrestamos.GetValueOrDefault(l.IdLlave, 0),
                UltimaPersona = ultimo != null ? ultimo.Persona.Nombres + " " + ultimo.Persona.Apellidos : "-",
                UltimaFecha = ultimo?.FechaHoraPrestamo
            };
        }).ToList();
    }

    private async Task<List<PrestamoVencidoItem>> ObtenerPrestamosVencidosAsync(DateTime? fechaDesde, DateTime? fechaHasta, int? idAmbiente, string? persona)
    {
        var ahora = DateTime.UtcNow;

        var query = _context.Prestamos
            .AsNoTracking()
            .Include(p => p.Persona)
            .Include(p => p.Llave)
                .ThenInclude(l => l.Ambiente)
            .Where(p => p.Estado == "A"
                     && p.FechaHoraDevolucionEsperada.HasValue
                     && p.FechaHoraDevolucionEsperada.Value < ahora)
            .AsQueryable();

        if (fechaDesde.HasValue)
        {
            var desde = DateTime.SpecifyKind(fechaDesde.Value.Date, DateTimeKind.Utc);
            query = query.Where(p => p.FechaHoraPrestamo >= desde);
        }

        if (fechaHasta.HasValue)
        {
            var hasta = DateTime.SpecifyKind(fechaHasta.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            query = query.Where(p => p.FechaHoraPrestamo <= hasta);
        }

        if (idAmbiente.HasValue)
            query = query.Where(p => p.Llave.IdAmbiente == idAmbiente.Value);

        if (!string.IsNullOrWhiteSpace(persona))
        {
            var pLower = persona.Trim().ToLower();
            query = query.Where(p => (p.Persona.Nombres + " " + p.Persona.Apellidos).ToLower().Contains(pLower) || p.Persona.Ci.Contains(persona));
        }

        var prestamos = await query.OrderBy(p => p.FechaHoraDevolucionEsperada).ToListAsync();

        return prestamos.Select(p => new PrestamoVencidoItem
        {
            IdPrestamo = p.IdPrestamo,
            Persona = p.Persona.Nombres + " " + p.Persona.Apellidos,
            Ci = p.Persona.Ci,
            Celular = p.Persona.Celular ?? "-",
            Llave = p.Llave.Codigo,
            Ambiente = p.Llave.Ambiente?.Nombre ?? "-",
            FechaHoraPrestamo = p.FechaHoraPrestamo,
            FechaEsperada = p.FechaHoraDevolucionEsperada!.Value,
            DiasVencido = (int)(ahora - p.FechaHoraDevolucionEsperada!.Value).TotalDays
        }).ToList();
    }

    private async Task<List<TopLlaveItem>> ObtenerTopLlavesAsync(DateTime? fechaDesde, DateTime? fechaHasta, int? idAmbiente, int top)
    {
        var query = _context.Prestamos
            .AsNoTracking()
            .Join(_context.Llaves, p => p.IdLlave, l => l.IdLlave, (p, l) => new { p, l })
            .Join(_context.Ambientes, x => x.l.IdAmbiente, a => a.IdAmbiente, (x, a) => new { x.p, x.l, a })
            .AsQueryable();

        if (fechaDesde.HasValue)
        {
            var desde = DateTime.SpecifyKind(fechaDesde.Value.Date, DateTimeKind.Utc);
            query = query.Where(x => x.p.FechaHoraPrestamo >= desde);
        }

        if (fechaHasta.HasValue)
        {
            var hasta = DateTime.SpecifyKind(fechaHasta.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            query = query.Where(x => x.p.FechaHoraPrestamo <= hasta);
        }

        if (idAmbiente.HasValue)
            query = query.Where(x => x.l.IdAmbiente == idAmbiente.Value);

        var ranking = await query
            .GroupBy(x => new { x.l.IdLlave, x.l.Codigo, x.a.Nombre })
            .Select(g => new TopLlaveItem
            {
                IdLlave = g.Key.IdLlave,
                Codigo = g.Key.Codigo,
                Ambiente = g.Key.Nombre ?? "-",
                TotalPrestamos = g.Count()
            })
            .OrderByDescending(x => x.TotalPrestamos)
            .Take(top)
            .ToListAsync();

        return ranking;
    }

    private async Task<List<ActividadAmbienteItem>> ObtenerActividadAmbienteAsync(DateTime? fechaDesde, DateTime? fechaHasta, int? idAmbiente)
    {
        var desde = fechaDesde.HasValue ? DateTime.SpecifyKind(fechaDesde.Value.Date, DateTimeKind.Utc) : (DateTime?)null;
        var hasta = fechaHasta.HasValue ? DateTime.SpecifyKind(fechaHasta.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc) : (DateTime?)null;

        var ambientesQuery = _context.Ambientes
            .AsNoTracking()
            .Where(a => a.Estado == "A");

        if (idAmbiente.HasValue)
            ambientesQuery = ambientesQuery.Where(a => a.IdAmbiente == idAmbiente.Value);

        var resultado = await ambientesQuery
            .Select(a => new
            {
                a.IdAmbiente,
                a.Codigo,
                a.Nombre,
                PrestamosCount = a.Llaves.SelectMany(l => l.Prestamos)
                    .Count(p => (!desde.HasValue || p.FechaHoraPrestamo >= desde) && (!hasta.HasValue || p.FechaHoraPrestamo <= hasta)),
                ReservasCount = a.Llaves.SelectMany(l => l.Reservas)
                    .Count(r => (!desde.HasValue || r.FechaInicio >= desde) && (!hasta.HasValue || r.FechaInicio <= hasta)),
                HorasUso = a.Llaves.SelectMany(l => l.Prestamos)
                    .Where(p => p.FechaHoraDevolucionReal.HasValue &&
                               (!desde.HasValue || p.FechaHoraPrestamo >= desde) &&
                               (!hasta.HasValue || p.FechaHoraPrestamo <= hasta))
                    .Select(p => (double)(p.FechaHoraDevolucionReal!.Value - p.FechaHoraPrestamo).TotalHours)
            })
            .Where(x => (x.PrestamosCount + x.ReservasCount) > 0)
            .ToListAsync();

        return resultado
            .Select(x =>
            {
                var horas = x.HorasUso.ToList();
                var promHoras = horas.Count > 0 ? Math.Round(horas.Average(), 1) : 0.0;
                return new ActividadAmbienteItem
                {
                    IdAmbiente = x.IdAmbiente,
                    Codigo = x.Codigo,
                    Ambiente = x.Nombre,
                    TotalPrestamos = x.PrestamosCount,
                    TotalReservas = x.ReservasCount,
                    TotalActividad = x.PrestamosCount + x.ReservasCount,
                    PromedioHorasPrestamo = promHoras
                };
            })
            .OrderByDescending(x => x.TotalActividad)
            .ToList();
    }

    private static IContainer CellHeader(IContainer container)
    {
        return container
            .Background(Colors.Grey.Lighten3)
            .PaddingVertical(6)
            .PaddingHorizontal(4)
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten1)
            .DefaultTextStyle(x => x.SemiBold());
    }

    private static IContainer CellBody(IContainer container)
    {
        return container
            .PaddingVertical(5)
            .PaddingHorizontal(4)
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten3);
    }

    private static string TraducirEstadoReserva(string estado)
    {
        return estado switch
        {
            "P" => "Pendiente",
            "C" => "Confirmada",
            "U" => "Utilizada",
            "X" => "Cancelada",
            _ => estado
        };
    }

    private static string TraducirEstadoPrestamo(string estado)
    {
        return estado switch
        {
            "A" => "Activo",
            "D" => "Devuelto",
            "V" => "Vencido",
            "C" => "Cancelado",
            _ => estado
        };
    }

    private static string TraducirEstadoLlave(string estado)
    {
        return estado switch
        {
            "D" => "Disponible",
            "P" => "Prestada",
            "R" => "Reservada",
            "I" => "Inactiva",
            _ => estado
        };
    }

    /// <summary>Valida que fechaDesde &lt;= fechaHasta cuando ambas están definidas.</summary>
    private static bool ValidarFechasInconsistentes(DateTime? fechaDesde, DateTime? fechaHasta, out string? mensaje)
    {
        mensaje = null;
        if (!fechaDesde.HasValue || !fechaHasta.HasValue) return false;
        if (fechaDesde.Value.Date <= fechaHasta.Value.Date) return false;
        mensaje = "La fecha 'Desde' debe ser anterior o igual a la fecha 'Hasta'.";
        return true;
    }
}
