using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApiVinculacionProyectosV2.Models;

namespace WebApiVinculacionProyectosV2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DependenciasMedicasController : ControllerBase
    {
        private readonly ResidenciasDbContext _db;

        public DependenciasMedicasController(ResidenciasDbContext db)
        {
            _db = db;
        }

        // GET: api/DependenciasMedicas
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DependenciaMedica>>> GetAll()
        {
            // Si quieres solo activas, cambia por: .Where(d => d.Activo)
            var dependencias = await _db.DependenciasMedica
                .AsNoTracking()
                .ToListAsync();

            return Ok(dependencias);
        }

        // GET: api/DependenciasMedicas/activas
        [HttpGet("activas")]
        public async Task<ActionResult<IEnumerable<DependenciaMedica>>> GetActivas()
        {
            var dependencias = await _db.DependenciasMedica
                .AsNoTracking()
                .Where(d => d.Activo)
                .ToListAsync();

            return Ok(dependencias);
        }

        // GET: api/DependenciasMedicas/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<DependenciaMedica>> GetById(int id)
        {
            var dep = await _db.DependenciasMedica
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id);

            if (dep is null)
                return NotFound(new { message = "Dependencia médica no encontrada" });

            return Ok(dep);
        }

        // POST: api/DependenciasMedicas
        [HttpPost]
        public async Task<ActionResult<DependenciaMedica>> Create([FromBody] DependenciaMedica dependencia)
        {
            if (dependencia is null)
                return BadRequest(new { message = "Modelo DependenciaMedica requerido" });

            if (string.IsNullOrWhiteSpace(dependencia.Descripcion))
                return BadRequest(new { message = "La descripción es obligatoria" });

            dependencia.Descripcion = dependencia.Descripcion.Trim();

            // Por defecto Activo = true si no viene marcado
            if (!dependencia.Activo)
                dependencia.Activo = true;

            _db.DependenciasMedica.Add(dependencia);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = dependencia.Id }, dependencia);
        }

        // PUT: api/DependenciasMedicas/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] DependenciaMedica dependencia)
        {
            if (dependencia is null)
                return BadRequest(new { message = "Modelo DependenciaMedica requerido" });

            if (id != dependencia.Id)
                return BadRequest(new { message = "El id de la ruta no coincide con el modelo" });

            if (string.IsNullOrWhiteSpace(dependencia.Descripcion))
                return BadRequest(new { message = "La descripción es obligatoria" });

            var dbDep = await _db.DependenciasMedica.FirstOrDefaultAsync(d => d.Id == id);
            if (dbDep is null)
                return NotFound(new { message = "Dependencia médica no encontrada" });

            dbDep.Descripcion = dependencia.Descripcion.Trim();
            dbDep.Activo = dependencia.Activo;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/DependenciasMedicas/5
        // Aquí hago un borrado lógico (Activo = false)
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var dbDep = await _db.DependenciasMedica.FirstOrDefaultAsync(d => d.Id == id);
            if (dbDep is null)
                return NotFound(new { message = "Dependencia médica no encontrada" });

            dbDep.Activo = false;
            await _db.SaveChangesAsync();

            // Si quisieras borrado físico:
            // _db.DependenciasMedicas.Remove(dbDep);
            // await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}
