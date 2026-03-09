using System;

namespace SistemaGestionLlaves.Models.DTOs;

public class PrestamoReporteDto
{
    public int IdPrestamo { get; set; }
    public string Persona { get; set; } = string.Empty;
    public string Llave { get; set; } = string.Empty;
    public string Ambiente { get; set; } = "-";
    public DateTime FechaHoraPrestamo { get; set; }
    public DateTime? FechaHoraDevolucionReal { get; set; }
    public string Estado { get; set; } = string.Empty;
}

public class PrestamoVencidoDto
{
    public int IdPrestamo { get; set; }
    public string Persona { get; set; } = string.Empty;
    public string Ci { get; set; } = string.Empty;
    public string Celular { get; set; } = "-";
    public string Llave { get; set; } = string.Empty;
    public string Ambiente { get; set; } = "-";
    public DateTime FechaHoraPrestamo { get; set; }
    public DateTime FechaEsperada { get; set; }
    public int DiasRetraso { get; set; }
}

public class TopLlaveDto
{
    public int IdLlave { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Ambiente { get; set; } = "-";
    public int TotalPrestamos { get; set; }
}

public class ActividadAmbienteDto
{
    public int IdAmbiente { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Ambiente { get; set; } = string.Empty;
    public int TotalPrestamos { get; set; }
    public int TotalReservas { get; set; }
    public int ActividadTotal { get; set; }
    public double PromedioHorasUso { get; set; }
}
