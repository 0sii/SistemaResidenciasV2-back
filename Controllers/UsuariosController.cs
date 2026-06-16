using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApiVinculacionProyectosV2.Models;
using WebApiVinculacionProyectosV2.Custom;
using System.Text.Json;


namespace WebApiVinculacionProyectosV2.Controllers
{
    [ApiController]
    [Route("api/Usuarios")]
    public class UsuariosController : ControllerBase
    {
        private readonly ResidenciasDbContext _db;
        private readonly Utilidades _utilidades;

        public UsuariosController(ResidenciasDbContext db, Utilidades utilidades)
        {
            _db = db;
            _utilidades = utilidades;
        }

        // DTO para UPDATE (coincide con lo que manda Angular)
        public class UsuarioUpdateDto
        {
            public string? Correo { get; set; }
            public bool Activo { get; set; }
            public string? Nombre { get; set; }
            public string? ApellidoPaterno { get; set; }
            public string? ApellidoMaterno { get; set; }
        }

        // =====================================================================
        // GET: api/Usuarios  → listado de usuarios (sin password)
        // =====================================================================
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _db.Usuarios
                .AsNoTracking()
                .OrderBy(u => u.Id)
                .Select(u => new
                {
                    u.Id,
                    u.Correo,
                    u.Activo,
                    u.Nombre,
                    u.ApellidoPaterno,
                    u.ApellidoMaterno
                })
                .ToListAsync();

            return Ok(data);
        }

        // =====================================================================
        // GET: api/Usuarios/{id}
        // =====================================================================
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var user = await _db.Usuarios
                .AsNoTracking()
                .Where(u => u.Id == id)
                .Select(u => new
                {
                    u.Id,
                    u.Correo,
                    u.Activo,
                    u.Nombre,
                    u.ApellidoPaterno,
                    u.ApellidoMaterno
                })
                .FirstOrDefaultAsync();

