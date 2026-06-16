namespace WebApiVinculacionProyectosV2.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using WebApiVinculacionProyectosV2.Models;

public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly ResidenciasDbContext _db;
    private readonly IMemoryCache _cache;

    public PermissionHandler(ResidenciasDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var userIdStr = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId) || userId <= 0)
            return;

        var cacheKey = $"perm:{userId}";
        var permisos = await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);

            return await (
                from ur in _db.UsuarioRol
                join rp in _db.RolPermiso on ur.IdRol equals rp.idRol
                join p in _db.Permisos on rp.idPermiso equals p.id
                where ur.IdUsuario == userId && p.Activo
                select p.Descripcion
            ).Distinct().ToListAsync();
        });

        if (permisos != null && permisos.Contains(requirement.Permission))
            context.Succeed(requirement);
    }
}
