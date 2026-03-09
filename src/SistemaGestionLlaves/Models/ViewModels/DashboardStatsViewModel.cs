namespace SistemaGestionLlaves.Models.ViewModels;

public class DashboardStatsViewModel
{
    // KPIs
    public int LlavesDisponibles { get; set; }
    public int LlavesPrestadas { get; set; }
    public int PrestamosActivos { get; set; }
    public int ReservasHoy { get; set; }

    // Analytics
    public List<ChartDataPoint> Prestamos7Dias { get; set; } = new();
    public List<ChartDataPoint> DistribucionLlaves { get; set; } = new();
    public List<ChartDataPoint> TopAmbientes { get; set; } = new();
    public List<RecentLoanViewModel> UltimosPrestamos { get; set; } = new();
}

public record ChartDataPoint(string Label, int Value);

public class RecentLoanViewModel
{
    public int IdPrestamo { get; set; }
    public string Persona { get; set; } = "";
    public string Llave { get; set; } = "";
    public DateTime Fecha { get; set; }
    public string Estado { get; set; } = "";
}
