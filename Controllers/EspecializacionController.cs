using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApiVinculacionProyectosV2.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApiVinculacionProyectosV2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EspecializacionesController : ControllerBase
    {
        private readonly ResidenciasDbContext _context;

        public EspecializacionesController(ResidenciasDbContext context)
        {
            _context = context;
        }

        // GET: api/Especializaciones
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Especializacion>>> GetEspecializaciones()
        {
            return await _context.Especializacion.ToListAsync();
        }

        // GET: api/Especializaciones/activas
        [HttpGet("activas")]
        public async Task<ActionResult<IEnumerable<Especializacion>>> GetEspecializacionesActivas()
        {
            return await _context.Especializacion
                                 .Where(e => e.Activo)
                                 .ToListAsync();
        }

        // GET: api/Especializaciones/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Especializacion>> GetEspecializacion(int id)
        {
            var especializacion = await _context.Especializacion.FindAsync(id);

            if (especializacion == null)
            {
                return NotFound();
            }

            return especializacion;
        }

        // PUT: api/Especializaciones/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> PutEspecializacion(int id, Especializacion especializacion)
        {
            if (id != especializacion.id)
            {
                return BadRequest("El id de la URL no coincide con el del cuerpo.");
            }

            _context.Entry(especializacion).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EspecializacionExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Especializaciones
        [HttpPost]
        public async Task<ActionResult<Especializacion>> PostEspecializacion(Especializacion especializacion)
        {
            // Si no te mandan Activo, lo puedes forzar a true por defecto
            if (!especializacion.Activo)
            {
                especializacion.Activo = true;
            }

            _context.Especializacion.Add(especializacion);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetEspecializacion),
                new { id = especializacion.id },
                especializacion
            );
        }

        // DELETE: api/Especializaciones/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteEspecializacion(int id)
        {
            var especializacion = await _context.Especializacion.FindAsync(id);
            if (especializacion == null)
            {
                return NotFound();
            }

            // Soft delete: marcar como inactiva
            especializacion.Activo = false;
            _context.Entry(especializacion).Property(e => e.Activo).IsModified = true;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool EspecializacionExists(int id)
        {
            return _context.Especializacion.Any(e => e.id == id);
        }
    }
}
