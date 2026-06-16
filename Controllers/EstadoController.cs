using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApiVinculacionProyectosV2.Models;

namespace WebApiVinculacionProyectosV2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EstadosController : ControllerBase
    {
        private readonly ResidenciasDbContext _db;

        public EstadosController(ResidenciasDbContext db)
        {
            _db = db;
        }

        // GET: api/Estados?soloActivos=true&search=aguas&page=1&pageSize=50
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool? soloActivos, [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 200) pageSize = 50;

            var query = _db.Estado.AsNoTracking();

            if (soloActivos == true)
                query = query.Where(e => e.Activo);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(e => e.Descripcion.ToLower().Contains(s));
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(e => e.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new { total, page, pageSize, items });
        }

        // GET: api/Estados/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _db.Estado.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            return item is null ? NotFound(new { message = "Estado no encontrado" }) : Ok(item);
        }

        // POST: api/Estados
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Estado body)
        {
            if (body is null) return BadRequest(new { message = "Modelo requerido" });
            if (string.IsNullOrWhiteSpace(body.Descripcion))
                return BadRequest(new { message = "La descripción es requerida" });

            _db.Estado.Add(body);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = body.Id }, body);
        }

        // PUT: api/Estados/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] Estado body)
        {
            var entity = await _db.Estado.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return NotFound(new { message = "Estado no encontrado" });

            entity.Descripcion = body.Descripcion;
            entity.Activo = body.Activo;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/Estados/5  (delete duro; si prefieres soft delete, cambia a Activo=false)
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _db.Estado.FindAsync(id);
            if (entity is null) return NotFound(new { message = "Estado no encontrado" });

            _db.Estado.Remove(entity);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
