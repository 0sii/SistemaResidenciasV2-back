using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApiVinculacionProyectosV2.Models;

namespace WebApiVinculacionProyectosV2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContactosController : ControllerBase
    {
        private readonly ResidenciasDbContext _db;

        public ContactosController(ResidenciasDbContext db)
        {
            _db = db;
        }

        // GET: api/Contactos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Contacto>>> GetAll()
        {
            var contactos = await _db.Contacto
                .AsNoTracking()
                .ToListAsync();

            return Ok(contactos);
        }

        // GET: api/Contactos/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Contacto>> GetById(int id)
        {
            var contacto = await _db.Contacto
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (contacto is null)
                return NotFound(new { message = "Contacto no encontrado" });

            return Ok(contacto);
        }

        // GET: api/Contactos/empresa/10
        [HttpGet("empresa/{idEmpresa:int}")]
        public async Task<ActionResult<IEnumerable<Contacto>>> GetByEmpresa(int idEmpresa)
        {
            var contactos = await _db.Contacto
                .AsNoTracking()
                .Where(c => c.IdEmpresa == idEmpresa)
                .ToListAsync();

            return Ok(contactos);
        }

        // POST: api/Contactos
        [HttpPost]
        public async Task<ActionResult<Contacto>> Create([FromBody] Contacto contacto)
        {
            if (contacto is null)
                return BadRequest(new { message = "Modelo Contacto requerido" });

            if (contacto.IdEmpresa <= 0)
                return BadRequest(new { message = "IdEmpresa es obligatorio" });

            if (string.IsNullOrWhiteSpace(contacto.nombre))
                return BadRequest(new { message = "El nombre es obligatorio" });

            if (string.IsNullOrWhiteSpace(contacto.correo))
                return BadRequest(new { message = "El correo es obligatorio" });

            // Opcional: normalizar/corregir datos
            contacto.nombre = contacto.nombre.Trim();
            contacto.correo = contacto.correo.Trim().ToLower();

            _db.Contacto.Add(contacto);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = contacto.Id }, contacto);
        }

        // PUT: api/Contactos/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] Contacto contacto)
        {
            if (contacto is null)
                return BadRequest(new { message = "Modelo Contacto requerido" });

            if (id != contacto.Id)
                return BadRequest(new { message = "El id de la ruta no coincide con el modelo" });

            if (contacto.IdEmpresa <= 0)
                return BadRequest(new { message = "IdEmpresa es obligatorio" });

            if (string.IsNullOrWhiteSpace(contacto.nombre))
                return BadRequest(new { message = "El nombre es obligatorio" });

            if (string.IsNullOrWhiteSpace(contacto.correo))
                return BadRequest(new { message = "El correo es obligatorio" });

            var dbContacto = await _db.Contacto.FirstOrDefaultAsync(c => c.Id == id);
            if (dbContacto is null)
                return NotFound(new { message = "Contacto no encontrado" });

            // Actualizar campos permitidos
            dbContacto.IdEmpresa = contacto.IdEmpresa;
            dbContacto.nombre = contacto.nombre.Trim();
            dbContacto.Telefono = contacto.Telefono?.Trim();
            dbContacto.correo = contacto.correo.Trim().ToLower();

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/Contactos/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var dbContacto = await _db.Contacto.FirstOrDefaultAsync(c => c.Id == id);
            if (dbContacto is null)
                return NotFound(new { message = "Contacto no encontrado" });

            _db.Contacto.Remove(dbContacto);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}
