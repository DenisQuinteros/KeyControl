using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaGestionLlaves.Data;
using SistemaGestionLlaves.Models;

namespace SistemaGestionLlaves.Controllers;

[Authorize]
public class RolController : Controller
{
    private readonly ApplicationDbContext _context;

    public RolController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
    {
        var query = _context.Roles.AsQueryable();

        int totalItems = await query.CountAsync();
        var roles = await query
            .AsNoTracking()
            .OrderBy(r => r.IdRol)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        return View(roles);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var rol = await _context.Roles.FirstOrDefaultAsync(r => r.IdRol == id);
        if (rol == null) return NotFound();

        return View(rol);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("NombreRol,Descripcion,Estado")] Rol rol)
    {
        if (ModelState.IsValid)
        {
            _context.Add(rol);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(rol);
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var rol = await _context.Roles.FindAsync(id);
        if (rol == null) return NotFound();
        return View(rol);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("IdRol,NombreRol,Descripcion,Estado")] Rol rol)
    {
        if (id != rol.IdRol) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(rol);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Roles.Any(e => e.IdRol == rol.IdRol)) return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }
        return View(rol);
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
        var rol = await _context.Roles.FirstOrDefaultAsync(r => r.IdRol == id);
        if (rol == null) return NotFound();
        return View(rol);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var rol = await _context.Roles.FindAsync(id);
        if (rol != null)
        {
            _context.Roles.Remove(rol);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}
