using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaGestionLlaves.Data;
using SistemaGestionLlaves.Models;

namespace SistemaGestionLlaves.Controllers;

/// <summary>
/// Controlador MVC para las vistas de gestión de Llaves.
/// Rutas: /Llaves
/// </summary>
[Authorize(Roles = "Administrador, Operador")]
public class LlavesViewController : Controller
{
    private readonly ApplicationDbContext _context;

    public LlavesViewController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Llaves
    public async Task<IActionResult> Index(string? busqueda, string? estado, int? idAmbiente, int page = 1, int pageSize = 10)
    {
        var query = _context.Llaves
            .Include(l => l.Ambiente)
            .Where(l => l.Estado != "I")
            .AsQueryable();

        if (idAmbiente.HasValue)
            query = query.Where(l => l.IdAmbiente == idAmbiente.Value);

        if (!string.IsNullOrWhiteSpace(estado))
            query = query.Where(l => l.Estado == estado);

        if (!string.IsNullOrWhiteSpace(busqueda))
            query = query.Where(l =>
                l.Codigo.Contains(busqueda) ||
                l.Ambiente.Nombre.Contains(busqueda));

        ViewData["busqueda"] = busqueda;
        ViewData["estado"] = estado;
        ViewData["idAmbiente"] = idAmbiente;
        ViewData["Ambientes"] = new SelectList(_context.Ambientes.Where(a => a.Estado == "A").OrderBy(a => a.Nombre), "IdAmbiente", "Nombre", idAmbiente);

        int totalItems = await query.CountAsync();
        var llaves = await query
            .AsNoTracking()
            .OrderBy(l => l.Ambiente.Nombre)
            .ThenBy(l => l.Codigo)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        return View(llaves);
    }

    // GET: Llaves/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var llave = await _context.Llaves
            .Include(l => l.Ambiente).ThenInclude(a => a.TipoAmbiente)
            .Include(l => l.PersonasAutorizadas).ThenInclude(pa => pa.Persona)
            .FirstOrDefaultAsync(l => l.IdLlave == id);

        if (llave == null) return NotFound();

        return View(llave);
    }

    // GET: Llaves/Create
    public IActionResult Create()
    {
        ViewData["IdAmbiente"] = new SelectList(
            _context.Ambientes.Where(a => a.Estado == "A").OrderBy(a => a.Nombre),
            "IdAmbiente", "Nombre");
        return View();
    }

    // POST: Llaves/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Codigo,NumCopias,IdAmbiente,EsMaestra,Observaciones")] Llave llave)
    {
        // Verificar código único
        if (await _context.Llaves.AnyAsync(l => l.Codigo == llave.Codigo.ToUpper()))
            ModelState.AddModelError("Codigo", "Ya existe una llave con este código.");

        if (ModelState.IsValid)
        {
            llave.Codigo = llave.Codigo.Trim().ToUpper();
            llave.Estado = "D"; // Disponible por defecto
            _context.Add(llave);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Llave '{llave.Codigo}' creada exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        ViewData["IdAmbiente"] = new SelectList(
            _context.Ambientes.Where(a => a.Estado == "A").OrderBy(a => a.Nombre),
            "IdAmbiente", "Nombre", llave.IdAmbiente);
        return View(llave);
    }

    // GET: Llaves/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var llave = await _context.Llaves.FindAsync(id);
        if (llave == null) return NotFound();

        // Solo se pueden editar llaves no prestadas
        if (llave.Estado == "P")
        {
            TempData["Error"] = "No se puede editar una llave que está actualmente prestada.";
            return RedirectToAction(nameof(Index));
        }

        ViewData["IdAmbiente"] = new SelectList(
            _context.Ambientes.Where(a => a.Estado == "A").OrderBy(a => a.Nombre),
            "IdAmbiente", "Nombre", llave.IdAmbiente);
        return View(llave);
    }

    // POST: Llaves/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("IdLlave,Codigo,NumCopias,IdAmbiente,EsMaestra,Observaciones,Estado")] Llave llave)
    {
        if (id != llave.IdLlave) return NotFound();

        // Verificar código único excluyendo la llave actual
        if (await _context.Llaves.AnyAsync(l => l.Codigo == llave.Codigo.ToUpper() && l.IdLlave != id))
            ModelState.AddModelError("Codigo", "Ya existe otra llave con este código.");

        if (ModelState.IsValid)
        {
            try
            {
                llave.Codigo = llave.Codigo.Trim().ToUpper();
                _context.Update(llave);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Llave '{llave.Codigo}' actualizada exitosamente.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Llaves.Any(l => l.IdLlave == llave.IdLlave))
                    return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        ViewData["IdAmbiente"] = new SelectList(
            _context.Ambientes.Where(a => a.Estado == "A").OrderBy(a => a.Nombre),
            "IdAmbiente", "Nombre", llave.IdAmbiente);
        return View(llave);
    }

    // POST: Llaves/CambiarEstado/5  (toggle activo/inactivo)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id)
    {
        var llave = await _context.Llaves.FindAsync(id);
        if (llave == null) return NotFound();

        if (llave.Estado == "P")
        {
            TempData["Error"] = "No se puede cambiar el estado de una llave prestada.";
            return RedirectToAction(nameof(Index));
        }

        if (llave.Estado == "I")
        {
            llave.Estado = "D";
            TempData["Success"] = $"Llave '{llave.Codigo}' reactivada.";
        }
        else
        {
            bool tienePrestamo = await _context.Prestamos.AnyAsync(p => p.IdLlave == id && p.Estado == "A");
            bool tieneReserva = await _context.Reservas.AnyAsync(r => r.IdLlave == id && (r.Estado == "P" || r.Estado == "C"));

            if (tienePrestamo || tieneReserva)
            {
                TempData["Error"] = "No se puede inactivar: la llave tiene préstamos activos o reservas pendientes.";
                return RedirectToAction(nameof(Index));
            }

            llave.Estado = "I";
            TempData["Success"] = $"Llave '{llave.Codigo}' inactivada.";
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
