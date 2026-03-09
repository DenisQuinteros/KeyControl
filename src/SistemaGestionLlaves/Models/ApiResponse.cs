namespace SistemaGestionLlaves.Models;

/// <summary>
/// Respuesta estándar para todas las APIs del sistema.
/// </summary>
/// <param name="Exito">Indica si la operación fue exitosa.</param>
/// <param name="Mensaje">Mensaje descriptivo del resultado.</param>
/// <param name="Data">Datos opcionales de la respuesta.</param>
public record ApiResponse(bool Exito, string Mensaje, object? Data = null);
