using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApiVinculacionProyectosV2.Models;

namespace WebApiVinculacionProyectosV2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContactoemergenciaController : ControllerBase
    {
        private readonly ResidenciasDbContext _db;

        public ContactoemergenciaController(ResidenciasDbContext db)
        {
            _db = db;
        }

        // GET: api/Contactoemergencia?search=ana&page=1&pageSize=20
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 200) pageSize = 20;

            var query = _db.Contactoemergencia.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(c =>
                    (c.Nombre != null && c.Nombre.ToLower().Contains(s)) ||
                    (c.Parentesco != null && c.Parentesco.ToLower().Contains(s)) ||
                    (c.Domicilio != null && c.Domicilio.ToLower().Contains(s)) ||
                    (c.Telefono != null && c.Telefono.ToLower().Contains(s)) ||
                    (c.email != null && c.email.ToLower().Contains(s))
                );
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(c => c.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new { total, page, pageSize, items });
        }

        // GET: api/Contactoemergencia/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _db.Contactoemergencia.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            return item is null ? NotFound(new { message = "Contacto de emergencia no encontrado" }) : Ok(item);
        }

        // POST: api/Contactoemergencia
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Contactoemergencia body)
        {
            if (body is null) return BadRequest(new { message = "Modelo requerido" });


            _db.Contactoemergencia.Add(body);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = body.Id }, body);
        }

        // PUT: api/Contactoemergencia/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] Contactoemergencia body)
        {
            var entity = await _db.Contactoemergencia.FirstOrDefaultAsync(x => x.Id == id);
            if (entity is null) return NotFound(new { message = "Contacto de emergencia no encontrado" });

            entity.Nombre = body.Nombre;
            entity.Parentesco = body.Parentesco;
            entity.Domicilio = body.Domicilio;
            entity.Telefono = body.Telefono;
            entity.email = body.email;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/Contactoemergencia/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _db.Contactoemergencia.FindAsync(id);
            if (entity is null) return NotFound(new { message = "Contacto de emergencia no encontrado" });

            _db.Contactoemergencia.Remove(entity);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
