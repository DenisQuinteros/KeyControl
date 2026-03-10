using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaGestionLlaves.Data;
using SistemaGestionLlaves.Models;

namespace SistemaGestionLlaves.Controllers
{
    [Authorize(Roles = "Administrador, Operador")]
    public class AmbientesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AmbientesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // LISTAR
        public async Task<IActionResult> Index(string buscar, int page = 1, int pageSize = 10)
        {
            var query = _context.Ambientes
                .Include(a => a.TipoAmbiente)
                .Include(a => a.Llaves)
                .Where(a => a.Estado == "A")
                .AsQueryable();

            if (!string.IsNullOrEmpty(buscar))
            {
                query = query.Where(a => a.Nombre.Contains(buscar) || a.Codigo.Contains(buscar));
            }

            ViewBag.Buscar = buscar;
            int totalItems = await query.CountAsync();
            var ambientes = await query
                .AsNoTracking()
                .OrderBy(a => a.IdAmbiente)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            return View(ambientes);
        }

    public async Task<IActionResult> Details(int id)
    {
        var ambiente = await _context.Ambientes
            .Include(a => a.TipoAmbiente)
            .Include(a => a.Llaves)
            .FirstOrDefaultAsync(a => a.IdAmbiente == id);

        if (ambiente == null)
            return NotFound();

        return View(ambiente);
    }

        // CREAR GET
        public async Task<IActionResult> Create()
{
    ViewBag.Tipos = new SelectList(
        await _context.TiposAmbiente.ToListAsync(),
        "IdTipo",
        "NombreTipo"   // 👈 CORREGIDO
    );

    return View();
}


        // CREAR POST
        [HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(Ambiente ambiente)
{
    if (ModelState.IsValid)
    {
        _context.Add(ambiente);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // Si hay error, volver a cargar el combo
    ViewBag.Tipos = new SelectList(
        await _context.TiposAmbiente.ToListAsync(),
        "IdTipo",
        "NombreTipo",
        ambiente.IdTipo
    );

    return View(ambiente);
}

        // EDITAR POST
       [HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Edit(int id, Ambiente ambiente)
{
    if (id != ambiente.IdAmbiente)
        return NotFound();

    if (ModelState.IsValid)
    {
        try
        {
            _context.Update(ambiente);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Ambientes.Any(e => e.IdAmbiente == ambiente.IdAmbiente))
                return NotFound();
            else
                throw;
        }

        return RedirectToAction(nameof(Index));
    }

    ViewBag.Tipos = new SelectList(
        await _context.TiposAmbiente.ToListAsync(),
        "IdTipo",
        "NombreTipo",
        ambiente.IdTipo
    );

    return View(ambiente);
}
        // EDITAR
       public async Task<IActionResult> Edit(int id)
{
    var ambiente = await _context.Ambientes.FindAsync(id);

    if (ambiente == null)
        return NotFound();

    ViewBag.Tipos = new SelectList(await _context.TiposAmbiente.ToListAsync(), "IdTipo", "NombreTipo", ambiente.IdTipo);

    return View(ambiente);
}


// DELETE GET
public async Task<IActionResult> Delete(int id)
{
    var ambiente = await _context.Ambientes
        .Include(a => a.TipoAmbiente)
        .FirstOrDefaultAsync(a => a.IdAmbiente == id);

    if (ambiente == null)
        return NotFound();

    return View(ambiente);
}

// DELETE POST
[HttpPost, ActionName("Delete")]
[ValidateAntiForgeryToken]
public async Task<IActionResult> DeleteConfirmed(int id)
{
    var tieneLlaves = _context.Llaves.Any(l => l.IdAmbiente == id);

    if (tieneLlaves)
    {
        ModelState.AddModelError("", "No se puede eliminar el ambiente porque tiene llaves asociadas.");
        var ambiente = await _context.Ambientes.FindAsync(id);
        return View(ambiente);
    }

    var ambienteEliminar = await _context.Ambientes.FindAsync(id);

    if (ambienteEliminar != null)
    {
        ambienteEliminar.Estado = "I";
        _context.Ambientes.Update(ambienteEliminar);
        await _context.SaveChangesAsync();
    }

    return RedirectToAction(nameof(Index));
}


    }

    
}
