using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaGestionLlaves.Data;
using SistemaGestionLlaves.Models;

namespace SistemaGestionLlaves.Controllers
{
    [Route("api/reservas")]
    [ApiController]
    [Authorize(Roles = "Administrador,Operador")]
    public class ApiReservasController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ApiReservasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // DTO para recibir solo los campos que envía el formulario
        public class ReservaDto
        {
            public int IdLlave { get; set; }
            public int IdPersona { get; set; }
            public int IdUsuario { get; set; }
            public DateTime FechaInicio { get; set; }
            public DateTime FechaFin { get; set; }
        }

        // 🔹 LISTAR
        [HttpGet]
        public async Task<IActionResult> Get(string? estado, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var query = _context.Reservas
                .Include(r => r.Llave)
                .Include(r => r.Persona)
                .AsQueryable();

            if (!string.IsNullOrEmpty(estado))
                query = query.Where(r => r.Estado == estado);

            var reservas = await query
                .AsNoTracking()
                .OrderByDescending(r => r.IdReserva)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new
                {
                    r.IdReserva,
                    r.IdLlave,
                    r.IdPersona,
                    r.IdUsuario,
                    r.FechaInicio,
                    r.FechaFin,
                    r.Estado,
                    Llave = r.Llave != null ? r.Llave.Codigo : null,
                    Persona = r.Persona != null ? r.Persona.NombreCompleto : null
                })
                .ToListAsync();

            return Ok(reservas);
        }

        // 🔹 CREAR RESERVA
        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] ReservaDto dto)
        {
            if (dto.IdLlave == 0 || dto.IdPersona == 0)
                return BadRequest(new { exito = false, mensaje = "Seleccione una llave y una persona." });

            // Normalizar fechas a UTC antes de cualquier operación con la BD
            var fechaInicio = DateTime.SpecifyKind(dto.FechaInicio, DateTimeKind.Utc);
            var fechaFin    = DateTime.SpecifyKind(dto.FechaFin,    DateTimeKind.Utc);

            if (fechaFin <= fechaInicio)
                return BadRequest(new { exito = false, mensaje = "La fecha fin debe ser mayor a la fecha inicio." });

            // VALIDACIÓN: NO SOLAPAMIENTO
            bool existeConflicto = await _context.Reservas.AnyAsync(r =>
                r.IdLlave == dto.IdLlave &&
                r.Estado != "X" &&
                fechaInicio < r.FechaFin &&
                fechaFin > r.FechaInicio
            );

            if (existeConflicto)
                return BadRequest(new { exito = false, mensaje = "La llave ya está reservada en ese rango de fechas." });

            var model = new Reserva
            {
                IdLlave = dto.IdLlave,
                IdPersona = dto.IdPersona,
                IdUsuario = dto.IdUsuario > 0 ? dto.IdUsuario : 1,
                FechaInicio = fechaInicio,
                FechaFin    = fechaFin,
                Estado = "P"
            };

            _context.Reservas.Add(model);
            await _context.SaveChangesAsync();

            return Ok(new { exito = true, mensaje = "Reserva creada correctamente." });
        }

        // 🔹 CONFIRMAR
        [HttpPatch("{id}/confirmar")]
        public async Task<IActionResult> Confirmar(int id)
        {
            var reserva = await _context.Reservas.FindAsync(id);
            if (reserva == null)
                return NotFound();

            if (reserva.Estado != "P")
                return BadRequest(new { exito = false, mensaje = "Solo reservas pendientes pueden confirmarse." });

            reserva.Estado = "C";
            await _context.SaveChangesAsync();

            return Ok(new { exito = true, mensaje = "Reserva confirmada." });
        }

        // 🔹 CANCELAR
        [HttpPatch("{id}/cancelar")]
        public async Task<IActionResult> Cancelar(int id)
        {
            var reserva = await _context.Reservas.FindAsync(id);
            if (reserva == null)
                return NotFound();

            reserva.Estado = "X";
            await _context.SaveChangesAsync();

            return Ok(new { exito = true, mensaje = "Reserva cancelada." });
        }
    }
}