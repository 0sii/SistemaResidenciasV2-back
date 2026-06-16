using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApiVinculacionProyectosV2.Models;
using WebApiVinculacionProyectosV2.Models.DTOs;

namespace WebApiVinculacionProyectosV2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EgresadosController : ControllerBase
    {
        private readonly ResidenciasDbContext _db;

        private const int    ESTADO_FINALIZADO_ID = 8;
        private const string CLAVE_ASESOR         = "ASESOR_INTERNO";
        private const string CLAVE_REVISOR        = "REVISOR_ANTEPROYECTO";

        public EgresadosController(ResidenciasDbContext db) => _db = db;

        // ── GET /api/Egresados ────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetEgresados(
            [FromQuery] string? search,
            [FromQuery] int?    idPeriodo,
            [FromQuery] int     page     = 1,
            [FromQuery] int     pageSize = 20)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 200) pageSize = 20;

            var tiposRel = await _db.TipoRelacionDocenteProyecto.AsNoTracking().ToListAsync();

            var idAsesor  = tiposRel.FirstOrDefault(t =>
                t.Clave.Equals(CLAVE_ASESOR,  StringComparison.OrdinalIgnoreCase))?.Id;
            var idRevisor = tiposRel.FirstOrDefault(t =>
                t.Clave.Equals(CLAVE_REVISOR, StringComparison.OrdinalIgnoreCase))?.Id;

            var query =
                from e in _db.Estudiantes.AsNoTracking()
                join p in _db.Proyectos.AsNoTracking() on e.idProyecto equals p.Id
                where p.idEstado == ESTADO_FINALIZADO_ID
                join pa in _db.PeriodosAcademicos.AsNoTracking()
                    on p.IdPeriodoAcademico equals pa.Id into paGroup
                from pa in paGroup.DefaultIfEmpty()
                join car in _db.Carreras.AsNoTracking()
                    on e.idcarrera equals car.Id into carGroup
                from car in carGroup.DefaultIfEmpty()
                join emp in _db.Empresas.AsNoTracking()
                    on p.IdEmpresa equals emp.Id into empGroup
                from emp in empGroup.DefaultIfEmpty()
                join mod in _db.Modalidad.AsNoTracking()
                    on p.idModalidad equals mod.id into modGroup
                from mod in modGroup.DefaultIfEmpty()
                select new
                {
                    IdEstudiante    = e.id,
                    Nombre          = e.Nombre,
                    ApellidoPaterno = e.ApellidoPaterno,
                    ApellidoMaterno = e.ApellidoMaterno,
                    NoControl       = e.noControl,
                    CorreoPersonal  = e.correoPersonal,
                    TelefonoCelular = e.telefonoCelular,
                    IdProyecto      = p.Id,
                    TituloProyecto  = p.Titulo,
                    Descripcion     = p.Descripcion,
                    IdPeriodo       = pa != null ? (int?)pa.Id : null,
                    Periodo         = pa != null ? pa.Nombre   : null,
                    Carrera         = car != null ? car.Descripcion : null,
                    Empresa         = emp != null ? emp.Nombre      : null,
                    EmpresaCorreo    = emp != null ? emp.Email    : null,
                    EmpresaTelefono  = emp != null ? emp.Telefono : null,
                    EmpresaDireccion = emp != null ? emp.Domicilio : null,
                    Modalidad       = mod != null ? mod.Descripcion : null,
                };

            if (idPeriodo.HasValue && idPeriodo.Value > 0)
                query = query.Where(x => x.IdPeriodo == idPeriodo.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(x =>
                    x.Nombre.ToLower().Contains(s)          ||
                    x.ApellidoPaterno.ToLower().Contains(s) ||
                    x.ApellidoMaterno.ToLower().Contains(s) ||
                    (x.NoControl      != null && x.NoControl.ToLower().Contains(s))      ||
                    (x.TituloProyecto != null && x.TituloProyecto.ToLower().Contains(s)) ||
                    (x.Empresa        != null && x.Empresa.ToLower().Contains(s)));
            }

            var total = await query.CountAsync();

            var rows = await query
                .OrderBy(x => x.ApellidoPaterno)
                .ThenBy(x => x.Nombre)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var proyectoIds  = rows.Select(r => r.IdProyecto).Distinct().ToList();
            var docRelaciones = await _db.ProyectoDocente.AsNoTracking()
                .Where(pd => proyectoIds.Contains(pd.idProyecto))
                .ToListAsync();

            var docenteIds = docRelaciones.Select(d => d.idDocente).Distinct().ToList();
            var docentes   = await _db.Docentes.AsNoTracking()
                .Where(d => docenteIds.Contains(d.Id))
                .ToDictionaryAsync(d => d.Id,
                    d => $"{d.Nombre} {d.ApellidoPaterno} {d.ApellidoMaterno}".Trim());

            var items = rows.Select(r =>
            {
                var relAsesor  = idAsesor.HasValue
                    ? docRelaciones.FirstOrDefault(pd =>
                        pd.idProyecto == r.IdProyecto && pd.IdTipoRelacion == idAsesor.Value)
                    : null;
                var relRevisor = idRevisor.HasValue
                    ? docRelaciones.FirstOrDefault(pd =>
                        pd.idProyecto == r.IdProyecto && pd.IdTipoRelacion == idRevisor.Value)
                    : null;

                return new EgresadoDto
                {
                    IdEstudiante        = r.IdEstudiante,
                    NombreCompleto      = $"{r.Nombre} {r.ApellidoPaterno} {r.ApellidoMaterno}".Trim(),
                    NoControl           = r.NoControl,
                    CorreoPersonal      = r.CorreoPersonal,
                    Telefono            = r.TelefonoCelular,
                    IdProyecto          = r.IdProyecto,
                    TituloProyecto      = r.TituloProyecto,
                    DescripcionProyecto = r.Descripcion,
                    Periodo             = r.Periodo,
                    Carrera             = r.Carrera,
                    Empresa             = r.Empresa,
                    EmpresaCorreo       = r.EmpresaCorreo,
                    EmpresaTelefono     = r.EmpresaTelefono,
                    EmpresaDireccion    = r.EmpresaDireccion,
                    Modalidad           = r.Modalidad,
                    Asesor  = relAsesor  != null && docentes.TryGetValue(relAsesor.idDocente,  out var a)  ? a  : null,
                    Revisor = relRevisor != null && docentes.TryGetValue(relRevisor.idDocente, out var rv) ? rv : null,
                };
            }).ToList();

            return Ok(new { total, page, pageSize, items });
        }
    }
}
