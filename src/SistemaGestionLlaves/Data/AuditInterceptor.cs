using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SistemaGestionLlaves.Models;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using System.Text.Json.Serialization;
using System.Security.Claims;

namespace SistemaGestionLlaves.Data;

public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditInterceptor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ProcesarAuditoria(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        ProcesarAuditoria(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ProcesarAuditoria(DbContext? context)
    {
        if (context == null) return;

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.Entity is not Auditoria && 
                       (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted))
            .ToList();

        if (!entries.Any()) return;

        int? idUsuario = null;
        var httpUser = _httpContextAccessor.HttpContext?.User;
        if (httpUser?.Identity?.IsAuthenticated == true)
        {
            var idClaim = httpUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(idClaim, out var parsedId))
            {
                idUsuario = parsedId;
            }
        }

        var auditorias = new List<Auditoria>();

        var jsonOptions = new JsonSerializerOptions 
        { 
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            WriteIndented = false
        };

        foreach (var entry in entries)
        {
            var auditoria = new Auditoria
            {
                TablaAfectada = entry.Metadata.GetTableName() ?? entry.Entity.GetType().Name,
                IdUsuario = idUsuario,
                FechaHora = DateTime.UtcNow
            };

            var antes = new Dictionary<string, object?>();
            var despues = new Dictionary<string, object?>();

            foreach (var prop in entry.Properties)
            {
                if (prop.IsTemporary) continue;
                string propName = prop.Metadata.Name;

                if (entry.State == EntityState.Added)
                {
                    despues[propName] = prop.CurrentValue;
                }
                else if (entry.State == EntityState.Deleted)
                {
                    antes[propName] = prop.OriginalValue;
                }
                else if (entry.State == EntityState.Modified && prop.IsModified)
                {
                    antes[propName] = prop.OriginalValue;
                    despues[propName] = prop.CurrentValue;
                }
            }

            if (entry.State == EntityState.Added)
            {
                auditoria.Operacion = "INSERT";
                auditoria.DatosNuevos = JsonSerializer.Serialize(despues, jsonOptions);
            }
            else if (entry.State == EntityState.Deleted)
            {
                auditoria.Operacion = "DELETE";
                auditoria.DatosAnteriores = JsonSerializer.Serialize(antes, jsonOptions);
            }
            else if (entry.State == EntityState.Modified)
            {
                if (!antes.Any()) continue;
                auditoria.Operacion = "UPDATE";
                auditoria.DatosAnteriores = JsonSerializer.Serialize(antes, jsonOptions);
                auditoria.DatosNuevos = JsonSerializer.Serialize(despues, jsonOptions);
            }

            auditorias.Add(auditoria);
        }

        if (auditorias.Any())
        {
            context.Set<Auditoria>().AddRange(auditorias);
        }
    }
}
