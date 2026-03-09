using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SistemaGestionLlaves.Data;
using SistemaGestionLlaves.Models.DTOs;

namespace SistemaGestionLlaves.Services;

public class ReportesService : IReportesService
{
    private readonly ApplicationDbContext _context;

    public ReportesService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<PrestamoReporteDto>> GetPrestamosReportAsync(DateTime? desde, DateTime? hasta, int? idAmbiente, string? estado, string? persona)
    {
        var query = _context.Prestamos
            .AsNoTracking()
            .Include(p => p.Persona)
            .Include(p => p.Llave)
                .ThenInclude(l => l.Ambiente)
            .AsQueryable();

        if (desde.HasValue)
        {
            var d = DateTime.SpecifyKind(desde.Value.Date, DateTimeKind.Utc);
            query = query.Where(p => p.FechaHoraPrestamo >= d);
        }

        if (hasta.HasValue)
        {
            var h = DateTime.SpecifyKind(hasta.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            query = query.Where(p => p.FechaHoraPrestamo <= h);
        }

        if (idAmbiente.HasValue)
            query = query.Where(p => p.Llave.IdAmbiente == idAmbiente.Value);

        if (!string.IsNullOrWhiteSpace(estado))
            query = query.Where(p => p.Estado == estado);

        if (!string.IsNullOrWhiteSpace(persona))
        {
            var pLower = persona.ToLower();
            query = query.Where(p => (p.Persona.Nombres + " " + p.Persona.Apellidos).ToLower().Contains(pLower) || 
                                     p.Persona.Ci.Contains(persona));
        }

        var result = await query
            .OrderByDescending(p => p.FechaHoraPrestamo)
            .ToListAsync();

        return result.Select(p => new PrestamoReporteDto
        {
            IdPrestamo = p.IdPrestamo,
            Persona = p.Persona.Nombres + " " + p.Persona.Apellidos,
            Llave = p.Llave.Codigo,
            Ambiente = p.Llave.Ambiente != null ? p.Llave.Ambiente.Nombre : "-",
            FechaHoraPrestamo = p.FechaHoraPrestamo,
            FechaHoraDevolucionReal = p.FechaHoraDevolucionReal,
            Estado = p.Estado switch
            {
                "A" => "Activo",
                "D" => "Devuelto",
                "V" => "Vencido",
                "C" => "Cancelado",
                _ => p.Estado
            }
        }).ToList();
    }

    public async Task<List<PrestamoVencidoDto>> GetPrestamosVencidosReportAsync(int? idAmbiente)
    {
        var ahora = DateTime.UtcNow;

        var query = _context.Prestamos
            .AsNoTracking()
            .Include(p => p.Persona)
            .Include(p => p.Llave)
                .ThenInclude(l => l.Ambiente)
            .Where(p => p.Estado == "A" && p.FechaHoraDevolucionEsperada.HasValue && p.FechaHoraDevolucionEsperada.Value < ahora)
            .AsQueryable();

        if (idAmbiente.HasValue)
            query = query.Where(p => p.Llave.IdAmbiente == idAmbiente.Value);

        var data = await query.OrderBy(p => p.FechaHoraDevolucionEsperada).ToListAsync();

        return data.Select(p => new PrestamoVencidoDto
        {
            IdPrestamo = p.IdPrestamo,
            Persona = p.Persona.Nombres + " " + p.Persona.Apellidos,
            Ci = p.Persona.Ci,
            Celular = p.Persona.Celular ?? "-",
            Llave = p.Llave.Codigo,
            Ambiente = p.Llave.Ambiente?.Nombre ?? "-",
            FechaHoraPrestamo = p.FechaHoraPrestamo,
            FechaEsperada = p.FechaHoraDevolucionEsperada!.Value,
            DiasRetraso = (int)(ahora - p.FechaHoraDevolucionEsperada.Value).TotalDays
        }).ToList();
    }

    public async Task<List<TopLlaveDto>> GetTopLlavesReportAsync(int top)
    {
        return await _context.Llaves
            .AsNoTracking()
            .Include(l => l.Ambiente)
            .OrderByDescending(l => l.Prestamos.Count)
            .Take(top)
            .Select(l => new TopLlaveDto
            {
                IdLlave = l.IdLlave,
                Codigo = l.Codigo,
                Ambiente = l.Ambiente != null ? l.Ambiente.Nombre : "-",
                TotalPrestamos = l.Prestamos.Count
            })
            .ToListAsync();
    }

    public async Task<List<ActividadAmbienteDto>> GetActividadAmbientesReportAsync(DateTime? desde, DateTime? hasta)
    {
        var d = desde.HasValue ? DateTime.SpecifyKind(desde.Value.Date, DateTimeKind.Utc) : (DateTime?)null;
        var h = hasta.HasValue ? DateTime.SpecifyKind(hasta.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc) : (DateTime?)null;

        return await _context.Ambientes
            .AsNoTracking()
            .Where(a => a.Estado == "A")
            .Select(a => new
            {
                a.IdAmbiente,
                a.Codigo,
                a.Nombre,
                PrestamosCount = a.Llaves.SelectMany(l => l.Prestamos)
                    .Count(p => (!d.HasValue || p.FechaHoraPrestamo >= d) && (!h.HasValue || p.FechaHoraPrestamo <= h)),
                ReservasCount = a.Llaves.SelectMany(l => l.Reservas)
                    .Count(r => (!d.HasValue || r.FechaInicio >= d) && (!h.HasValue || r.FechaInicio <= h)),
                PrestamosConDevolucion = a.Llaves.SelectMany(l => l.Prestamos)
                    .Where(p => p.FechaHoraDevolucionReal.HasValue && 
                               (!d.HasValue || p.FechaHoraPrestamo >= d) && 
                               (!h.HasValue || p.FechaHoraPrestamo <= h))
            })
            .Where(x => (x.PrestamosCount + x.ReservasCount) > 0)
            .Select(x => new ActividadAmbienteDto
            {
                IdAmbiente = x.IdAmbiente,
                Codigo = x.Codigo,
                Ambiente = x.Nombre,
                TotalPrestamos = x.PrestamosCount,
                TotalReservas = x.ReservasCount,
                ActividadTotal = x.PrestamosCount + x.ReservasCount,
                PromedioHorasUso = x.PrestamosConDevolucion.Any() 
                    ? Math.Round(x.PrestamosConDevolucion.Average(p => (p.FechaHoraDevolucionReal!.Value - p.FechaHoraPrestamo).TotalHours), 1)
                    : 0
            })
            .OrderByDescending(x => x.ActividadTotal)
            .ToListAsync();
    }
}
