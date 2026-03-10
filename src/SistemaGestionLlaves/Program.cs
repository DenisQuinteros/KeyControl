using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using SistemaGestionLlaves.Data;
using SistemaGestionLlaves.Services;

// Required for QuestPDF to work without a commercial license
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// -------------------------------------------------------
// Servicios
// -------------------------------------------------------

// Filtro global: toda acción requiere usuario autenticado.
// Las acciones públicas deben decorarse explícitamente con [AllowAnonymous].
var requireAuthenticated = new AuthorizationPolicyBuilder()
    .RequireAuthenticatedUser()
    .Build();

builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new AuthorizeFilter(requireAuthenticated)));

// Entity Framework Core + PostgreSQL + Auditoría
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuditInterceptor>();

builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorCodesToAdd: null);
        }
    );

    // Interceptor de auditoría EF Core
    options.AddInterceptors(serviceProvider.GetRequiredService<AuditInterceptor>());
});

// Autenticación por cookies
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", options =>
    {
        options.LoginPath = "/Cuenta/Login";
        options.LogoutPath = "/Cuenta/Logout";
        options.AccessDeniedPath = "/Cuenta/Denegado";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddAuthorization();

// Capa de servicios
builder.Services.AddScoped<IPrestamoService, PrestamoService>();
builder.Services.AddScoped<IReportesService, ReportesService>();
builder.Services.AddSingleton<IPdfService, PdfService>();

// -------------------------------------------------------
// Pipeline
// -------------------------------------------------------
var app = builder.Build();

// Aplicar migraciones y seed al iniciar
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        db.Database.Migrate();
        await SeedData.InicializarAsync(db);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error al aplicar migraciones o seed.");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
