using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SistemaGestionLlaves.Models.DTOs;

namespace SistemaGestionLlaves.Services;

public interface IReportesService
{
    Task<List<PrestamoReporteDto>> GetPrestamosReportAsync(DateTime? desde, DateTime? hasta, int? idAmbiente, string? estado, string? persona);
    Task<List<PrestamoVencidoDto>> GetPrestamosVencidosReportAsync(int? idAmbiente);
    Task<List<TopLlaveDto>> GetTopLlavesReportAsync(int top);
    Task<List<ActividadAmbienteDto>> GetActividadAmbientesReportAsync(DateTime? desde, DateTime? hasta);
}
