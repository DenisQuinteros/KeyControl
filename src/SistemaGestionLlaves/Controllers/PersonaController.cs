using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaGestionLlaves.Data;
using SistemaGestionLlaves.Models;

namespace SistemaGestionLlaves.Controllers
{
    [Authorize(Roles = "Administrador, Operador")]
    public class PersonaController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PersonaController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================
        // LISTAR
        // ==========================
        public async Task<IActionResult> Index(string buscar, int page = 1, int pageSize = 10)
        {
            var personas = from p in _context.Personas
                           where p.Estado == "A"
                           select p;

            if (!string.IsNullOrEmpty(buscar))
            {
                personas = personas.Where(p =>
                    p.Nombres.Contains(buscar) ||
                    p.Apellidos.Contains(buscar) ||
                    p.Ci.Contains(buscar));
            }

            int totalItems = await personas.CountAsync();
            var items = await personas
                .AsNoTracking()
                .OrderBy(p => p.IdPersona)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.Buscar = buscar;

            return View(items);
        }

        // ==========================
        // DETALLE
        // ==========================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var persona = await _context.Personas
                .FirstOrDefaultAsync(m => m.IdPersona == id);

            if (persona == null) return NotFound();

            return View(persona);
        }

        // ==========================
        // CREAR (GET)
        // ==========================
        public IActionResult Create()
        {
            return View();
        }

        // ==========================
        // CREAR (POST)
        // ==========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Persona persona)
        {
            if (ModelState.IsValid)
            {
                if (await _context.Personas.AnyAsync(p => p.Ci == persona.Ci))
                {
                    ModelState.AddModelError("Ci", "El CI ya está registrado");
                    return View(persona);
                }

                persona.Estado = "A";
                _context.Add(persona);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(persona);
        }

        // ==========================
        // EDITAR (GET)
        // ==========================
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var persona = await _context.Personas.FindAsync(id);
            if (persona == null) return NotFound();

            return View(persona);
        }

        // ==========================
        // EDITAR (POST)
        // ==========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Persona persona)
        {
            if (id != persona.IdPersona) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(persona);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Personas.Any(e => e.IdPersona == persona.IdPersona))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(persona);
        }

        // ==========================
        // ELIMINAR (GET)
        // ==========================
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var persona = await _context.Personas
                .FirstOrDefaultAsync(m => m.IdPersona == id);

            if (persona == null) return NotFound();

            return View(persona);
        }

        // ==========================
        // ELIMINAR (POST)
        // ==========================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var persona = await _context.Personas.FindAsync(id);
            if (persona != null)
            {
                persona.Estado = "I";
                _context.Personas.Update(persona);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}