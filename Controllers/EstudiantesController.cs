using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApiVinculacionProyectosV2.Models;

// DTOs para validación masiva
public class ExistsBulkRequest
{
    public List<string> NoControles { get; set; } = new();
    public List<string> Correos { get; set; } = new();
}

public class ExistsBulkResponse
{
    public List<string> NoControlesExistentes { get; set; } = new();
    public List<string> CorreosExistentes { get; set; } = new();
}

namespace WebApiVinculacionProyectosV2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EstudiantesController : ControllerBase
    {
        private readonly ResidenciasDbContext _db;
        public EstudiantesController(ResidenciasDbContext db) => _db = db;

        private static string NormalizeNoControl(string? v) =>
    string.IsNullOrWhiteSpace(v) ? "" : v.Trim().ToUpperInvariant();

        // GET: api/Estudiantes
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await (
                from e in _db.Estudiantes.AsNoTracking()
                join u in _db.Usuarios.AsNoTracking() on e.idUsuario equals u.Id
                select new
                {
                    e.id,
                    e.idUsuario,
                    u.Correo,
                    e.Nombre,
                    e.ApellidoPaterno,
                    e.ApellidoMaterno, // <- nombre según tu modelo
                    e.noControl,
                    e.correoPersonal,
                    e.telefonoCelular
                }).ToListAsync();

            return Ok(data);
        }

        // GET: api/Estudiantes/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var data = await (
                from e in _db.Estudiantes.AsNoTracking()
                join u in _db.Usuarios.AsNoTracking() on e.idUsuario equals u.Id
                where e.id == id
                select new
                {
                    e.id,
                    e.idUsuario,
                    e.idProyecto,
                    u.Correo,
                    e.Nombre,
                    e.ApellidoPaterno,
                    e.ApellidoMaterno,
                    e.noControl,
                    e.correoPersonal,
                    e.telefonoCelular,
                    e.idcarrera,
                    e.domicilio,
                    e.ciudad,
                    e.cp,
                    e.noSeguroSocial,
                    e.idDependenciaMedica,
                    e.idContactoEmergencia
                }).FirstOrDefaultAsync();

            return data is null ? NotFound() : Ok(data);
        }



        // GET: api/Estudiantes/5
        [HttpGet("idUsuario/{idUsuario:int}")]
        public async Task<IActionResult> GetByIdUsuario([FromRoute] int idUsuario)
        {
            var data = await (
                from e in _db.Estudiantes.AsNoTracking()
                join u in _db.Usuarios.AsNoTracking() on e.idUsuario equals u.Id
                where e.idUsuario == idUsuario
                select new
                {
                    e.id,
                    e.idUsuario,
                    e.idProyecto,
                    u.Correo,
                    e.Nombre,
                    e.ApellidoPaterno,
                    e.ApellidoMaterno,
                    e.noControl,
                    e.correoPersonal,
                    e.telefonoCelular,
                    e.idcarrera,
                    e.domicilio,
                    e.ciudad,
                    e.cp,
                    e.noSeguroSocial,
                    e.idDependenciaMedica,
                    e.idContactoEmergencia
                }).FirstOrDefaultAsync();

            return data is null ? NotFound() : Ok(data);
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Estudiantes estudiante)
        {
            if (estudiante is null)
                return BadRequest(new { message = "Modelo Estudiante requerido" });

            if (string.IsNullOrWhiteSpace(estudiante.Nombre) ||
                string.IsNullOrWhiteSpace(estudiante.ApellidoPaterno) ||
                string.IsNullOrWhiteSpace(estudiante.ApellidoMaterno))
                return BadRequest(new { message = "Nombre y Apellidos son obligatorios" });

            // --- CP (nuevo) ---
            estudiante.cp = NormalizeCp(estudiante.cp);
            if (!IsValidCp(estudiante.cp))
                return BadRequest(new { field = "cp", message = "El CP debe tener exactamente 5 dígitos." });


            // --- Validación de unicidad noControl ---
            var normalizedNoCtrl = NormalizeNoControl(estudiante.noControl);
            if (await _db.Estudiantes.AnyAsync(e => e.noControl == normalizedNoCtrl))
                return Conflict(new { field = "noControl", message = "El No. de control ya está registrado." });

            estudiante.noControl = normalizedNoCtrl;

            try
            {
                _db.Estudiantes.Add(estudiante);
                await _db.SaveChangesAsync();

                return CreatedAtAction(nameof(GetById), new { id = estudiante.id }, estudiante);
            }
            catch (DbUpdateException ex)
            {
                // Aquí normalmente viene el mensaje de FK, CHECK, etc.
                return StatusCode(500, new
                {
                    message = "Error al guardar el estudiante en la base de datos.",
                    error = ex.InnerException?.Message ?? ex.Message,
                    // si quieres ser MUY explícito en desarrollo, puedes agregar:
                    // stackTrace = ex.ToString()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error inesperado al guardar el estudiante.",
                    error = ex.Message
                });
            }
        }

        // PUT: api/Estudiantes/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] Estudiantes estudiante)
        {
            var dbE = await _db.Estudiantes.FirstOrDefaultAsync(x => x.id == id);
            if (dbE is null) return NotFound();

            if (estudiante is null)
                return BadRequest(new { message = "Modelo Estudiante requerido" });

            if (string.IsNullOrWhiteSpace(estudiante.Nombre) ||
                string.IsNullOrWhiteSpace(estudiante.ApellidoPaterno) ||
                string.IsNullOrWhiteSpace(estudiante.ApellidoMaterno))
                return BadRequest(new { message = "Nombre y Apellidos son obligatorios" });

            // Validar cambio de FK idUsuario (opcional)
            if (dbE.idUsuario != estudiante.idUsuario)
            {
                var exists = await _db.Usuarios.AnyAsync(u => u.Id == estudiante.idUsuario);
                if (!exists) return BadRequest(new { message = "Nuevo idUsuario no existe" });
                dbE.idUsuario = estudiante.idUsuario;
            }

            // --- Validación de unicidad noControl (excluyendo el propio id) ---
            var normalizedNoCtrl = NormalizeNoControl(estudiante.noControl);
            var dupNoCtrl = await _db.Estudiantes
                .AnyAsync(e => e.id != id && e.noControl == normalizedNoCtrl);
            if (dupNoCtrl)
                return Conflict(new { field = "noControl", message = "El No. de control ya está registrado." });

            // Actualizar campos
            dbE.Nombre = estudiante.Nombre;
            dbE.idProyecto = estudiante.idProyecto;
            dbE.ApellidoPaterno = estudiante.ApellidoPaterno;
            dbE.ApellidoMaterno = estudiante.ApellidoMaterno;
            dbE.idcarrera = estudiante.idcarrera;
            dbE.domicilio = estudiante.domicilio;
            dbE.ciudad = estudiante.ciudad;
            // --- CP (nuevo) ---
            var cpNorm = NormalizeCp(estudiante.cp);
            if (!IsValidCp(cpNorm))
                return BadRequest(new { field = "cp", message = "El CP debe tener exactamente 5 dígitos." });

            dbE.cp = cpNorm;

            dbE.noControl = normalizedNoCtrl;          // << aquí normalizado
            dbE.correoPersonal = estudiante.correoPersonal;
            dbE.noSeguroSocial = estudiante.noSeguroSocial;
            dbE.idDependenciaMedica = estudiante.idDependenciaMedica;
            dbE.telefonoCelular = estudiante.telefonoCelular;
            dbE.idContactoEmergencia = estudiante.idContactoEmergencia;

            await _db.SaveChangesAsync();
            return NoContent();
        }


        // DELETE: api/Estudiantes/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var dbE = await _db.Estudiantes.FindAsync(id);
            if (dbE is null) return NotFound();

            _db.Estudiantes.Remove(dbE);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // GET: api/Estudiantes/noControl/{noControl}
        // Si existe -> devuelve proyección; si no -> false
        [HttpGet("noControl/{noControl}")]
        public async Task<IActionResult> GetByNoControl(string noControl)
        {
            var normalized = NormalizeNoControl(noControl);
            if (string.IsNullOrWhiteSpace(normalized))
                return Ok(false);

            var data = await (
                from e in _db.Estudiantes.AsNoTracking()
                join u in _db.Usuarios.AsNoTracking() on e.idUsuario equals u.Id
                where e.noControl == normalized
                select new
                {
                    e.id,
                    e.idUsuario,
                    e.idProyecto,
                    u.Correo,
                    e.Nombre,
                    e.ApellidoPaterno,
                    e.ApellidoMaterno,
                    e.noControl,
                    e.correoPersonal,
                    e.telefonoCelular,
                    e.idcarrera,
                    e.domicilio,
                    e.ciudad,
                    e.cp,
                    e.noSeguroSocial,
                    e.idDependenciaMedica,
                    e.idContactoEmergencia
                }).FirstOrDefaultAsync();

            return data is null ? Ok(false) : Ok(data);
        }
        // GET: api/Estudiantes/proyecto/5
        // GET: api/Estudiantes/proyecto/123
        // GET: api/Estudiantes/proyecto/123
        [HttpGet("proyecto/{idProyecto:int}")]
        public async Task<IActionResult> GetByProyecto(int idProyecto)
        {
            var data = await (
                from e in _db.Estudiantes.AsNoTracking()
                join u in _db.Usuarios.AsNoTracking() on e.idUsuario equals u.Id
                where e.idProyecto == idProyecto
                select new
                {
                    id = e.id,
                    idUsuario = e.idUsuario,
                    idProyecto = e.idProyecto,

                    noControl = e.noControl,

                    nombre = e.Nombre,
                    apellidoPaterno = e.ApellidoPaterno,
                    apellidoMaterno = e.ApellidoMaterno,

                    // correo institucional (tabla Usuarios)
                    correo = u.Correo,

                    // opcional
                    correoPersonal = e.correoPersonal,
                    telefonoCelular = e.telefonoCelular
                }
            ).ToListAsync();

            return Ok(data);
        }


        private static string? NormalizeCp(string? v)
        {
            if (string.IsNullOrWhiteSpace(v)) return null;
            var cp = v.Trim();
            return cp;
        }

        private static bool IsValidCp(string? cp)
        {
            if (string.IsNullOrWhiteSpace(cp)) return true; // cp opcional
                                                            // 5 dígitos exactos
            return System.Text.RegularExpressions.Regex.IsMatch(cp.Trim(), @"^\d{5}$");
        }


        // POST: api/Estudiantes/exists-bulk
        // Valida en un solo viaje qué noControl y correos ya existen en BD
        [HttpPost("exists-bulk")]
        public async Task<IActionResult> ExistsBulk([FromBody] ExistsBulkRequest req)
        {
            req ??= new ExistsBulkRequest();

            var noControles = (req.NoControles ?? new List<string>())
                .Select(NormalizeNoControl)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            var correos = (req.Correos ?? new List<string>())
                .Select(x => (x ?? "").Trim().ToLowerInvariant())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            var existentesNoCtrl = new List<string>();
            var existentesCorreo = new List<string>();

            if (noControles.Count > 0)
            {
                existentesNoCtrl = await _db.Estudiantes
                    .AsNoTracking()
                    .Where(e => noControles.Contains(e.noControl))
                    .Select(e => e.noControl)
                    .Distinct()
                    .ToListAsync();
            }

            if (correos.Count > 0)
            {
                // Ajusta el campo si tu tabla Usuarios se llama distinto
                existentesCorreo = await _db.Usuarios
                    .AsNoTracking()
                    .Where(u => correos.Contains((u.Correo ?? "").Trim().ToLower()))
                    .Select(u => u.Correo)
                    .Distinct()
                    .ToListAsync();
            }

            var resp = new ExistsBulkResponse
            {
                NoControlesExistentes = existentesNoCtrl,
                CorreosExistentes = existentesCorreo
                    .Select(c => (c ?? "").Trim().ToLowerInvariant())
                    .Distinct()
                    .ToList()
            };

            return Ok(resp);
        }


        // GET: api/Estudiantes/existe-nocontrol?noControl=...
        [HttpGet("existe-nocontrol")]
        public async Task<IActionResult> ExisteNoControl([FromQuery] string noControl)
        {
            if (string.IsNullOrWhiteSpace(noControl))
                return Ok(new { exists = false });

            var norm = noControl.Trim().ToUpperInvariant();

            var existe = await _db.Estudiantes.AsNoTracking()
                .AnyAsync(e => e.noControl == norm);

            return Ok(new { exists = existe });
        }

        // GET: api/Estudiantes/libres
[HttpGet("libres")]
public async Task<IActionResult> GetLibres()
{
    var data = await (
        from e in _db.Estudiantes.AsNoTracking()
        join u in _db.Usuarios.AsNoTracking() on e.idUsuario equals u.Id
        where e.idProyecto == null || e.idProyecto == 0
        select new
        {
            id = e.id,
            idUsuario = e.idUsuario,
            idProyecto = e.idProyecto,

            noControl = e.noControl,

            nombre = e.Nombre,
            apellidoPaterno = e.ApellidoPaterno,
            apellidoMaterno = e.ApellidoMaterno,

            correo = u.Correo,

            correoPersonal = e.correoPersonal,
            telefonoCelular = e.telefonoCelular
        }
    ).ToListAsync();

    return Ok(data);
}


    }
}
