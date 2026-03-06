using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaGestionLlaves.Data;
using SistemaGestionLlaves.Models;

namespace SistemaGestionLlaves.Controllers;

/// <summary>
/// Controlador responsable de las operaciones CRUD para la entidad Usuario.
/// Requiere que el usuario esté autenticado para acceder.
/// </summary>
[Authorize]
public class UsuariosController : Controller
{
    private readonly ApplicationDbContext _context;

    public UsuariosController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Obtiene y muestra la lista completa de usuarios del sistema.
    /// GET: Usuarios
    /// </summary>
    /// <returns>Vista con la lista de usuarios.</returns>
    public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
    {
        var query = _context.Usuarios
            .Include(u => u.Persona)
            .Include(u => u.Rol)
            .AsQueryable();

        int totalItems = await query.CountAsync();
        var usuarios = await query
            .AsNoTracking()
            .OrderBy(u => u.IdUsuario)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        return View(usuarios);
    }

    /// <summary>
    /// Muestra los detalles de un usuario específico.
    /// GET: Usuarios/Details/5
    /// </summary>
    /// <param name="id">Identificador del usuario.</param>
    /// <returns>Vista de detalles o NotFound en caso de no existir.</returns>
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var usuario = await _context.Usuarios
            .Include(u => u.Persona)
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(m => m.IdUsuario == id);
            
        if (usuario == null)
        {
            return NotFound();
        }

        return View(usuario);
    }

    /// <summary>
    /// Prepara y muestra el formulario para la creación de un nuevo usuario.
    /// GET: Usuarios/Create
    /// </summary>
    /// <returns>Vista con el formulario de creación.</returns>
    public IActionResult Create()
    {
        // Preparar las listas desplegables para la vista de creación
        ViewData["IdPersona"] = new SelectList(_context.Personas.OrderBy(p => p.Nombres).ThenBy(p => p.Apellidos), "IdPersona", "NombreCompleto");
        ViewData["IdRol"] = new SelectList(_context.Roles.OrderBy(r => r.NombreRol), "IdRol", "NombreRol");
        return View();
    }

    /// <summary>
    /// Recibe los datos del formulario y crea un nuevo usuario en la base de datos.
    /// POST: Usuarios/Create
    /// </summary>
    /// <param name="usuario">Entidad usuario a insertar.</param>
    /// <returns>Redirección a Index si es exitoso o la vista de creación si hay error.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("IdPersona,IdRol,NombreUsuario,PasswordHash,Estado")] Usuario usuario)
    {
        if (ModelState.IsValid)
        {
            usuario.FechaInicio = DateTime.UtcNow;

            // Hashear la contraseña antes de guardarla en la BD
            if(!string.IsNullOrEmpty(usuario.PasswordHash))
            {
                usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(usuario.PasswordHash);
            }
            
            _context.Add(usuario);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        
        // Si hay errores de validación, recargar las listas desplegables
        ViewData["IdPersona"] = new SelectList(_context.Personas, "IdPersona", "NombreCompleto", usuario.IdPersona);
        ViewData["IdRol"] = new SelectList(_context.Roles.OrderBy(r => r.NombreRol), "IdRol", "NombreRol", usuario.IdRol);
        return View(usuario);
    }

    /// <summary>
    /// Obtiene un usuario específico para ser editado y prepara las listas para la vista.
    /// GET: Usuarios/Edit/5
    /// </summary>
    /// <param name="id">Identificador del usuario.</param>
    /// <returns>Vista con el formulario de edición.</returns>
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null)
        {
            return NotFound();
        }
        
        ViewData["IdPersona"] = new SelectList(_context.Personas.OrderBy(p => p.Nombres).ThenBy(p => p.Apellidos), "IdPersona", "NombreCompleto", usuario.IdPersona);
        ViewData["IdRol"] = new SelectList(_context.Roles.OrderBy(r => r.NombreRol), "IdRol", "NombreRol", usuario.IdRol);
        return View(usuario);
    }

    /// <summary>
    /// Recibe los datos modificados y los actualiza en la base de datos, con especial atención a la contraseña.
    /// POST: Usuarios/Edit/5
    /// </summary>
    /// <param name="id">Identificador del usuario a modificar.</param>
    /// <param name="usuario">Entidad usuario con los datos modificados.</param>
    /// <returns>Redirección a Index si es exitoso, NotFound o redirección a Edit si hay error.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("IdUsuario,IdPersona,IdRol,NombreUsuario,PasswordHash,Estado")] Usuario usuario)
    {
        if (id != usuario.IdUsuario)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                // Obtenemos el usuario existente sin tracking para poder comparar su contraseña previa
                var existingUser = await _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.IdUsuario == id);
                if (existingUser == null) return NotFound();

                // Gestionar la actualización de la contraseña
                if (!string.IsNullOrEmpty(usuario.PasswordHash) && existingUser.PasswordHash != usuario.PasswordHash)
                {
                    // Si la contraseña ha cambiado y no es un hash de BCrypt, aplicar hash
                    if(!usuario.PasswordHash.StartsWith("$2")) 
                    {
                        usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(usuario.PasswordHash);
                    }
                }
                else
                {
                    // Mantener la contraseña existente si no se provee una nueva
                    usuario.PasswordHash = existingUser.PasswordHash;
                }

                _context.Update(usuario);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UsuarioExists(usuario.IdUsuario))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        
        // Si hay error en validación, recargar listas
        ViewData["IdPersona"] = new SelectList(_context.Personas, "IdPersona", "NombreCompleto", usuario.IdPersona);
        ViewData["IdRol"] = new SelectList(_context.Roles.OrderBy(r => r.NombreRol), "IdRol", "NombreRol", usuario.IdRol);
        return View(usuario);
    }

    /// <summary>
    /// Muestra un elemento antes de confirmar su eliminación o desactivación.
    /// GET: Usuarios/Delete/5
    /// </summary>
    /// <param name="id">Identificador del usuario.</param>
    /// <returns>Vista de confirmación de eliminación.</returns>
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var usuario = await _context.Usuarios
            .Include(u => u.Persona)
            .Include(u => u.Rol)
            .FirstOrDefaultAsync(m => m.IdUsuario == id);
            
        if (usuario == null)
        {
            return NotFound();
        }

        return View(usuario);
    }

    /// <summary>
    /// Confirma la eliminación física del usuario en la base de datos.
    /// POST: Usuarios/Delete/5
    /// </summary>
    /// <param name="id">Identificador del usuario.</param>
    /// <returns>Redirección a la lista de usuarios.</returns>
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario != null)
        {
            _context.Usuarios.Remove(usuario);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Método privado para validar si un usuario existe previendo errores de concurrencia.
    /// </summary>
    private bool UsuarioExists(int id)
    {
        return _context.Usuarios.Any(e => e.IdUsuario == id);
    }
}
