using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaGestionLlaves.Data;
using SistemaGestionLlaves.Models;
using SistemaGestionLlaves.Services;

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
    private readonly IPrestamoService _prestamoService;

    public PrestamosApiController(
        ApplicationDbContext db,
        ILogger<PrestamosApiController> logger,
        IPrestamoService prestamoService)
    {
        _db = db;
        _logger = logger;
        _prestamoService = prestamoService;
    }

    // ── GET /api/prestamos ───────────────────────────────────
    /// <summary>
    /// Retorna la lista de préstamos con Llave y Persona incluidos.
    /// Parámetro opcional: estado (A/D/V/C).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? buscar, [FromQuery] string? estado, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var query = _db.Prestamos
            .Include(p => p.Llave).ThenInclude(l => l.Ambiente)
            .Include(p => p.Persona)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(estado))
            query = query.Where(p => p.Estado == estado.ToUpper());

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var b = buscar.ToLower();
            query = query.Where(p =>
                p.Persona.Nombres.ToLower().Contains(b) ||
                p.Persona.Apellidos.ToLower().Contains(b) ||
                p.Persona.Ci.ToLower().Contains(b) ||
                p.Llave.Codigo.ToLower().Contains(b) ||
                (p.Llave.Ambiente != null && p.Llave.Ambiente.Nombre.ToLower().Contains(b))
            );
        }

        var prestamos = await query
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
        // Validaciones básicas de entrada
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

        var resultado = await _prestamoService.CrearPrestamoAsync(
            dto.IdLlave,
            dto.IdPersona,
            dto.IdUsuario,
            dto.FechaHoraDevolucionEsperada,
            dto.Observaciones);

        if (!resultado.Success)
            return UnprocessableEntity(new ApiResponse(false, resultado.Message));

        var prestamo = resultado.Prestamo!;

        return CreatedAtAction(nameof(GetById), new { id = prestamo.IdPrestamo },
            new ApiResponse(true, "Préstamo registrado correctamente.",
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
