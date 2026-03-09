using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SistemaGestionLlaves.Data;
using SistemaGestionLlaves.Models;

namespace SistemaGestionLlaves.Services
{
    public class DemoDataSeeder
    {
        private readonly ApplicationDbContext _context;

        public DemoDataSeeder(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<object> SeedAsync()
        {
            var random = new Random();

            // ───── TipoAmbiente ─────
            var tipoAmbiente = await _context.TiposAmbiente
                .FirstOrDefaultAsync(t => t.NombreTipo == "Aula");

            if (tipoAmbiente == null)
            {
                tipoAmbiente = new TipoAmbiente
                {
                    NombreTipo = "Aula"
                };

                await _context.TiposAmbiente.AddAsync(tipoAmbiente);
                await _context.SaveChangesAsync();
            }

            // ───── Ambientes (30) ─────
            int ambientesActuales = await _context.Ambientes.CountAsync();

            if (ambientesActuales < 30)
            {
                var codigosExistentes = (await _context.Ambientes
                    .Select(a => a.Codigo)
                    .ToListAsync()).ToHashSet();

                var nuevosAmbientes = new List<Ambiente>();

                for (int i = 1; i <= 30; i++)
                {
                    var codigo = $"AMB-{i:D3}";

                    if (!codigosExistentes.Contains(codigo))
                    {
                        nuevosAmbientes.Add(new Ambiente
                        {
                            Codigo = codigo,
                            Nombre = $"Aula {100 + i}",
                            IdTipo = tipoAmbiente.IdTipo,
                            Estado = "A"
                        });
                    }
                }

                if (nuevosAmbientes.Any())
                {
                    await _context.Ambientes.AddRangeAsync(nuevosAmbientes);
                    await _context.SaveChangesAsync();
                }
            }

            var ambientes = await _context.Ambientes.ToListAsync();

            // ───── Llaves (50) ─────
            int llavesActuales = await _context.Llaves.CountAsync();

            if (llavesActuales < 50 && ambientes.Any())
            {
                var codigosLlavesExistentes = (await _context.Llaves
                    .Select(l => l.Codigo)
                    .ToListAsync()).ToHashSet();

                var nuevasLlaves = new List<Llave>();
                int contador = llavesActuales + 1;

                while (nuevasLlaves.Count + llavesActuales < 50)
                {
                    var ambiente = ambientes[random.Next(ambientes.Count)];
                    var codigo = $"LL-{ambiente.Codigo}-{contador:D2}";

                    if (!codigosLlavesExistentes.Contains(codigo))
                    {
                        nuevasLlaves.Add(new Llave
                        {
                            Codigo = codigo,
                            IdAmbiente = ambiente.IdAmbiente,
                            NumCopias = 1,
                            Estado = "D",
                            EsMaestra = false
                        });
                    }

                    contador++;
                }

                if (nuevasLlaves.Any())
                {
                    await _context.Llaves.AddRangeAsync(nuevasLlaves);
                    await _context.SaveChangesAsync();
                }
            }

            var llaves = await _context.Llaves.ToListAsync();

            // ───── Personas (80) ─────
            int personasActuales = await _context.Personas.CountAsync();

            if (personasActuales < 80)
            {
                var cisExistentes = (await _context.Personas
                    .Select(p => p.Ci)
                    .ToListAsync()).ToHashSet();

                var nuevasPersonas = new List<Persona>();

                for (int i = personasActuales + 1; i <= 80; i++)
                {
                    var ci = $"8000{i:D5}";

                    if (!cisExistentes.Contains(ci))
                    {
                        nuevasPersonas.Add(new Persona
                        {
                            Nombres = $"NombreDemo{i}",
                            Apellidos = $"ApellidoDemo{i}",
                            Ci = ci,
                            Estado = "A"
                        });
                    }
                }

                if (nuevasPersonas.Any())
                {
                    await _context.Personas.AddRangeAsync(nuevasPersonas);
                    await _context.SaveChangesAsync();
                }
            }

            var personas = await _context.Personas.ToListAsync();

            // ───── Rol Operador ─────
            var rolOperador = await _context.Roles
                .FirstOrDefaultAsync(r => r.NombreRol == "Operador");

            if (rolOperador == null)
            {
                rolOperador = new Rol
                {
                    NombreRol = "Operador",
                    Estado = "A"
                };

                await _context.Roles.AddAsync(rolOperador);
                await _context.SaveChangesAsync();
            }

            // ───── Usuarios Operadores (10) ─────
            int usuariosActuales = await _context.Usuarios
                .CountAsync(u => u.IdRol == rolOperador.IdRol);

            if (usuariosActuales < 10)
            {
                var nombresExistentes = (await _context.Usuarios
                    .Select(u => u.NombreUsuario)
                    .ToListAsync()).ToHashSet();

                var personasConUsuario = (await _context.Usuarios
                    .Select(u => u.IdPersona)
                    .ToListAsync()).ToHashSet();

                var personasLibres = personas
                    .Where(p => !personasConUsuario.Contains(p.IdPersona))
                    .ToList();

                var passwordHash = BCrypt.Net.BCrypt.HashPassword("123456");
                var nuevosUsuarios = new List<Usuario>();

                for (int i = usuariosActuales + 1; i <= 10 && personasLibres.Any(); i++)
                {
                    var nombreUsuario = $"operador{i}";

                    if (!nombresExistentes.Contains(nombreUsuario))
                    {
                        var idx = random.Next(personasLibres.Count);
                        var persona = personasLibres[idx];
                        personasLibres.RemoveAt(idx);

                        nuevosUsuarios.Add(new Usuario
                        {
                            NombreUsuario = nombreUsuario,
                            PasswordHash = passwordHash,
                            IdRol = rolOperador.IdRol,
                            IdPersona = persona.IdPersona,
                            Estado = "A"
                        });
                    }
                }

                if (nuevosUsuarios.Any())
                {
                    await _context.Usuarios.AddRangeAsync(nuevosUsuarios);
                    await _context.SaveChangesAsync();
                }
            }

            var operadores = await _context.Usuarios
                .Where(u => u.IdRol == rolOperador.IdRol)
                .ToListAsync();

            // ───── Préstamos (25) ─────
            int prestamosActuales = await _context.Prestamos.CountAsync();

            if (prestamosActuales < 25 && llaves.Any() && personas.Any() && operadores.Any())
            {
                var llavesConPrestamo = (await _context.Prestamos
                    .Where(p => p.Estado == "A")
                    .Select(p => p.IdLlave)
                    .ToListAsync()).ToHashSet();

                var llavesDisponibles = llaves
                    .Where(l => !llavesConPrestamo.Contains(l.IdLlave))
                    .OrderBy(x => random.Next())
                    .ToList();

                int faltantes = Math.Min(25 - prestamosActuales, llavesDisponibles.Count);

                var nuevosPrestamos = new List<Prestamo>();

                for (int i = 0; i < faltantes; i++)
                {
                    var llave = llavesDisponibles[i];
                    var persona = personas[random.Next(personas.Count)];
                    var operador = operadores[random.Next(operadores.Count)];

                    nuevosPrestamos.Add(new Prestamo
                    {
                        IdLlave = llave.IdLlave,
                        IdPersona = persona.IdPersona,
                        IdUsuario = operador.IdUsuario,
                        FechaHoraPrestamo = DateTime.UtcNow.AddDays(-random.Next(1,5)),
                        Estado = "A"
                    });

                    llave.Estado = "P";
                }

                await _context.Prestamos.AddRangeAsync(nuevosPrestamos);
                await _context.SaveChangesAsync();
            }

            // ───── Reservas (10) ─────
            int reservasActuales = await _context.Reservas.CountAsync();

            if (reservasActuales < 10 && llaves.Any())
            {
                var llavesDisponibles = llaves
                    .Where(l => l.Estado == "D")
                    .ToList();

                int faltantes = Math.Min(10 - reservasActuales, llavesDisponibles.Count);

                var nuevasReservas = new List<Reserva>();

                for (int i = 0; i < faltantes; i++)
                {
                    var llave = llavesDisponibles[random.Next(llavesDisponibles.Count)];
                    var persona = personas[random.Next(personas.Count)];
                    var operador = operadores[random.Next(operadores.Count)];

                    var inicio = DateTime.UtcNow.AddDays(random.Next(1,5));

                    nuevasReservas.Add(new Reserva
                    {
                        IdLlave = llave.IdLlave,
                        IdPersona = persona.IdPersona,
                        IdUsuario = operador.IdUsuario,
                        FechaInicio = inicio,
                        FechaFin = inicio.AddHours(random.Next(2,6)),
                        Estado = "P"
                    });
                }

                await _context.Reservas.AddRangeAsync(nuevasReservas);
                await _context.SaveChangesAsync();
            }

            return new
            {
                Ambientes = await _context.Ambientes.CountAsync(),
                Llaves = await _context.Llaves.CountAsync(),
                Personas = await _context.Personas.CountAsync(),
                Operadores = await _context.Usuarios.CountAsync(u => u.IdRol == rolOperador.IdRol),
                Prestamos = await _context.Prestamos.CountAsync(),
                Reservas = await _context.Reservas.CountAsync(),
                Mensaje = "Seeder ejecutado correctamente."
            };
        }
    }
}