using SistemaGestionLlaves.Models;

namespace SistemaGestionLlaves.Services;

public interface IPrestamoService
{
    /// <summary>
    /// Registra un nuevo préstamo de llave de forma atómica.
    /// </summary>
    Task<PrestamoResult> CrearPrestamoAsync(
        int idLlave, 
        int idPersona, 
        int idUsuario, 
        DateTime? fechaDevolucionEsperada, 
        string? observaciones);
}

public record PrestamoResult(bool Success, string Message, Prestamo? Prestamo = null);
