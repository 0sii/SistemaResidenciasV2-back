using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApiVinculacionProyectosV2.Models;

// DTOs para validación masiva
public class DocenteExistsBulkRequest
{
    public List<string> correos { get; set; } = new();
    public List<string> rfcs { get; set; } = new();
}

public class DocenteExistsBulkResponse
{
    public List<string> correosExistentes { get; set; } = new();
    public List<string> rfcsExistentes { get; set; } = new();
}

public class DocenteCargaResumenDto
{
    public int IdDocente { get; set; }
    public int IdUsuario { get; set; }
    public string Correo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;
    public string ApellidoPaterno { get; set; } = string.Empty;
    public string ApellidoMaterno { get; set; } = string.Empty;

    public int AsesorInternoCount { get; set; }
    public int RevisorResidenciaCount { get; set; }
    public int RevisorAnteproyectoCount { get; set; }
    public int TotalActivos { get; set; }
}

namespace WebApiVinculacionProyectosV2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocentesController : ControllerBase
    {
        private readonly ResidenciasDbContext _db;
        public DocentesController(ResidenciasDbContext db) => _db = db;

        // GET: api/Docentes
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await (
                from d in _db.Docentes.AsNoTracking()
                join u in _db.Usuarios.AsNoTracking() on d.idUsuario equals u.Id
                select new
                {
                    d.Id,
                    d.idUsuario,
                    u.Correo,
                    d.Nombre,
                    d.ApellidoPaterno,
                    d.ApellidoMaterno, // <- nombre según tu modelo
                    d.RFC,
                    d.Telefono,
                    d.NivelAcademico,
                    d.EsJefeDepartamento
                }).ToListAsync();

            return Ok(data);
        }

        // GET: api/Docentes/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var data = await (
                from d in _db.Docentes.AsNoTracking()
                join u in _db.Usuarios.AsNoTracking() on d.idUsuario equals u.Id
                where d.Id == id
                select new
                {
                    d.Id,
                    d.idUsuario,
                    u.Correo,
                    d.Nombre,
                    d.ApellidoPaterno,
                    d.ApellidoMaterno,
                    d.RFC,
                    d.Telefono,
                    d.NivelAcademico,
                    d.EsJefeDepartamento
                }).FirstOrDefaultAsync();

            return data is null ? NotFound() : Ok(data);
        }

        // GET: api/Docentes/idUsuario/5
        [HttpGet("idUsuario/{idUsuario:int}")]
        public async Task<IActionResult> GetByIdUsuario([FromRoute] int idUsuario)
        {
            var data = await (
                from d in _db.Docentes.AsNoTracking()
                join u in _db.Usuarios.AsNoTracking() on d.idUsuario equals u.Id
                where d.idUsuario == idUsuario
                select new
                {
                    d.Id,
                    d.idUsuario,
                    u.Correo,
                    d.Nombre,
                    d.ApellidoPaterno,
                    d.ApellidoMaterno,
                    d.RFC,
                    d.Telefono,
                    d.NivelAcademico,
                    d.EsJefeDepartamento
                }
            ).FirstOrDefaultAsync();

            return data is null ? NotFound() : Ok(data);
        }
    
        // POST: api/Docentes
        // Recibe Docente; idUsuario debe existir
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Docentes docente)
        {
            if (docente is null)
                return BadRequest(new { message = "Modelo Docente requerido" });

            if (string.IsNullOrWhiteSpace(docente.Nombre) ||
                string.IsNullOrWhiteSpace(docente.ApellidoPaterno) ||
                string.IsNullOrWhiteSpace(docente.ApellidoMaterno))
                return BadRequest(new { message = "Nombre y Apellidos son obligatorios" });

            // ── Evitar duplicados ──────────────────────────────────────────
            // 1) Un usuario solo puede tener UN perfil docente
            var existePorUsuario = await _db.Docentes
                .AnyAsync(d => d.idUsuario == docente.idUsuario);
            if (existePorUsuario)
                return Conflict(new
                {
                    campo = "idUsuario",
                    message = $"Ya existe un docente registrado para el usuario {docente.idUsuario}."
                });

            // 2) RFC único (solo si se envía)
            if (!string.IsNullOrWhiteSpace(docente.RFC))
            {
                var rfcNorm = docente.RFC.Trim().ToUpper();
                var existePorRfc = await _db.Docentes
                    .AnyAsync(d => d.RFC != null && d.RFC.ToUpper() == rfcNorm);
                if (existePorRfc)
                    return Conflict(new
                    {
                        campo = "RFC",
                        message = $"El RFC '{rfcNorm}' ya está registrado en otro docente."
                    });

                docente.RFC = rfcNorm; // normalizar mayúsculas al guardar
            }
            // ──────────────────────────────────────────────────────────────

            _db.Docentes.Add(docente);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = docente.Id }, docente);
        }

        // PUT: api/Docentes/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] Docentes docente)
        {
            var dbD = await _db.Docentes.FirstOrDefaultAsync(x => x.Id == id);
            if (dbD is null) return NotFound();

            if (docente is null)
                return BadRequest(new { message = "Modelo Docente requerido" });

            if (string.IsNullOrWhiteSpace(docente.Nombre) ||
                string.IsNullOrWhiteSpace(docente.ApellidoPaterno) ||
                string.IsNullOrWhiteSpace(docente.ApellidoMaterno))
                return BadRequest(new { message = "Nombre y Apellidos son obligatorios" });

            // Validar cambio de FK idUsuario (opcional)
            if (dbD.idUsuario != docente.idUsuario)
            {
                var existsUser = await _db.Usuarios.AnyAsync(u => u.Id == docente.idUsuario);
                if (!existsUser) return BadRequest(new { message = "Nuevo idUsuario no existe" });
                dbD.idUsuario = docente.idUsuario;
            }

            // ── Evitar duplicado de RFC al editar ─────────────────────────
            if (!string.IsNullOrWhiteSpace(docente.RFC))
            {
                var rfcNorm = docente.RFC.Trim().ToUpper();
                var rfcEnOtro = await _db.Docentes
                    .AnyAsync(d => d.Id != id && d.RFC != null && d.RFC.ToUpper() == rfcNorm);
                if (rfcEnOtro)
                    return Conflict(new
                    {
                        campo = "RFC",
                        message = $"El RFC '{rfcNorm}' ya está registrado en otro docente."
                    });

                docente.RFC = rfcNorm; // normalizar mayúsculas al guardar
            }
            // ──────────────────────────────────────────────────────────────

            // Actualizar campos del modelo
            dbD.Nombre = docente.Nombre;
            dbD.ApellidoPaterno = docente.ApellidoPaterno;
            dbD.ApellidoMaterno = docente.ApellidoMaterno;
            dbD.RFC = docente.RFC;
            dbD.Telefono = docente.Telefono;
            dbD.NivelAcademico = docente.NivelAcademico;
            dbD.EsJefeDepartamento = docente.EsJefeDepartamento;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/Docentes/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var dbD = await _db.Docentes.FindAsync(id);
            if (dbD is null) return NotFound();

            _db.Docentes.Remove(dbD);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // GET: api/Docentes/{idDocente}/suscripciones
        [HttpGet("{idDocente:int}/suscripciones")]
        public async Task<IActionResult> GetSuscripcionesDocente(int idDocente)
        {
            var docente = await _db.Docentes.AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == idDocente);

            if (docente is null)
                return NotFound(new { message = "Docente no encontrado" });

            // 🔥 Estados cerrados (según tu catálogo)
            var closedStates = new[] { 8, 9 }; // Finalizado, Cancelado

            var rows = await (
                from pd in _db.ProyectoDocente.AsNoTracking()
                join p in _db.Proyectos.AsNoTracking() on pd.idProyecto equals p.Id
                join tr in _db.TipoRelacionDocenteProyecto.AsNoTracking() on pd.IdTipoRelacion equals tr.Id

                // LEFT join Empresa (si existe tu DbSet)
                join e0 in _db.Empresas.AsNoTracking() on p.IdEmpresa equals e0.Id into ee
                from e in ee.DefaultIfEmpty()

                    // LEFT join Periodo (ajusta DbSet si se llama distinto)
                join per0 in _db.PeriodosAcademicos.AsNoTracking() on p.IdPeriodoAcademico equals per0.Id into pp
                from per in pp.DefaultIfEmpty()

                    // ✅ JOIN estado (AJUSTA el DbSet y campos a tu modelo real)
                join est0 in _db.Estado.AsNoTracking() on p.idEstado equals est0.Id into ests
                from est in ests.DefaultIfEmpty()

                where pd.idDocente == idDocente
                      // ✅ filtro principal: no finalizado/cancelado
                      && (p.idEstado == null || !closedStates.Contains(p.idEstado.Value))

                select new
                {
                    pd.idProyecto,
                    ProyectoTitulo = p.Titulo,
                    ProyectoDescripcion = p.Descripcion,
                    ProyectoObjetivo = p.Objetivo,

                    p.IdPeriodoAcademico,
                    PeriodoNombre = per != null ? per.Nombre : null,

                    EmpresaId = p.IdEmpresa,
                    EmpresaNombre = e != null ? e.Nombre : null,

                    p.idEstado,
                    EstadoDescripcion = est != null ? est.Descripcion : null,

                    pd.IdTipoRelacion,
                    TipoRelacionClave = tr.Clave,
                    TipoRelacionDescripcion = tr.Descripcion,

                    pd.FechaInscripcion
                }
            )
            .OrderByDescending(x => x.FechaInscripcion)
            .ToListAsync();

            // ✅ Clasificación EXACTA por clave (según tu tabla)
            var asesor = rows.Where(x => x.TipoRelacionClave == "ASESOR_INTERNO").ToList();
            var revisor = rows.Where(x => x.TipoRelacionClave == "REVISOR_RESIDENCIA").ToList();
            var revisorAnte = rows.Where(x => x.TipoRelacionClave == "REVISOR_ANTEPROYECTO").ToList();

            return Ok(new
            {
                docente = new { docente.Id, docente.idUsuario, docente.Nombre, docente.ApellidoPaterno, docente.ApellidoMaterno },
                asesor,
                revisor,
                revisorAnteproyecto = revisorAnte,
                total = rows.Count
            });
        }

        // GET: api/ProyectoDocentes/mis-proyectos?idUsuario=5&idTipoRelacion=2&includeClosed=false
        [HttpGet("mis-proyectos")]
        public async Task<IActionResult> MisProyectos(
            [FromQuery] int idUsuario,
            [FromQuery] int? idTipoRelacion = null,
            [FromQuery] bool includeClosed = false)
        {
            if (idUsuario <= 0) return BadRequest(new { message = "idUsuario inválido" });

            var docente = await _db.Docentes.AsNoTracking()
                .FirstOrDefaultAsync(d => d.idUsuario == idUsuario);

            if (docente is null) return NotFound(new { message = "No existe docente para ese idUsuario" });

            var closedStates = new[] { 8, 9 }; // Finalizado, Cancelado

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

            // ✅ filtro por defecto
            if (!includeClosed)
                q = q.Where(x => x.idEstado == null || !closedStates.Contains(x.idEstado.Value));

            var data = await q
                .OrderByDescending(x => x.FechaInscripcion)
                .ToListAsync();

            return Ok(data);
        }


        [HttpGet("exists")]
        public async Task<IActionResult> Exists([FromQuery] string? correo = null, [FromQuery] string? rfc = null)
        {
            if (string.IsNullOrWhiteSpace(correo) && string.IsNullOrWhiteSpace(rfc))
                return BadRequest(new { message = "Debes enviar correo o rfc." });

            bool exists = false;

            if (!string.IsNullOrWhiteSpace(correo))
            {
                var c = correo.Trim().ToLower();
                exists = await (
                    from d in _db.Docentes.AsNoTracking()
                    join u in _db.Usuarios.AsNoTracking() on d.idUsuario equals u.Id
                    where u.Correo.ToLower() == c
                    select d.Id
                ).AnyAsync();
            }

            if (!exists && !string.IsNullOrWhiteSpace(rfc))
            {
                var r = rfc.Trim().ToUpper();
                exists = await _db.Docentes.AsNoTracking().AnyAsync(d => d.RFC != null && d.RFC.ToUpper() == r);
            }

            return Ok(new { exists });
        }

        // POST: api/Docentes/exists-bulk
        // Recibe listas de correos/RFC y regresa cuáles ya existen
        [HttpPost("exists-bulk")]
        public async Task<IActionResult> ExistsBulk([FromBody] DocenteExistsBulkRequest req)
        {
            if (req == null) return BadRequest(new { message = "Body requerido" });

            var correos = (req.correos ?? new())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim().ToLower())
                .Distinct()
                .ToList();

            var rfcs = (req.rfcs ?? new())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim().ToUpper())
                .Distinct()
                .ToList();

            // Buscar correos existentes (Docente ligado a Usuario)
            var correosExistentes = await (
                from d in _db.Docentes.AsNoTracking()
                join u in _db.Usuarios.AsNoTracking() on d.idUsuario equals u.Id
                where correos.Contains(u.Correo.ToLower())
                select u.Correo.ToLower()
            ).Distinct().ToListAsync();

            // Buscar RFCs existentes (en tabla Docentes)
            var rfcsExistentes = await _db.Docentes.AsNoTracking()
                .Where(d => d.RFC != null && rfcs.Contains(d.RFC.ToUpper()))
                .Select(d => d.RFC!.ToUpper())
                .Distinct()
                .ToListAsync();

            var resp = new DocenteExistsBulkResponse
            {
                correosExistentes = correosExistentes,
                rfcsExistentes = rfcsExistentes
            };

            return Ok(resp);
        }

        // GET: api/Docentes/cargas-resumen