            return user is null ? NotFound(new { message = "Usuario no encontrado" }) : Ok(user);
        }

        // =====================================================================
        // GET: api/Usuarios/by-correo?correo=...
        // =====================================================================
        [HttpGet("by-correo")]
        public async Task<IActionResult> GetByCorreo([FromQuery] string correo)
        {
            if (string.IsNullOrWhiteSpace(correo))
                return Ok(false);

            var user = await _db.Usuarios
                .AsNoTracking()
                .Where(u => u.Correo == correo)
                .Select(u => new
                {
                    u.Id,
                    u.Correo,
                    u.Activo,
                    u.Nombre,
                    u.ApellidoPaterno,
                    u.ApellidoMaterno
                })
                .FirstOrDefaultAsync();

            return user is null ? Ok(false) : Ok(user);
        }

        // =====================================================================
        // GET: api/Usuarios/Roles?idUsuario=1  → roles de un usuario
        // usado por getRolesByUsuario(idUsuario)
        // =====================================================================
        [HttpGet("Roles")]
        public async Task<IActionResult> GetUsuarioRol([FromQuery] int idUsuario)
        {
            if (idUsuario <= 0)
                return BadRequest("idUsuario inválido");

            var roles = await (
                from ur in _db.UsuarioRol
                join r in _db.Rol on ur.IdRol equals r.Id
                where ur.IdUsuario == idUsuario
                select new
                {
                    id = r.Id,
                    descripcion = r.Descripcion,
                    activo = r.Activo
                }
            ).AsNoTracking().ToListAsync();

            if (roles == null || roles.Count == 0)
                return Ok(false);

            return Ok(roles);
        }

        // =====================================================================
        // GET: api/Usuarios/Permisos?idRol=1  → permisos de UN rol
        // usado por getPermisosByRol(idRol)
        // =====================================================================
        [HttpGet("Permisos")]
        public async Task<IActionResult> GetPermisosPorRol([FromQuery] int idRol)
        {
            if (idRol <= 0)
                return BadRequest("idRol inválido");

            var permisos = await (
                from rp in _db.RolPermiso
                join p in _db.Permisos on rp.idPermiso equals p.id
                where rp.idRol == idRol
                select new
                {
                    id = p.id,
                    descripcion = p.Descripcion,
                    activo = p.Activo
                }
            )
            .AsNoTracking()
            .ToListAsync();

            if (permisos == null || permisos.Count == 0)
                return Ok(false);

            return Ok(permisos);
        }

        // =====================================================================
        // GET: api/Usuarios/PermisosCatalogo  → TODOS los permisos
        // usado por getAllPermisos()
        // =====================================================================
        [HttpGet("PermisosCatalogo")]
        public async Task<IActionResult> GetPermisosCatalogo()
        {
            var permisos = await _db.Permisos
                .AsNoTracking()
                .OrderBy(p => p.Descripcion)
                .Select(p => new
                {
                    id = p.id,
                    descripcion = p.Descripcion,
                    activo = p.Activo
                })
                .ToListAsync();

            return Ok(permisos);
        }

        // =====================================================================
        // POST: api/Usuarios
        // =====================================================================
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Usuarios usuario)
        {
            if (usuario is null)
                return BadRequest(new { message = "Modelo Usuario requerido" });

            // ===== Normalización básica =====
            var correo = (usuario.Correo ?? "").Trim().ToLowerInvariant();
            var nombre = (usuario.Nombre ?? "").Trim();
            var apPaterno = (usuario.ApellidoPaterno ?? "").Trim();
            var apMaterno = (usuario.ApellidoMaterno ?? "").Trim();

            if (string.IsNullOrWhiteSpace(correo))
                return BadRequest(new { message = "El correo es obligatorio" });

            if (string.IsNullOrWhiteSpace(usuario.PasswordHash))
                return BadRequest(new { message = "La contraseña es obligatoria" });

            if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(apPaterno))
                return BadRequest(new { message = "Nombre y Apellido Paterno son obligatorios" });

            // ===== Validación 1: correo repetido (case-insensitive) =====
            var correoRepetido = await _db.Usuarios
                .AnyAsync(u => u.Correo.ToLower() == correo);

            if (correoRepetido)
                return BadRequest(new { message = "El correo ya está registrado" });

            // ===== Validación 2: validación compuesta (correo + nombre + apellidos) =====
            // Nota: correo ya quedó validado arriba, pero esta validación asegura el "conjunto"
            // por si en algún momento relajas la validación del correo o quieres dejar ambas reglas claras.
            var duplicadoCompuesto = await _db.Usuarios.AnyAsync(u =>
                u.Correo.ToLower() == correo
                && (u.Nombre ?? "").Trim().ToLower() == nombre.ToLower()
                && (u.ApellidoPaterno ?? "").Trim().ToLower() == apPaterno.ToLower()
                && (u.ApellidoMaterno ?? "").Trim().ToLower() == apMaterno.ToLower()
            );

            if (duplicadoCompuesto)
                return BadRequest(new
                {
                    message = "Ya existe un usuario con el mismo correo y nombre completo (Nombre + Apellidos)."
                });

            // ===== Persistencia con valores normalizados =====
            usuario.Correo = correo;
            usuario.Nombre = nombre;
            usuario.ApellidoPaterno = apPaterno;
            usuario.ApellidoMaterno = apMaterno;

            usuario.PasswordHash = _utilidades.encriptarSHA256(usuario.PasswordHash);

            _db.Usuarios.Add(usuario);
            await _db.SaveChangesAsync();

            var result = new
            {
                usuario.Id,
                usuario.Correo,
                usuario.Activo,
                usuario.Nombre,
                usuario.ApellidoPaterno,
                usuario.ApellidoMaterno
            };

            return CreatedAtAction(nameof(GetById), new { id = usuario.Id }, result);
        }

        // =====================================================================
        // PUT: api/Usuarios/{id}
        // =====================================================================
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UsuarioUpdateDto model)
        {
            if (model is null)
                return BadRequest(new { message = "Modelo Usuario requerido" });

            var dbUser = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == id);
            if (dbUser is null)
                return NotFound(new { message = "Usuario no encontrado" });

            if (string.IsNullOrWhiteSpace(model.Correo))
                return BadRequest(new { message = "El correo es obligatorio" });

            var nuevoCorreo = model.Correo.Trim();

            if (!string.Equals(dbUser.Correo, nuevoCorreo, StringComparison.OrdinalIgnoreCase))
            {
                var correoRepetido = await _db.Usuarios
                    .AnyAsync(u => u.Id != id && u.Correo == nuevoCorreo);

                if (correoRepetido)
                    return BadRequest(new { message = "El correo ya está registrado" });
            }

            var originalHash = dbUser.PasswordHash;

            dbUser.Correo = nuevoCorreo;
            dbUser.Activo = model.Activo;
            dbUser.Nombre = model.Nombre;
            dbUser.ApellidoPaterno = model.ApellidoPaterno;
            dbUser.ApellidoMaterno = model.ApellidoMaterno;

            dbUser.PasswordHash = originalHash;
            _db.Entry(dbUser).Property(u => u.PasswordHash).IsModified = false;

            try
            {
                await _db.SaveChangesAsync();

                var result = new
                {
                    dbUser.Id,
                    dbUser.Correo,
                    dbUser.Activo,
                    dbUser.Nombre,
                    dbUser.ApellidoPaterno,
                    dbUser.ApellidoMaterno
                };

                return Ok(result);
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new
                {
                    message = "Ocurrió un error al actualizar el usuario en la base de datos.",
                    error = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Ocurrió un error inesperado al actualizar el usuario.",
                    error = ex.Message
                });
            }
        }

        // =====================================================================
        // PUT: api/Usuarios/{id}/password
        // =====================================================================
        public class PasswordDto { public string? Password { get; set; } }

        [HttpPut("{id:int}/password")]
        public async Task<IActionResult> UpdatePassword(int id, [FromBody] PasswordDto body)
        {
            var dbUser = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == id);
            if (dbUser is null)
                return NotFound(new { message = "Usuario no encontrado" });

            if (body is null || string.IsNullOrWhiteSpace(body.Password))
                return BadRequest(new { message = "La nueva contraseña es obligatoria" });

            dbUser.PasswordHash = _utilidades.encriptarSHA256(body.Password);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // =====================================================================
        // DELETE: api/Usuarios/{id}
        // =====================================================================
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var dbUser = await _db.Usuarios.FindAsync(id);
            if (dbUser is null)
                return NotFound(new { message = "Usuario no encontrado" });

            _db.Usuarios.Remove(dbUser);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // =====================================================================
        // PUT: api/Usuarios/{idUsuario}/Roles  → asigna roles a usuario
        // REGLAS:
        // - Estudiante: SOLO puede tener rol ESTUDIANTE
        // - Docente: puede tener DOCENTE + 1 rol extra
        // - Docente + Estudiante NO permitido
        // - Si se asigna ESTUDIANTE y no existe registro en Estudiantes -> se crea
        // - Si se asigna DOCENTE y no existe registro en Docentes -> se crea
        // =====================================================================


        public class RolesAsignacionDto
        {
            public int[] RolesIds { get; set; } = Array.Empty<int>();
            public string? NoControl { get; set; } // solo si se asigna Estudiante y no existe registro
        }

        private static bool IsValidNoControl(string v)
        {
            // 8 números o 1 letra + 8 números
            // total 8 o 9 caracteres
            return System.Text.RegularExpressions.Regex.IsMatch(v, @"^[A-Za-z]?\d{8}$");
        }

        [HttpPut("{idUsuario:int}/Roles")]
        public async Task<IActionResult> SetRolesUsuario(int idUsuario, [FromBody] RolesAsignacionDto dto)
        {
            var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == idUsuario);
            if (usuario is null)
                return NotFound(new { message = "Usuario no encontrado" });

            var rolesIds = (dto?.RolesIds ?? Array.Empty<int>()).Distinct().ToArray();

            // Traer descripciones reales de roles
            var roles = await _db.Rol.AsNoTracking()
                .Where(r => rolesIds.Contains(r.Id))
                .Select(r => new { r.Id, r.Descripcion })
                .ToListAsync();

            // Si mandaron ids inexistentes
            if (roles.Count != rolesIds.Length)
                return BadRequest(new { message = "Se enviaron roles inválidos." });

            bool esEstudiante = roles.Any(r => (r.Descripcion ?? "").Trim().ToLower() == "estudiante");
            bool esDocente = roles.Any(r => (r.Descripcion ?? "").Trim().ToLower() == "docente");

            // Reglas:
            // 1) No permitir Docente + Estudiante
            if (esEstudiante && esDocente)
                return BadRequest(new { message = "No está permitido asignar Docente + Estudiante al mismo usuario." });

            // 2) Estudiante SOLO puede tener rol Estudiante
            if (esEstudiante && rolesIds.Length > 1)
                return BadRequest(new { message = "Un Estudiante solo puede tener el rol Estudiante." });

            // === Actualizar UsuarioRol como ya lo hacías ===
            var actuales = await _db.UsuarioRol
                .Where(ur => ur.IdUsuario == idUsuario)
                .ToListAsync();

            _db.UsuarioRol.RemoveRange(actuales);

            foreach (var idRol in rolesIds)
            {
                _db.UsuarioRol.Add(new UsuarioRol
                {
                    IdUsuario = idUsuario,
                    IdRol = idRol
                });
            }

            await _db.SaveChangesAsync();

            // === Crear/recuperar registro asociado SIN borrarlo jamás ===

            // Si es Estudiante: si ya existe registro -> no pedir noControl
            if (esEstudiante)
            {
                var existeEst = await _db.Estudiantes.AnyAsync(e => e.idUsuario == idUsuario);

                if (!existeEst)
                {
                    var nc = (dto?.NoControl ?? "").Trim().ToUpperInvariant();

                    if (string.IsNullOrWhiteSpace(nc))
                        return BadRequest(new { field = "noControl", message = "No. de control es obligatorio para rol Estudiante." });

                    if (!IsValidNoControl(nc))
                        return BadRequest(new { field = "noControl", message = "Formato inválido. Debe ser 8 números o 1 letra + 8 números." });

                    var ncRepetido = await _db.Estudiantes.AnyAsync(e => e.noControl == nc);
                    if (ncRepetido)
                        return Conflict(new { field = "noControl", message = "El No. de control ya está registrado." });

                    // Crear Estudiante “mínimo viable”
                    _db.Estudiantes.Add(new Estudiantes
                    {
                        idUsuario = idUsuario,
                        Nombre = usuario.Nombre ?? "",
                        ApellidoPaterno = usuario.ApellidoPaterno ?? "",
                        ApellidoMaterno = usuario.ApellidoMaterno ?? "",
                        noControl = nc,
                        cp = "00000" // para que no truene si tu tabla lo requiere; luego lo editas en módulo de estudiantes
                    });

                    await _db.SaveChangesAsync();
                }
            }

            // Si es Docente: el registro en la tabla Docentes lo crea DocentesController.Create
            // con los datos completos del formulario. Aquí solo gestionamos el rol.
            // (No auto-crear un registro vacío para evitar duplicados)

            return NoContent();
        }
        // ==========================
        // Helper: noControl temporal único
        // ==========================
        private async Task<string> GenerarNoControlTemporalUnico(int idUsuario)
        {
            // TEMP000123 (6 dígitos). Si existe, incrementa sufijo.
            var baseVal = $"TEMP{idUsuario:D6}".ToUpperInvariant();
            var candidate = baseVal;

            int i = 1;
            while (await _db.Estudiantes.AnyAsync(e => e.noControl == candidate))
            {
                candidate = $"{baseVal}_{i}";
                i++;
                if (i > 9999) break; // fail-safe
            }

            return candidate;
        }
    
        // =====================================================================
        // PUT: api/Usuarios/Roles/{idRol}/Permisos  → asigna permisos a un rol
        // usado por updatePermisosRol(idRol, permisosIds)
        // =====================================================================
        [HttpPut("Roles/{idRol:int}/Permisos")]
        public async Task<IActionResult> SetPermisosRol(int idRol, [FromBody] int[] permisosIds)
        {
            var rolExiste = await _db.Rol.AnyAsync(r => r.Id == idRol);
            if (!rolExiste)
                return NotFound(new { message = "Rol no encontrado" });

            var actuales = await _db.RolPermiso
                .Where(rp => rp.idRol == idRol)
                .ToListAsync();

            _db.RolPermiso.RemoveRange(actuales);

            if (permisosIds != null && permisosIds.Length > 0)
            {
                var distintos = permisosIds.Distinct();
                foreach (var idPermiso in distintos)
                {
                    _db.RolPermiso.Add(new RolPermiso
                    {
                        idRol = idRol,
                        idPermiso = idPermiso
                    });
                }
            }

            await _db.SaveChangesAsync();
            return NoContent();
        }


