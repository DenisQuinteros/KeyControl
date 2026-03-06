using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaGestionLlaves.Data;
using SistemaGestionLlaves.Models;

namespace SistemaGestionLlaves.Controllers;

// ─────────────────────────────────────────────────────────────
//  DTOs de entrada
// ─────────────────────────────────────────────────────────────

/// <summary>Cuerpo para crear un préstamo.</summary>
public record PrestamoRequest(
    int IdLlave,
    int IdPersona,
    int IdUsuario,
    DateTime? FechaHoraDevolucionEsperada,
    string? Observaciones
);

// ─────────────────────────────────────────────────────────────
//  Controlador
// ─────────────────────────────────────────────────────────────

/// <summary>
/// API REST para la gestión de Préstamos.
/// Base URL: /api/prestamos
/// </summary>
[ApiController]
[Route("api/prestamos")]
[Produces("application/json")]
[Authorize(Roles = "Administrador,Operador")]
public class PrestamosApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<PrestamosApiController> _logger;

    public PrestamosApiController(ApplicationDbContext db, ILogger<PrestamosApiController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ── GET /api/prestamos ───────────────────────────────────
    /// <summary>
    /// Retorna la lista de préstamos con Llave y Persona incluidos.
    /// Parámetro opcional: estado (A/D/V/C).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? estado, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var query = _db.Prestamos
            .Include(p => p.Llave)
            .Include(p => p.Persona)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(estado))
            query = query.Where(p => p.Estado == estado.ToUpper());

        var prestamos = await query
            .AsNoTracking()
            .OrderByDescending(p => p.FechaHoraPrestamo)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.IdPrestamo,
                p.IdLlave,
                p.IdPersona,
                p.IdUsuario,
                p.FechaHoraPrestamo,
                p.FechaHoraDevolucionEsperada,
                p.FechaHoraDevolucionReal,
                p.Estado,
                p.Observaciones,
                Llave = new
                {
                    p.Llave.IdLlave,
                    p.Llave.Codigo,
                    p.Llave.Estado
                },
                Persona = new
                {
                    p.Persona.IdPersona,
                    p.Persona.Ci,
                    Nombre = $"{p.Persona.Nombres} {p.Persona.Apellidos}"
                }
            })
            .ToListAsync();

        return Ok(prestamos);
    }

    // ── GET /api/prestamos/{id} ──────────────────────────────
    /// <summary>
    /// Retorna el detalle de un préstamo por su Id.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var prestamo = await _db.Prestamos
            .Include(p => p.Llave).ThenInclude(l => l.Ambiente)
            .Include(p => p.Persona)
            .Include(p => p.Usuario).ThenInclude(u => u.Persona)
            .FirstOrDefaultAsync(p => p.IdPrestamo == id);

        if (prestamo == null)
            return NotFound(new ApiResponse(false, $"Préstamo con Id={id} no encontrado."));

        var resultado = new
        {
            prestamo.IdPrestamo,
            prestamo.IdLlave,
            prestamo.IdPersona,
            prestamo.IdUsuario,
            prestamo.FechaHoraPrestamo,
            prestamo.FechaHoraDevolucionEsperada,
            prestamo.FechaHoraDevolucionReal,
            prestamo.Estado,
            prestamo.Observaciones,
            Llave = new
            {
                prestamo.Llave.IdLlave,
                prestamo.Llave.Codigo,
                prestamo.Llave.Estado,
                Ambiente = prestamo.Llave.Ambiente == null ? null : new
                {
                    prestamo.Llave.Ambiente.IdAmbiente,
                    prestamo.Llave.Ambiente.Nombre
                }
            },
            Persona = new
            {
                prestamo.Persona.IdPersona,
                prestamo.Persona.Ci,
                Nombre = $"{prestamo.Persona.Nombres} {prestamo.Persona.Apellidos}"
            },
            Operador = new
            {
                prestamo.Usuario.IdUsuario,
                Nombre = $"{prestamo.Usuario.Persona.Nombres} {prestamo.Usuario.Persona.Apellidos}"
            }
        };

        return Ok(resultado);
    }

    // ── POST /api/prestamos ──────────────────────────────────
    /// <summary>
    /// Crea un nuevo préstamo.
    /// Reglas: la llave debe estar Disponible (D) y no tener préstamo Activo (A).
    /// Cambia Llave.Estado a "P" y registra el préstamo con Estado="A".
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] PrestamoRequest dto)
    {
        // Validaciones básicas
        if (dto.IdLlave <= 0)
            return BadRequest(new ApiResponse(false, "El Id de llave es obligatorio."));

        if (dto.IdPersona <= 0)
            return BadRequest(new ApiResponse(false, "El Id de persona es obligatorio."));

        if (dto.IdUsuario <= 0)
            return BadRequest(new ApiResponse(false, "El Id de usuario (operador) es obligatorio."));

        if (dto.FechaHoraDevolucionEsperada.HasValue &&
            dto.FechaHoraDevolucionEsperada.Value <= DateTime.UtcNow)
            return BadRequest(new ApiResponse(false,
                "La fecha de devolución esperada debe ser posterior a la fecha actual."));

        // Verificar que la llave existe
        var llave = await _db.Llaves.FindAsync(dto.IdLlave);
        if (llave == null)
            return NotFound(new ApiResponse(false, $"Llave con Id={dto.IdLlave} no encontrada."));

        // Regla 1: la llave debe estar Disponible
        if (llave.Estado != "D")
            return UnprocessableEntity(new ApiResponse(false,
                $"La llave '{llave.Codigo}' no está disponible (estado actual: {llave.Estado})."));

        // Regla 2: no debe existir préstamo activo para esa llave
        bool tienePrestamoActivo = await _db.Prestamos
            .AnyAsync(p => p.IdLlave == dto.IdLlave && p.Estado == "A");
        if (tienePrestamoActivo)
            return UnprocessableEntity(new ApiResponse(false,
                $"La llave '{llave.Codigo}' ya tiene un préstamo activo."));

        // Verificar que la persona existe
        var persona = await _db.Personas.FindAsync(dto.IdPersona);
        if (persona == null)
            return NotFound(new ApiResponse(false, $"Persona con Id={dto.IdPersona} no encontrada."));

        // Verificar que el usuario (operador) existe
        bool usuarioExiste = await _db.Usuarios.AnyAsync(u => u.IdUsuario == dto.IdUsuario);
        if (!usuarioExiste)
            return NotFound(new ApiResponse(false, $"Usuario con Id={dto.IdUsuario} no encontrado."));

        // Crear préstamo
        var prestamo = new Prestamo
        {
            IdLlave                    = dto.IdLlave,
            IdPersona                  = dto.IdPersona,
            IdUsuario                  = dto.IdUsuario,
            FechaHoraPrestamo          = DateTime.UtcNow,
            FechaHoraDevolucionEsperada = dto.FechaHoraDevolucionEsperada,
            Estado                     = "A",
            Observaciones              = dto.Observaciones?.Trim()
        };

        // Cambiar estado de la llave
        llave.Estado = "P";

        _db.Prestamos.Add(prestamo);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Préstamo creado: Id={Id}, Llave={Llave}, Persona={Persona}",
            prestamo.IdPrestamo, llave.Codigo, dto.IdPersona);

        return CreatedAtAction(nameof(GetById), new { id = prestamo.IdPrestamo },
            new ApiResponse(true, "Préstamo registrado exitosamente.",
                new { prestamo.IdPrestamo, prestamo.Estado }));
    }

    // ── PATCH /api/prestamos/{id}/devolver ───────────────────
    /// <summary>
    /// Registra la devolución de un préstamo activo.
    /// Solo se permite si Estado == "A".
    /// Cambia Llave.Estado a "D".
    /// </summary>
    [HttpPatch("{id:int}/devolver")]
    public async Task<IActionResult> Devolver(int id)
    {
        var prestamo = await _db.Prestamos
            .Include(p => p.Llave)
            .FirstOrDefaultAsync(p => p.IdPrestamo == id);

        if (prestamo == null)
            return NotFound(new ApiResponse(false, $"Préstamo con Id={id} no encontrado."));

        // No permitir si ya está devuelto o cancelado
        if (prestamo.Estado == "D")
            return UnprocessableEntity(new ApiResponse(false,
                "El préstamo ya fue devuelto."));

        if (prestamo.Estado == "C")
            return UnprocessableEntity(new ApiResponse(false,
                "No se puede devolver un préstamo cancelado."));

        // Solo si está activo
        if (prestamo.Estado != "A")
            return UnprocessableEntity(new ApiResponse(false,
                $"El préstamo no está en estado Activo (estado actual: {prestamo.Estado})."));

        prestamo.FechaHoraDevolucionReal = DateTime.UtcNow;
        prestamo.Estado                  = "D";
        prestamo.Llave.Estado            = "D";

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Préstamo devuelto: Id={Id}, Llave={Llave}",
            prestamo.IdPrestamo, prestamo.Llave.Codigo);

        return Ok(new ApiResponse(true, "Devolución registrada exitosamente.",
            new { prestamo.IdPrestamo, prestamo.Estado, prestamo.FechaHoraDevolucionReal }));
    }
}
