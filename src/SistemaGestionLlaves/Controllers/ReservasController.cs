using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaGestionLlaves.Data;

[Authorize(Roles = "Administrador,Operador")]
public class ReservasController : Controller
{
    private readonly ApplicationDbContext _context;

    public ReservasController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    public async Task<IActionResult> Crear()
    {
        ViewBag.Llaves = await _context.Llaves
            .Include(l => l.Ambiente)
            .Where(l => l.Estado == "D")
            .OrderBy(l => l.Codigo)
            .Select(l => new SelectListItem
            {
                Value = l.IdLlave.ToString(),
                Text = l.Codigo + " — " + (l.Ambiente != null ? l.Ambiente.Nombre : "Sin ambiente")
            }).ToListAsync();

        ViewBag.Personas = await _context.Personas
            .Where(p => p.Estado == "A")
            .OrderBy(p => p.Apellidos)
            .Select(p => new SelectListItem
            {
                Value = p.IdPersona.ToString(),
                Text = p.Apellidos + ", " + p.Nombres + " (" + p.Ci + ")"
            }).ToListAsync();

        return View("Create");
    }
}