// GET: api/Usuarios/puede-ser-estudiante?correo=...
[HttpGet("puede-ser-estudiante")]
public async Task<IActionResult> PuedeSerEstudiante([FromQuery] string? correo = null)
{
    if (string.IsNullOrWhiteSpace(correo))
        return BadRequest(new { message = "Correo requerido" });

    var c = correo.Trim().ToLower();

    // 1) Buscar usuario por correo
    var usuario = await _db.Usuarios.AsNoTracking()
        .FirstOrDefaultAsync(u => u.Correo.ToLower() == c);

    // Si no existe, sí puede ser estudiante
    if (usuario == null)
        return Ok(new { puedeSerEstudiante = true, motivo = "" });

    // 2) Revisar roles del usuario (Rol activo + vínculo en UsuarioRol)
    var roles = await (
        from ur in _db.UsuarioRol
        join r in _db.Rol on ur.IdRol equals r.Id
        where ur.IdUsuario == usuario.Id && r.Activo == true
        select r.Descripcion
    ).AsNoTracking().ToListAsync();

    bool tieneDocente = roles.Any(x => (x ?? "").Trim().ToLower() == "docente");
    if (tieneDocente)
        return Ok(new
        {
            puedeSerEstudiante = false,
            motivo = "Este correo ya tiene un rol de Docente asignado. No puede ser Estudiante."
        });

    bool tieneEstudiante = roles.Any(x => (x ?? "").Trim().ToLower() == "estudiante");
    if (tieneEstudiante)
        return Ok(new
        {
            puedeSerEstudiante = false,
            motivo = "Este correo ya tiene un rol de Estudiante asignado."
        });

    // 3) No tiene roles conflictivos
    return Ok(new { puedeSerEstudiante = true, motivo = "" });
}


