using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaGestionLlaves.Data;

namespace SistemaGestionLlaves.Controllers;

/// <summary>
/// API JSON para el Dashboard. Endpoints consumidos por Chart.js y la vista Home/Index.
/// </summary>
[ApiController]
[Route("api/dashboard")]
[Produces("application/json")]
[Authorize]
public class DashboardApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public DashboardApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>GET /api/dashboard/kpis</summary>
    [HttpGet("kpis")]
    public async Task<IActionResult> GetKpis()
    {
        var totalLlaves = await _context.Llaves.AsNoTracking().CountAsync();
        var disponibles = await _context.Llaves.AsNoTracking().CountAsync(l => l.Estado == "D");
        var prestadas = await _context.Llaves.AsNoTracking().CountAsync(l => l.Estado == "P");
        var prestamosActivos = await _context.Prestamos.AsNoTracking().CountAsync(p => p.Estado == "A");

        return Ok(new
        {
            totalLlaves,
            llavesDisponibles = disponibles,
            llavesPrestadas = prestadas,
            prestamosActivos
        });
    }

    /// <summary>GET /api/dashboard/prestamos-semana</summary>
    [HttpGet("prestamos-semana")]
    public async Task<IActionResult> GetPrestamosSemana()
    {
        var hace7Dias = DateTime.UtcNow.Date.AddDays(-6);
        var hoy = DateTime.UtcNow.Date.AddDays(1);

        var porDia = await _context.Prestamos
            .AsNoTracking()
            .Where(p => p.FechaHoraPrestamo >= hace7Dias && p.FechaHoraPrestamo < hoy)
            .GroupBy(p => p.FechaHoraPrestamo.Date)
            .Select(g => new { fecha = g.Key, total = g.Count() })
            .ToListAsync();

        var labels = new List<string>();
        var valores = new List<int>();
        for (var d = hace7Dias; d < hoy; d = d.AddDays(1))
        {
            labels.Add(d.ToString("ddd dd/MM"));
            var item = porDia.FirstOrDefault(x => x.fecha == d);
            valores.Add(item?.total ?? 0);
        }

        return Ok(new { labels, valores });
    }

    /// <summary>GET /api/dashboard/estado-llaves</summary>
    [HttpGet("estado-llaves")]
    public async Task<IActionResult> GetEstadoLlaves()
    {
        var grupos = await _context.Llaves
            .AsNoTracking()
            .GroupBy(l => l.Estado)
            .Select(g => new { estado = g.Key, total = g.Count() })
            .ToListAsync();

        var colores = new Dictionary<string, string>
        {
            ["D"] = "#22c55e",
            ["P"] = "#f59e0b",
            ["R"] = "#3b82f6",
            ["I"] = "#94a3b8"
        };
        var nombres = new Dictionary<string, string>
        {
            ["D"] = "Disponibles",
            ["P"] = "Prestadas",
            ["R"] = "Reservadas",
            ["I"] = "Mantenimiento"
        };

        var labels = new List<string>();
        var valores = new List<int>();
        var coloresList = new List<string>();
        foreach (var g in grupos.OrderBy(x => x.estado))
        {
            labels.Add(nombres.GetValueOrDefault(g.estado, g.estado));
            valores.Add(g.total);
            coloresList.Add(colores.GetValueOrDefault(g.estado, "#64748b"));
        }

        return Ok(new { labels, valores, colores = coloresList });
    }

    /// <summary>GET /api/dashboard/ranking-ambientes</summary>
    [HttpGet("ranking-ambientes")]
    public async Task<IActionResult> GetRankingAmbientes([FromQuery] int top = 10)
    {
        var ranking = await _context.Prestamos
            .AsNoTracking()
            .Join(_context.Llaves, p => p.IdLlave, l => l.IdLlave, (p, l) => new { p, l })
            .Join(_context.Ambientes, x => x.l.IdAmbiente, a => a.IdAmbiente, (x, a) => new { x.p, a })
            .GroupBy(x => new { x.a.Codigo, x.a.Nombre })
            .Select(g => new
            {
                g.Key.Codigo,
                g.Key.Nombre,
                total = g.Count()
            })
            .OrderByDescending(x => x.total)
            .Take(top)
            .ToListAsync();

        return Ok(new
        {
            labels = ranking.Select(r => $"{r.Codigo} - {r.Nombre}").ToList(),
            valores = ranking.Select(r => r.total).ToList()
        });
    }

    /// <summary>GET /api/dashboard/actividad-reciente</summary>
    [HttpGet("actividad-reciente")]
    public async Task<IActionResult> GetActividadReciente([FromQuery] int limit = 10)
    {
        var prestamos = await _context.Prestamos
            .AsNoTracking()
            .Include(p => p.Persona)
            .Include(p => p.Llave).ThenInclude(l => l.Ambiente)
            .OrderByDescending(p => p.FechaHoraPrestamo)
            .Take(limit)
            .Select(p => new
            {
                p.IdPrestamo,
                Persona = p.Persona.Nombres + " " + p.Persona.Apellidos,
                Llave = p.Llave.Codigo,
                Ambiente = p.Llave.Ambiente != null ? p.Llave.Ambiente.Nombre : "-",
                p.FechaHoraPrestamo,
                p.Estado
            })
            .ToListAsync();

        var items = prestamos.Select(p => new
        {
            p.IdPrestamo,
            p.Persona,
            p.Llave,
            p.Ambiente,
            Fecha = p.FechaHoraPrestamo,
            EstadoTexto = p.Estado switch { "A" => "Prestado", "D" => "Devuelto", "V" => "Vencido", "C" => "Cancelado", _ => p.Estado }
        }).ToList();

        return Ok(new { data = items });
    }
}
