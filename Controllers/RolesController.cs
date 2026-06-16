using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApiVinculacionProyectosV2.Models;

namespace WebApiVinculacionProyectosV2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]   // => api/Roles
    public class RolesController : ControllerBase
    {
        private readonly ResidenciasDbContext _db;

        public RolesController(ResidenciasDbContext db)
        {
            _db = db;
        }

        // GET: api/Roles
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var roles = await _db.Rol
                .AsNoTracking()
                .OrderBy(r => r.Descripcion)
                .Select(r => new
                {
                    id = r.Id,
                    descripcion = r.Descripcion,
                    activo = r.Activo
                })
                .ToListAsync();

            return Ok(roles);
        }
    }
}
