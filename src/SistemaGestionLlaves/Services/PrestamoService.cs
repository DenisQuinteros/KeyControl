using Microsoft.EntityFrameworkCore;
using SistemaGestionLlaves.Data;
using SistemaGestionLlaves.Models;

namespace SistemaGestionLlaves.Services;

public class PrestamoService : IPrestamoService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PrestamoService> _logger;

    public PrestamoService(ApplicationDbContext context, ILogger<PrestamoService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PrestamoResult> CrearPrestamoAsync(
        int idLlave, 
        int idPersona, 
        int idUsuario, 
        DateTime? fechaDevolucionEsperada, 
        string? observaciones)
    {
        // 1. Validaciones previas
        var llave = await _context.Llaves.FindAsync(idLlave);
        if (llave == null)
            return new PrestamoResult(false, "La llave no existe.");

        if (llave.Estado != "D")
            return new PrestamoResult(false, $"La llave no está disponible (Estado actual: {llave.Estado}).");

        // Verificar si la persona existe y está activa
        var persona = await _context.Personas.FindAsync(idPersona);
        if (persona == null || persona.Estado != "A")
            return new PrestamoResult(false, "La persona no existe o está inactiva.");

        // 2. Nueva validación: ¿La llave está ya reservada para ahora?
        var ahora = DateTime.UtcNow;
        bool tieneReservaActiva = await _context.Reservas.AnyAsync(r => 
            r.IdLlave == idLlave && 
            r.Estado == "C" && // Confirmada
            ahora >= r.FechaInicio && ahora <= r.FechaFin);

        if (tieneReservaActiva)
            return new PrestamoResult(false, "La llave tiene una reserva confirmada para este horario.");

        // 3. Ejecución transaccional
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Crear el registro de préstamo
            var prestamo = new Prestamo
            {
                IdLlave = idLlave,
                IdPersona = idPersona,
                IdUsuario = idUsuario,
                FechaHoraPrestamo = DateTime.UtcNow,
                FechaHoraDevolucionEsperada = fechaDevolucionEsperada,
                Estado = "A", // Activo
                Observaciones = observaciones?.Trim()
            };

            // Cambiar estado de la llave a "P" (Prestado)
            llave.Estado = "P";

            _context.Prestamos.Add(prestamo);
            _context.Update(llave);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Préstamo ID {Id} creado exitosamente para llave {Codigo}", prestamo.IdPrestamo, llave.Codigo);

            return new PrestamoResult(true, "Préstamo registrado correctamente.", prestamo);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error al crear préstamo para llave {IdLlave}", idLlave);
            return new PrestamoResult(false, "Error interno al procesar el préstamo.");
        }
    }
}
