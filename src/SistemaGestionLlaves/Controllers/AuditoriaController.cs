using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaGestionLlaves.Data;

namespace SistemaGestionLlaves.Controllers;

[Authorize(Roles = "Administrador")]
public class AuditoriaController : Controller
{
    private readonly ApplicationDbContext _context;

    public AuditoriaController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var logs = await _context.Auditorias
            .AsNoTracking()
            .OrderByDescending(a => a.Fecha)
            .Take(200)
            .ToListAsync();

        return View(logs);
    }
}
