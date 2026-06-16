using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApiVinculacionProyectosV2.Models;

namespace WebApiVinculacionProyectosV2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CarrerasController : ControllerBase
    {
        private readonly ResidenciasDbContext _db;

        // Inyecta tu DbContext (cambia ApplicationDbContext por el tuyo si tiene otro nombre)
        public CarrerasController(ResidenciasDbContext db)
        {
            _db = db;
        }

        // GET: api/Carreras
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Carreras>>> GetAll()
        {
            // Si solo quieres las activas, usa: .Where(c => c.Activo)
            var carreras = await _db.Carreras
                .AsNoTracking()
                .ToListAsync();

            return Ok(carreras);
        }

        // GET: api/Carreras/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Carreras>> GetById(int id)
        {
            var carrera = await _db.Carreras
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (carrera is null)
                return NotFound(new { message = "Carrera no encontrada" });

            return Ok(carrera);
        }

        // POST: api/Carreras
        [HttpPost]
        public async Task<ActionResult<Carreras>> Create([FromBody] Carreras carrera)
        {
            if (carrera is null)
                return BadRequest(new { message = "Modelo Carrera requerido" });

            if (string.IsNullOrWhiteSpace(carrera.Descripcion))
                return BadRequest(new { message = "La descripción es obligatoria" });

            // Por defecto Activo = true si no viene
            if (!carrera.Activo)
                carrera.Activo = true;

            _db.Carreras.Add(carrera);
            await _db.SaveChangesAsync();

            // Devuelve 201 con la ruta al recurso creado
            return CreatedAtAction(nameof(GetById), new { id = carrera.Id }, carrera);
        }

        // PUT: api/Carreras/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] Carreras carrera)
        {
            if (carrera is null)
                return BadRequest(new { message = "Modelo Carrera requerido" });

            if (id != carrera.Id)
                return BadRequest(new { message = "El id de la ruta no coincide con el modelo" });

            if (string.IsNullOrWhiteSpace(carrera.Descripcion))
                return BadRequest(new { message = "La descripción es obligatoria" });

            var dbCarrera = await _db.Carreras.FirstOrDefaultAsync(c => c.Id == id);
            if (dbCarrera is null)
                return NotFound(new { message = "Carrera no encontrada" });

            // Actualizar campos permitidos
            dbCarrera.Descripcion = carrera.Descripcion.Trim();
            dbCarrera.Activo = carrera.Activo;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/Carreras/5
        // Puedes usarlo como borrado lógico o físico; aquí lo hago lógico
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var dbCarrera = await _db.Carreras.FirstOrDefaultAsync(c => c.Id == id);
            if (dbCarrera is null)
                return NotFound(new { message = "Carrera no encontrada" });

            // Borrado lógico
            dbCarrera.Activo = false;
            await _db.SaveChangesAsync();

            // Si quieres borrar físico, en vez de lo de arriba:
            // _db.Carreras.Remove(dbCarrera);
            // await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}
