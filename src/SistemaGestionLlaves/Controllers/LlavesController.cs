using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaGestionLlaves.Data;
using SistemaGestionLlaves.Models;

namespace SistemaGestionLlaves.Controllers;

// ─────────────────────────────────────────────────────────────
//  DTOs de entrada / salida
// ─────────────────────────────────────────────────────────────

/// <summary>Cuerpo para crear o actualizar una llave.</summary>
public record LlaveRequest(
    string Codigo,
    int NumCopias,
    int IdAmbiente,
    bool EsMaestra,
    string? Observaciones
);

/// <summary>Respuesta estándar con id, mensaje y datos opcionales.</summary>
public record ApiResponse(bool Exito, string Mensaje, object? Data = null);

// ─────────────────────────────────────────────────────────────
//  Controlador
// ─────────────────────────────────────────────────────────────

/// <summary>
/// API REST para la gestión de Llaves.
/// Base URL: /api/llaves
/// </summary>
[ApiController]
[Route("api/llaves")]
[Produces("application/json")]
public class LlavesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<LlavesController> _logger;

    public LlavesController(ApplicationDbContext db, ILogger<LlavesController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ── GET /api/llaves ──────────────────────────────────────
    /// <summary>
    /// Retorna la lista de llaves.
    /// Parámetros opcionales de filtro: estado (D/P/R/I), busqueda (código o nombre de ambiente).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? estado,
        [FromQuery] string? busqueda,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = _db.Llaves
            .Include(l => l.Ambiente)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(estado))
            query = query.Where(l => l.Estado == estado.ToUpper());

        if (!string.IsNullOrWhiteSpace(busqueda))
            query = query.Where(l =>
                l.Codigo.Contains(busqueda) ||
                l.Ambiente.Nombre.Contains(busqueda));

        var llaves = await query
            .AsNoTracking()
            .OrderBy(l => l.Ambiente.Nombre)
            .ThenBy(l => l.Codigo)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new
            {
                l.IdLlave,
                l.Codigo,
                l.NumCopias,
                l.EsMaestra,
                l.Estado,
                l.Observaciones,
                Ambiente = new
                {
                    l.Ambiente.IdAmbiente,
                    l.Ambiente.Codigo,
                    l.Ambiente.Nombre
                }
            })
            .ToListAsync();

        return Ok(llaves);
    }

    // ── GET /api/llaves/{id} ─────────────────────────────────
    /// <summary>
    /// Retorna el detalle completo de una llave, incluyendo sus préstamos
    /// activos, reservas pendientes y personas autorizadas.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var llave = await _db.Llaves
            .Include(l => l.Ambiente).ThenInclude(a => a.TipoAmbiente)
            .Include(l => l.Prestamos
                .OrderByDescending(p => p.FechaHoraPrestamo)
                .Take(10))
                .ThenInclude(p => p.Persona)
            .Include(l => l.Reservas
                .OrderByDescending(r => r.FechaInicio)
                .Take(10))
                .ThenInclude(r => r.Persona)
            .Include(l => l.PersonasAutorizadas)
                .ThenInclude(pa => pa.Persona)
            .FirstOrDefaultAsync(l => l.IdLlave == id);

        if (llave == null)
            return NotFound(new ApiResponse(false, $"Llave con Id={id} no encontrada."));

        var resultado = new
        {
            llave.IdLlave,
            llave.Codigo,
            llave.NumCopias,
            llave.EsMaestra,
            llave.Estado,
            llave.Observaciones,
            Ambiente = new
            {
                llave.Ambiente.IdAmbiente,
                llave.Ambiente.Codigo,
                llave.Ambiente.Nombre,
                Tipo = llave.Ambiente.TipoAmbiente.NombreTipo
            },
            Prestamos = llave.Prestamos.Select(p => new
            {
                p.IdPrestamo,
                p.FechaHoraPrestamo,
                p.FechaHoraDevolucionEsperada,
                p.FechaHoraDevolucionReal,
                p.Estado,
                p.Observaciones,
                Persona = $"{p.Persona.Nombres} {p.Persona.Apellidos}"
            }),
            Reservas = llave.Reservas.Select(r => new
            {
                r.IdReserva,
                r.FechaInicio,
                r.FechaFin,
                r.Estado,
                Persona = $"{r.Persona.Nombres} {r.Persona.Apellidos}"
            }),
            PersonasAutorizadas = llave.PersonasAutorizadas.Select(pa => new
            {
                pa.Persona.IdPersona,
                pa.Persona.Ci,
                Nombre = $"{pa.Persona.Nombres} {pa.Persona.Apellidos}"
            })
        };

        return Ok(resultado);
    }

    // ── POST /api/llaves ─────────────────────────────────────
    /// <summary>
    /// Crea una nueva llave. El estado inicial siempre es "D" (Disponible).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] LlaveRequest dto)
    {
        // Validar campo Codigo
        if (string.IsNullOrWhiteSpace(dto.Codigo) || dto.Codigo.Length > 30)
            return BadRequest(new ApiResponse(false, "El código es obligatorio y no puede superar 30 caracteres."));

        if (dto.NumCopias < 1)
            return BadRequest(new ApiResponse(false, "El número de copias debe ser al menos 1."));

        // Verificar código único
        if (await _db.Llaves.AnyAsync(l => l.Codigo == dto.Codigo.ToUpper()))
            return Conflict(new ApiResponse(false, $"Ya existe una llave con el código '{dto.Codigo.ToUpper()}'."));

        // Verificar que el ambiente existe y está activo
        var ambiente = await _db.Ambientes.FindAsync(dto.IdAmbiente);
        if (ambiente == null || ambiente.Estado != "A")
            return BadRequest(new ApiResponse(false, $"El ambiente con Id={dto.IdAmbiente} no existe o no está activo."));

        var llave = new Llave
        {
            Codigo        = dto.Codigo.Trim().ToUpper(),
            NumCopias     = dto.NumCopias,
            IdAmbiente    = dto.IdAmbiente,
            EsMaestra     = dto.EsMaestra,
            Observaciones = dto.Observaciones?.Trim(),
            Estado        = "D"
        };

        _db.Llaves.Add(llave);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Llave creada: {Codigo} (Id={Id})", llave.Codigo, llave.IdLlave);
        return CreatedAtAction(nameof(GetById), new { id = llave.IdLlave },
            new ApiResponse(true, "Llave creada exitosamente.", new { llave.IdLlave, llave.Codigo }));
    }

    // ── PUT /api/llaves/{id} ─────────────────────────────────
    /// <summary>
    /// Actualiza los datos editables de una llave (no cambia el estado).
    /// Para cambiar el estado use PATCH /api/llaves/{id}/estado.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Editar(int id, [FromBody] LlaveRequest dto)
    {
        var llave = await _db.Llaves.FindAsync(id);
        if (llave == null)
            return NotFound(new ApiResponse(false, $"Llave con Id={id} no encontrada."));

        if (string.IsNullOrWhiteSpace(dto.Codigo) || dto.Codigo.Length > 30)
            return BadRequest(new ApiResponse(false, "El código es obligatorio y no puede superar 30 caracteres."));

        if (dto.NumCopias < 1)
            return BadRequest(new ApiResponse(false, "El número de copias debe ser al menos 1."));

        // Verificar código único (excluyendo la propia llave)
        if (await _db.Llaves.AnyAsync(l => l.Codigo == dto.Codigo.ToUpper() && l.IdLlave != id))
            return Conflict(new ApiResponse(false, $"Ya existe otra llave con el código '{dto.Codigo.ToUpper()}'."));

        // Verificar ambiente válido
        var ambiente = await _db.Ambientes.FindAsync(dto.IdAmbiente);
        if (ambiente == null || ambiente.Estado != "A")
            return BadRequest(new ApiResponse(false, $"El ambiente con Id={dto.IdAmbiente} no existe o no está activo."));

        llave.Codigo        = dto.Codigo.Trim().ToUpper();
        llave.NumCopias     = dto.NumCopias;
        llave.IdAmbiente    = dto.IdAmbiente;
        llave.EsMaestra     = dto.EsMaestra;
        llave.Observaciones = dto.Observaciones?.Trim();

        await _db.SaveChangesAsync();

        _logger.LogInformation("Llave actualizada: {Codigo} (Id={Id})", llave.Codigo, llave.IdLlave);
        return Ok(new ApiResponse(true, "Llave actualizada exitosamente.", new { llave.IdLlave, llave.Codigo }));
    }

    // ── PATCH /api/llaves/{id}/estado ────────────────────────
    /// <summary>
    /// Cambia el estado de una llave.
    /// - Si está Inactiva → la reactiva como Disponible (D).
    /// - Si está Disponible/Reservada → la inactiva (I), validando que no tenga
    ///   préstamos activos ni reservas pendientes/confirmadas.
    /// - No se puede cambiar el estado de una llave Prestada directamente.
    /// </summary>
    [HttpPatch("{id:int}/estado")]
    public async Task<IActionResult> CambiarEstado(int id)
    {
        var llave = await _db.Llaves.FindAsync(id);
        if (llave == null)
            return NotFound(new ApiResponse(false, $"Llave con Id={id} no encontrada."));

        if (llave.Estado == "I")
        {
            // Reactivar
            llave.Estado = "D";
            await _db.SaveChangesAsync();
            _logger.LogInformation("Llave reactivada: {Codigo} (Id={Id})", llave.Codigo, id);
            return Ok(new ApiResponse(true, $"Llave '{llave.Codigo}' reactivada como Disponible."));
        }

        if (llave.Estado == "P")
            return UnprocessableEntity(new ApiResponse(false,
                "No se puede modificar el estado de una llave que está actualmente prestada."));

        // Inactivar — validar reglas de negocio
        bool tienePrestamo = await _db.Prestamos.AnyAsync(p => p.IdLlave == id && p.Estado == "A");
        if (tienePrestamo)
            return UnprocessableEntity(new ApiResponse(false,
                "No se puede inactivar: la llave tiene un préstamo activo."));

        bool tieneReserva = await _db.Reservas
            .AnyAsync(r => r.IdLlave == id && (r.Estado == "P" || r.Estado == "C"));
        if (tieneReserva)
            return UnprocessableEntity(new ApiResponse(false,
                "No se puede inactivar: la llave tiene reservas pendientes o confirmadas."));

        llave.Estado = "I";
        await _db.SaveChangesAsync();
        _logger.LogInformation("Llave inactivada: {Codigo} (Id={Id})", llave.Codigo, id);
        return Ok(new ApiResponse(true, $"Llave '{llave.Codigo}' inactivada correctamente."));
    }
}