[HttpGet("cargas-resumen")]
public async Task<IActionResult> GetCargasResumen()
{
    var closedStates = new[] { 8, 9 }; // Finalizado, Cancelado

    var rows = await (
        from d in _db.Docentes.AsNoTracking()
        join u in _db.Usuarios.AsNoTracking() on d.idUsuario equals u.Id
        join pd0 in _db.ProyectoDocente.AsNoTracking() on d.Id equals pd0.idDocente into pdg
        from pd in pdg.DefaultIfEmpty()
        join tr0 in _db.TipoRelacionDocenteProyecto.AsNoTracking() on pd.IdTipoRelacion equals tr0.Id into trg
        from tr in trg.DefaultIfEmpty()
        join p0 in _db.Proyectos.AsNoTracking() on pd.idProyecto equals p0.Id into pg
        from p in pg.DefaultIfEmpty()
        where pd == null || p == null || p.idEstado == null || !closedStates.Contains(p.idEstado.Value)
        select new
        {
            d.Id,
            d.idUsuario,
            u.Correo,
            d.Nombre,
            d.ApellidoPaterno,
            d.ApellidoMaterno,
            TipoRelacionClave = tr != null ? tr.Clave : null
        }
    ).ToListAsync();

    var data = rows
        .GroupBy(x => new
        {
            x.Id,
            x.idUsuario,
            x.Correo,
            x.Nombre,
            x.ApellidoPaterno,
            x.ApellidoMaterno
        })
        .Select(g =>
        {
            var asesor = g.Count(x => x.TipoRelacionClave == "ASESOR_INTERNO");
            var revisor = g.Count(x => x.TipoRelacionClave == "REVISOR_RESIDENCIA");
            var revisorAnte = g.Count(x => x.TipoRelacionClave == "REVISOR_ANTEPROYECTO");

            return new DocenteCargaResumenDto
            {
                IdDocente = g.Key.Id,
                IdUsuario = g.Key.idUsuario,
                Correo = g.Key.Correo,
                Nombre = g.Key.Nombre,
                ApellidoPaterno = g.Key.ApellidoPaterno,
                ApellidoMaterno = g.Key.ApellidoMaterno,
                AsesorInternoCount = asesor,
                RevisorResidenciaCount = revisor,
                RevisorAnteproyectoCount = revisorAnte,
                TotalActivos = asesor + revisor + revisorAnte
            };
        })
        .OrderBy(x => x.ApellidoPaterno)
        .ThenBy(x => x.ApellidoMaterno)
        .ThenBy(x => x.Nombre)
        .ToList();

    return Ok(data);
}

    }
}
