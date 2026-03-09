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
            .Select(l => new SelectListItem
            {
                Value = l.IdLlave.ToString(),
                Text = l.Codigo
            }).ToListAsync();

        ViewBag.Personas = await _context.Personas
            .Select(p => new SelectListItem
            {
                Value = p.IdPersona.ToString(),
                Text = p.NombreCompleto
            }).ToListAsync();

        return View("Create");
    }
}