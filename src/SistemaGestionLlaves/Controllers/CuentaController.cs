using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaGestionLlaves.Data;
using System.Security.Claims;

namespace SistemaGestionLlaves.Controllers;

/// <summary>
/// Controlador responsable de la autenticación de usuarios.
/// Marcado con [AllowAnonymous] para que el filtro global no bloquee el acceso al Login.
/// </summary>
[AllowAnonymous]
public class CuentaController : Controller
{
    private readonly ApplicationDbContext _context;

    public CuentaController(ApplicationDbContext context)
    {
        _context = context;
    }

    // -------------------------------------------------------
    // GET /Cuenta/Login
    // -------------------------------------------------------
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        // Si ya está autenticado, redirigir directamente al Dashboard
        if (User.Identity is { IsAuthenticated: true })
            return RedirectToLocal(returnUrl);

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    // -------------------------------------------------------
    // POST /Cuenta/Login
    // -------------------------------------------------------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string username, string password, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ViewBag.Error = "Debe ingresar usuario y contraseña.";
            return View();
        }

        // Buscar usuario activo con su Rol y Persona
        var usuario = await _context.Usuarios
            .Include(u => u.Rol)
            .Include(u => u.Persona)
            .FirstOrDefaultAsync(u => u.NombreUsuario == username && u.Estado == "A");

        // Mensaje genérico para no revelar si el usuario existe
        if (usuario == null || !BCrypt.Net.BCrypt.Verify(password, usuario.PasswordHash))
        {
            ViewBag.Error = "Usuario o contraseña incorrectos.";
            return View();
        }

        // Crear claims para la sesión
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
            new(ClaimTypes.Name,           usuario.NombreUsuario),
            new(ClaimTypes.Role,           usuario.Rol.Nombre),
            new("NombreCompleto",          $"{usuario.Persona.Nombres} {usuario.Persona.Apellidos}"),
            new("IdPersona",               usuario.IdPersona.ToString())
        };

        var identidad = new ClaimsIdentity(claims, "CookieAuth");
        await HttpContext.SignInAsync("CookieAuth", new ClaimsPrincipal(identidad));

        return RedirectToLocal(returnUrl);
    }

    // -------------------------------------------------------
    // GET /Cuenta/Logout
    // -------------------------------------------------------
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("CookieAuth");
        return RedirectToAction("Login", "Cuenta");
    }

    // -------------------------------------------------------
    // GET /Cuenta/CambiarPassword
    // -------------------------------------------------------
    [HttpGet]
    [Authorize]
    public IActionResult CambiarPassword()
    {
        return View(new SistemaGestionLlaves.Models.ViewModels.CambiarPasswordViewModel());
    }

    // -------------------------------------------------------
    // POST /Cuenta/CambiarPassword
    // -------------------------------------------------------
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarPassword(SistemaGestionLlaves.Models.ViewModels.CambiarPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
        {
            return RedirectToAction("Login");
        }

        var usuario = await _context.Usuarios.FindAsync(userId);
        if (usuario == null || usuario.Estado != "A")
        {
            return RedirectToAction("Login");
        }

        // 1. Validar contraseña actual
        if (!BCrypt.Net.BCrypt.Verify(model.PasswordActual, usuario.PasswordHash))
        {
            ViewBag.Error = "La contraseña actual es incorrecta.";
            return View(model);
        }

        // 2. Hashear nueva contraseña y actualizar
        usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NuevaPassword);
        _context.Update(usuario);
        await _context.SaveChangesAsync();

        TempData["MensajeExito"] = "Su contraseña ha sido cambiada exitosamente. Por favor, vuelva a iniciar sesión.";
        
        // Cerrar sesión para obligar a entrar con nueva contraseña
        await HttpContext.SignOutAsync("CookieAuth");
        return RedirectToAction("Login");
    }

    // -------------------------------------------------------
    // GET /Cuenta/Denegado
    // -------------------------------------------------------
    public IActionResult Denegado() => View();

    // -------------------------------------------------------
    // Helpers
    // -------------------------------------------------------
    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }
}
