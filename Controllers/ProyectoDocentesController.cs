using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApiVinculacionProyectosV2.Models;

namespace WebApiVinculacionProyectosV2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProyectoDocentesController : ControllerBase
    {
    private readonly ResidenciasDbContext _db;
    public ProyectoDocentesController(ResidenciasDbContext db) => _db = db;

    // GET: api/ProyectoDocentes/mis-proyectos?idUsuario=5&idTipoRelacion=2
    [HttpGet("mis-proyectos")]
    public async Task<IActionResult> MisProyectos([FromQuery] int idUsuario, [FromQuery] int? idTipoRelacion = null)
    {
        if (idUsuario <= 0) return BadRequest(new { message = "idUsuario inválido" });

        var docente = await _db.Docentes.AsNoTracking()
            .FirstOrDefaultAsync(d => d.idUsuario == idUsuario);

        if (docente is null) return NotFound(new { message = "No existe docente para ese idUsuario" });

        var q = from pd in _db.ProyectoDocente.AsNoTracking()
                join p in _db.Proyectos.AsNoTracking() on pd.idProyecto equals p.Id
                join tr in _db.TipoRelacionDocenteProyecto.AsNoTracking() on pd.IdTipoRelacion equals tr.Id
                where pd.idDocente == docente.Id
                select new
                {
                    pd.idProyecto,
                    p.Titulo,
                    p.Descripcion,
                    p.Objetivo,
                    p.IdPeriodoAcademico,
                    p.idEstado,
                    p.PropuestaAlumno,
                    pd.IdTipoRelacion,
                    TipoRelacionClave = tr.Clave,
                    TipoRelacionDescripcion = tr.Descripcion,
                    pd.FechaInscripcion
                };

        if (idTipoRelacion.HasValue && idTipoRelacion.Value > 0)
            q = q.Where(x => x.IdTipoRelacion == idTipoRelacion.Value);

        var data = await q
            .OrderByDescending(x => x.FechaInscripcion)
            .ToListAsync();

        return Ok(data);
    }

    // GET: api/ProyectoDocentes/proyecto-docente?idUsuario=5&idProyecto=10
    [HttpGet("proyecto-docente")]
    public async Task<IActionResult> ProyectoDocenteView([FromQuery] int idUsuario, [FromQuery] int idProyecto)
    {
        if (idUsuario <= 0) return BadRequest(new { message = "idUsuario inválido" });
        if (idProyecto <= 0) return BadRequest(new { message = "idProyecto inválido" });

        var docente = await _db.Docentes.AsNoTracking()
            .FirstOrDefaultAsync(d => d.idUsuario == idUsuario);

        if (docente is null) return NotFound(new { message = "No existe docente para ese idUsuario" });

        var relacion = await (
            from pd in _db.ProyectoDocente.AsNoTracking()
            join tr in _db.TipoRelacionDocenteProyecto.AsNoTracking() on pd.IdTipoRelacion equals tr.Id
            where pd.idDocente == docente.Id && pd.idProyecto == idProyecto
            select new
            {
                pd.idProyecto,
                pd.idDocente,
                pd.IdTipoRelacion,
                TipoRelacionClave = tr.Clave,
                TipoRelacionDescripcion = tr.Descripcion,
                pd.FechaInscripcion
            }
        ).FirstOrDefaultAsync();
                
        if (relacion is null)
            return Forbid(); // no está asignado => no debe ver nada

        var proyecto = await _db.Proyectos.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == idProyecto);

        if (proyecto is null) return NotFound(new { message = "Proyecto no encontrado" });

        // ✅ AJUSTA este query a tu modelo real de Estudiantes:
        // aquí asumo Estudiantes.IdProyecto existe, porque tu sidebar lo usa.
        //var estudiantes = await _db.Estudiantes.AsNoTracking()
        //    .Where(e => e.idProyecto == idProyecto)
        //    .Select(e => new
        //    {
        //        e.id,
        //        e.Nombre,
        //        e.ApellidoPaterno,
        //        e.ApellidoMaterno,
        //        e.noControl,
        //        e.correoPersonal
        //    })
        //    .ToListAsync();

            var estudiantes = await _db.Estudiantes
                .Where(e => e.idProyecto == idProyecto)
                .GroupJoin(
                    _db.Usuarios,
                    e => e.idUsuario,    // 👈 FK en Estudiantes
                    u => u.Id,           // 👈 PK en Usuarios
                    (e, users) => new { e, users }
                )
                .SelectMany(
                    x => x.users.DefaultIfEmpty(), // LEFT JOIN
                    (x, u) => new
                    {
                        x.e.id,
                        x.e.Nombre,
                        x.e.ApellidoPaterno,
                        x.e.ApellidoMaterno,
                        x.e.noControl,

                        // 👇 Ajusta el nombre real del campo
                        correo = u != null ? u.Correo : null
                    }
                )
                .ToListAsync();

            return Ok(new
        {
            relacion,
            proyecto,
            estudiantes,
            anteproyectos = new object[] { } // placeholder hasta que exista entidad/endpoint real
        });
    }
}
}
