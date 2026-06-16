using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApiVinculacionProyectosV2.Models;

namespace WebApiVinculacionProyectosV2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ModalidadesController : ControllerBase
    {
        private readonly ResidenciasDbContext _db;

        public ModalidadesController(ResidenciasDbContext db)
        {
            _db = db;
        }

        // GET: api/Modalidades?soloActivos=true&search=hibrida
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Modalidad>>> GetAll()
        {
            // Si quieres solo activas:
            // var modalidades = await _db.Modalidad
            //     .AsNoTracking()
            //     .Where(m => m.Activo)
            //     .ToListAsync();

            var modalidades = await _db.Modalidad
                .AsNoTracking()
                .ToListAsync();

            return Ok(modalidades);
        }

        [HttpGet("activas")]
        public async Task<ActionResult<IEnumerable<Modalidad>>> GetActivas()
        {
            var activas = await _db.Modalidad
                .AsNoTracking()
                .Where(m => m.Activo)
                .ToListAsync();

            return Ok(activas);
        }


        // GET: api/Modalidades/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _db.Modalidad.AsNoTracking().FirstOrDefaultAsync(x => x.id == id);
            return item is null ? NotFound(new { message = "Modalidad no encontrada" }) : Ok(item);
        }

        // POST: api/Modalidades
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Modalidad body)
        {
            if (body is null) return BadRequest(new { message = "Modelo requerido" });
            if (string.IsNullOrWhiteSpace(body.Descripcion))
                return BadRequest(new { message = "La descripción es requerida" });

            _db.Modalidad.Add(body);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = body.id }, body);
        }

        // PUT: api/Modalidades/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] Modalidad body)
        {
            var entity = await _db.Modalidad.FirstOrDefaultAsync(x => x.id == id);
            if (entity is null) return NotFound(new { message = "Modalidad no encontrada" });

            entity.Descripcion = body.Descripcion;
            entity.Activo = body.Activo;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/Modalidades/5  (delete duro; si prefieres soft delete, cambia a Activo=false)
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _db.Modalidad.FindAsync(id);
            if (entity is null) return NotFound(new { message = "Modalidad no encontrada" });

            _db.Modalidad.Remove(entity);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
