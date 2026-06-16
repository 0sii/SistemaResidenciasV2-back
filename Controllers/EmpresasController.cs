using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApiVinculacionProyectosV2.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace WebApiVinculacionProyectosV2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmpresasController : ControllerBase
    {
        private readonly ResidenciasDbContext _context;

        public EmpresasController(ResidenciasDbContext context)
        {
            _context = context;
        }

        // GET: api/Empresas
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Empresas>>> GetEmpresas()
        {
            return await _context.Empresas.ToListAsync();
        }

        // GET: api/Empresas/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Empresas>> GetEmpresa(int id)
        {
            var empresa = await _context.Empresas.FindAsync(id);

            if (empresa == null)
            {
                return NotFound();
            }

            return empresa;
        }

        // PUT: api/Empresas/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutEmpresa(int id, Empresas empresa)
        {
            if (id != empresa.Id)
            {
                return BadRequest("El id de la URL no coincide con el del cuerpo.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.Entry(empresa).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EmpresaExists(id))
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

        // POST: api/Empresas
        [HttpPost]
        public async Task<ActionResult<Empresas>> PostEmpresa(Empresas empresa)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.Empresas.Add(empresa);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEmpresa), new { id = empresa.Id }, empresa);
        }

        // DELETE: api/Empresas/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEmpresa(int id)
        {
            var empresa = await _context.Empresas.FindAsync(id);
            if (empresa == null)
            {
                return NotFound();
            }

            _context.Empresas.Remove(empresa);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool EmpresaExists(int id)
        {
            return _context.Empresas.Any(e => e.Id == id);
        }
    }
}