// GET: api/Usuarios/puede-ser-docente?correo=...
[HttpGet("puede-ser-docente")]
public async Task<IActionResult> PuedeSerDocente([FromQuery] string? correo = null)
{
    if (string.IsNullOrWhiteSpace(correo))
        return BadRequest(new { message = "Correo requerido" });

    var c = correo.Trim().ToLower();

    var usuario = await _db.Usuarios.AsNoTracking()
        .FirstOrDefaultAsync(u => u.Correo.ToLower() == c);

    // Si no existe, sí puede ser docente
    if (usuario == null)
        return Ok(new { puedeSerDocente = true, motivo = "" });

    // Revisar roles del usuario
    var roles = await (
        from ur in _db.UsuarioRol
        join r in _db.Rol on ur.IdRol equals r.Id
        where ur.IdUsuario == usuario.Id && r.Activo == true
        select r.Descripcion
    ).AsNoTracking().ToListAsync();

    bool tieneEstudiante = roles.Any(x => (x ?? "").Trim().ToLower() == "estudiante");
    if (tieneEstudiante)
        return Ok(new
        {
            puedeSerDocente = false,
            motivo = "Este correo ya tiene un rol de Estudiante asignado. No puede ser Docente."
        });

    bool tieneDocente = roles.Any(x => (x ?? "").Trim().ToLower() == "docente");
    if (tieneDocente)
        return Ok(new
        {
            puedeSerDocente = false,
            motivo = "Este correo ya tiene un rol de Docente asignado."
        });

    return Ok(new { puedeSerDocente = true, motivo = "" });
}
}
}