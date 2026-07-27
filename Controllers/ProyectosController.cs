using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApiVinculacionProyectosV2.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using WebApiVinculacionProyectosV2.Dto;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Text.RegularExpressions;
using WebApiVinculacionProyectosV2.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.IO;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApiVinculacionProyectosV2.Models;
using WebApiVinculacionProyectosV2.Models.DTOs;
using System;



public class DocenteProyectoDashboardDto
{
    public int IdProyecto { get; set; }
    public string? Titulo { get; set; }
    public string? Descripcion { get; set; }

    public int IdTipoRelacion { get; set; }
    public string TipoRelacionClave { get; set; } = "";
    public string TipoRelacionDescripcion { get; set; } = "";

    public DateOnly FechaInscripcion { get; set; }

    public int? EstadoId { get; set; }
    public string EstadoDescripcion { get; set; } = "Sin estado";
}


public class ProyectoDocumentoMetaDto
{
    public bool Exists { get; set; }
    public int? Id { get; set; }
    public string? NombreOriginal { get; set; }
    public string? ContentType { get; set; }
    public long? TamanoBytes { get; set; }
    public DateTime? FechaSubida { get; set; }
}



namespace WebApiVinculacionProyectosV2.Dtos
{
    public class IntegranteProyectoDto
    {
        public int id { get; set; }

        public int idUsuario { get; set; }
        public int? idProyecto { get; set; }

        public string nombre { get; set; } = null!;
        public string apellidoPaterno { get; set; } = null!;
        public string apellidoMaterno { get; set; } = null!;

        public string? noControl { get; set; }

        // ✅ correo institucional (Usuarios)
        public string? correo { get; set; }

        // ✅ perfil estudiante (para validación)
        public int? idcarrera { get; set; }
        public string? domicilio { get; set; }
        public string? ciudad { get; set; }
        public string? cp { get; set; }
        public int? idestado { get; set; }
        public string? correoPersonal { get; set; }
        public string? noSeguroSocial { get; set; }
        public int? idDependenciaMedica { get; set; }
        public string? telefonoCelular { get; set; }
        public int? idContactoEmergencia { get; set; }

        // ✅ carrera (texto)
        public int? carreraId { get; set; }
        public string? carreraNombre { get; set; }
    }
}


namespace WebApiVinculacionProyectosV2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
   public partial class ProyectosController : ControllerBase    
    {
        private readonly ResidenciasDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IConstanciasPdfService _pdf;

        // ⭐ Estado por defecto para propuestas de alumno
        private const int ESTADO_PUBLICADO_ID = 2;

        // arriba del controller o dentro:
        private const int ESTADO_ESPERA_ASIGNANDO_REVISOR = 3;
        private const int ESTADO_ESPERA_REVISION_ANTEPROYECTO = 4;
        private const int ESTADO_ESPERA_ASIGNANDO_ASESOR = 6;
        private const int ESTADO_EN_CURSO = 7;
        private const int ESTADO_PRORROGA = 10; // Prórroga
        // ✅ Regex típico RFC (persona física/moral). Ajusta si tu CK es distinto.
        private static readonly Regex RfcRegex =
            new Regex(@"^[A-ZÑ&]{3,4}\d{6}[A-Z0-9]{3}$", RegexOptions.Compiled);

        public ProyectosController(ResidenciasDbContext context, IWebHostEnvironment env, IConstanciasPdfService pdf)
        {
            _context = context;
            _env = env;
            _pdf = pdf;
        }

        private const int ESTADO_CANCELADO_ID = 9; // ⚠️ confirma que en tu BD "Cancelado" sea 9

        private async Task<bool> EstudianteEstaLibreAsync(Estudiantes est)
        {
            var idProy = est.idProyecto ?? 0;
            if (idProy <= 0) return true;

            var estado = await _context.Proyectos
                .Where(p => p.Id == idProy)
                .Select(p => (int?)p.idEstado)
                .FirstOrDefaultAsync();

            // Si no existe el proyecto, por seguridad lo tratamos como NO libre (tú puedes decidir)
            if (estado == null) return false;

            return estado == ESTADO_CANCELADO_ID;
        }


        // GET: api/Proyectos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Proyectos>>> GetProyectos()
        {
            return await _context.Proyectos.ToListAsync();
        }

        [Authorize]
        [HttpGet("{id:int}")]
        public async Task<ActionResult> GetProyectos(int id)
        {
            // ✅ Seguridad: el proyecto existe pero no es tuyo => 403 (Forbid)
            var allowed = await UsuarioPuedeAccederProyectoAsync(id);
            if (!allowed) return Forbid();

            var proyecto = await (
                from p in _context.Proyectos.AsNoTracking()
                join e in _context.Estado.AsNoTracking()
                    on p.idEstado equals e.Id into ee
                from e in ee.DefaultIfEmpty()
                where p.Id == id
                select new
                {
                    p.Id,
                    p.Titulo,
                    p.Descripcion,
                    p.Objetivo,
                    p.NoResidentes,
                    p.IdEmpresa,
                    p.IdPeriodoAcademico,
                    p.idModalidad,
                    p.idEspecializcion,
                    p.idEstado,
                    EstadoDescripcion = e != null ? e.Descripcion : null,
                    p.PropuestaAlumno,
                    p.IdEstudianteCreador
                }
            ).FirstOrDefaultAsync();

            if (proyecto == null) return NotFound();
            return Ok(proyecto);
        }


        // PUT: api/Proyectos/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProyectos(int id, Proyectos dto)
        {
            if (id != dto.Id) return BadRequest();

            var entity = await _context.Proyectos.FirstOrDefaultAsync(p => p.Id == id);
            if (entity == null) return NotFound();

            // ✅ Actualiza SOLO lo que sí permites cambiar
            entity.Titulo = dto.Titulo;
            entity.Descripcion = dto.Descripcion;
            entity.Objetivo = dto.Objetivo;
            entity.NoResidentes = dto.NoResidentes;
            entity.HorarioInicio = dto.HorarioInicio;
            entity.HorarioFinal = dto.HorarioFinal;
            entity.idModalidad = dto.idModalidad;
            entity.idEspecializcion = dto.idEspecializcion;
            entity.idEstado = dto.idEstado;
            entity.IdEmpresa = dto.IdEmpresa;
            entity.IdPeriodoAcademico = dto.IdPeriodoAcademico;
            entity.PropuestaAlumno = dto.PropuestaAlumno;
            entity.IdEstudianteCreador = dto.IdEstudianteCreador;

            // ✅ IMPORTANTE: NO tocar FechaRegistro
            // entity.FechaRegistro = entity.FechaRegistro; // no hagas nada

            await _context.SaveChangesAsync();
            return NoContent();
        }


        // POST: api/Proyectos
        [HttpPost]
        public async Task<ActionResult<Proyectos>> PostProyectos(Proyectos proyecto)
        {
            // ✅ Siempre asigna en backend
            proyecto.FechaRegistro = DateTime.UtcNow;

            _context.Proyectos.Add(proyecto);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetProyectos", new { id = proyecto.Id }, proyecto);
        }



        // DELETE: api/Proyectos/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProyectos(int id)
        {
            var proyecto = await _context.Proyectos.FindAsync(id);
            if (proyecto == null)
            {
                return NotFound();
            }

            _context.Proyectos.Remove(proyecto);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ProyectosExists(int id)
        {
            return _context.Proyectos.Any(e => e.Id == id);
        }

        // ✅ FIX: leer el id del token sin cambiar el JWT
        private int GetUserId()
        {
            var candidates = new[]
            {
                ClaimTypes.NameIdentifier,
                JwtRegisteredClaimNames.Sub,
                JwtRegisteredClaimNames.NameId,
                "nameid",
                "id",
                "userId"
            };

            foreach (var t in candidates)
            {
                var v = User.FindFirstValue(t);
                if (int.TryParse(v, out var id) && id > 0) return id;
            }

            return 0;
        }

        [HttpGet("Banco")]
        public async Task<ActionResult<IEnumerable<ProyectoBancoDto>>> GetBanco()
        {
            var data = await _context.Proyectos
                .AsNoTracking()
                .Where(p =>
                    !p.PropuestaAlumno
                    && (p.idEstado == null || p.idEstado < 7) // 👈 desde 7 ya NO se muestra
                )
                .Select(p => new ProyectoBancoDto
                {
                    Id = p.Id,
                    IdEmpresa = p.IdEmpresa,
                    idEspecializcion = p.idEspecializcion,
                    Titulo = p.Titulo,
                    Descripcion = p.Descripcion,
                    Objetivo = p.Objetivo,
                    FechaRegistro = p.FechaRegistro,
                    NoResidentes = p.NoResidentes,
                    idPeriodoAcademico = p.IdPeriodoAcademico,
                    HorarioInicio = p.HorarioInicio,
                    HorarioFinal = p.HorarioFinal,
                    idModalidad = p.idModalidad,
                    idEstado = p.idEstado,
                    PropuestaAlumno = p.PropuestaAlumno,

                    Registrados = _context.Estudiantes.Count(e => e.idProyecto == p.Id)
                })
                .ToListAsync();

            return Ok(data);
        }


        // ===========================
        // ✅ Helpers RFC
        // ===========================
        private static string NormalizeRfc(string rfc)
        {
            // Quita espacios y pone en mayúsculas
            return (rfc ?? "")
                .Trim()
                .ToUpperInvariant()
                .Replace(" ", "");
        }

        private static string GenerateTempRfc()
        {
            // Genera RFC válido con patrón: 4 letras + yyMMdd + 3 alfanum
            // "XAXX" es RFC genérico usado en ejemplos; lo importante es que pase tu CK.
            var date = System.DateTime.UtcNow.ToString("yyMMdd");
            var homo = System.Guid.NewGuid().ToString("N").Substring(0, 3).ToUpperInvariant(); // 0-9A-F
            return $"XAXX{date}{homo}";
        }

        private async Task<string> GenerateUniqueTempRfcAsync()
        {
            // Por si tienes UNIQUE en RFC, evitamos colisiones (rarísimas, pero ajá).
            for (int i = 0; i < 10; i++)
            {
                var candidate = GenerateTempRfc();
                var exists = await _context.Empresas.AnyAsync(x => x.RFC == candidate);
                if (!exists) return candidate;
            }

            // Último recurso (si el universo conspira)
            return GenerateTempRfc();
        }

        [Authorize]
        [HttpPost("Propuesta")]
        public async Task<ActionResult<PropuestaCreateResultDto>> CrearPropuesta([FromBody] ProyectoPropuestaCreateDto dto)
        {


            var periodoActivo = await _context.PeriodosAcademicos
        .Where(p => p.Activo)
        .OrderByDescending(p => p.FechaInicio) // por si hay error y hay varios
        .FirstOrDefaultAsync();

            if (periodoActivo == null)
                return BadRequest("No hay un periodo académico activo. No se puede crear la propuesta.");


            var idUsuario = GetUserId();
            if (idUsuario <= 0) return Unauthorized("No se pudo leer idUsuario del token.");

            var est = await _context.Estudiantes.FirstOrDefaultAsync(e => e.idUsuario == idUsuario);
            if (est == null) return BadRequest("No eres estudiante.");

            var libre = await EstudianteEstaLibreAsync(est);
            if (!libre)
                return BadRequest("Ya tienes un proyecto asignado (no cancelado). No puedes crear una propuesta.");


            // ✅ Requeridos mínimos del proyecto
            if (string.IsNullOrWhiteSpace(dto.Titulo)) return BadRequest("Titulo es obligatorio.");
            if (string.IsNullOrWhiteSpace(dto.Descripcion)) return BadRequest("Descripcion es obligatoria.");

            if (dto.NoResidentes <= 0) return BadRequest("NoResidentes debe ser mayor a 0.");

            var idEmpresa = dto.IdEmpresa ?? 0;
            var traeNueva = dto.EmpresaNueva != null;

            if (idEmpresa <= 0 && !traeNueva)
                return BadRequest("Debes seleccionar una empresa existente o registrar una nueva.");

            if (idEmpresa > 0 && traeNueva)
                return BadRequest("Manda solo IdEmpresa o EmpresaNueva, no ambos.");

            // ✅ Normaliza FKs opcionales: 0 / null => null (evita FK fails)
            int? idEsp = dto.idEspecializcion;
            if (idEsp.GetValueOrDefault() <= 0) idEsp = null;

            int? idMod = dto.idModalidad;
            if (idMod.GetValueOrDefault() <= 0) idMod = null;

            await using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                int idEmpresaFinal;

                // ========= Empresa existente =========
                if (idEmpresa > 0)
                {
                    var existe = await _context.Empresas.AnyAsync(e => e.Id == idEmpresa);
                    if (!existe) return BadRequest("La empresa seleccionada no existe.");
                    idEmpresaFinal = idEmpresa;
                }
                else
                {
                    // ========= Empresa nueva =========
                    var e = dto.EmpresaNueva!;

                    // ✅ Requeridos mínimos empresa
                    if (string.IsNullOrWhiteSpace(e.Nombre) ||
                        string.IsNullOrWhiteSpace(e.Giro) ||
                        string.IsNullOrWhiteSpace(e.Telefono) ||
                        string.IsNullOrWhiteSpace(e.Email))
                    {
                        return BadRequest("EmpresaNueva requiere Nombre, Giro, Telefono y Email.");
                    }

                    // RFC: si viene, validarlo; si NO viene, generar uno válido para pasar el CK
                    var rfcProvided = !string.IsNullOrWhiteSpace(e.RFC);
                    string rfc;

                    if (rfcProvided)
                    {
                        rfc = NormalizeRfc(e.RFC!);
                        if (!RfcRegex.IsMatch(rfc))
                            return BadRequest("RFC inválido (formato no permitido).");
                    }
                    else
                    {
                        rfc = await GenerateUniqueTempRfcAsync();
                        // Por si tu CK es todavía más exigente
                        if (!RfcRegex.IsMatch(rfc))
                            return BadRequest("No se pudo generar un RFC temporal válido.");
                    }

                    // Si el RFC fue proporcionado, buscamos duplicado por RFC
                    Empresas? yaExiste = null;
                    if (rfcProvided)
                    {
                        yaExiste = await _context.Empresas.FirstOrDefaultAsync(x => x.RFC == rfc);
                    }

                    if (yaExiste != null)
                    {
                        idEmpresaFinal = yaExiste.Id;
                    }
                    else
                    {
                        var nueva = new Empresas
                        {
                            Nombre = e.Nombre.Trim(),
                            RFC = rfc, // ✅ siempre válido
                            Giro = e.Giro.Trim(),
                            Telefono = e.Telefono.Trim(),
                            Email = e.Email.Trim(),

                            // opcionales (si tu DTO los trae o los ignoras, no pasa nada)
                            Mision = e.Mision,
                            Domicilio = e.Domicilio,
                            Colonia = e.Colonia,
                            Estado = e.Estado,
                            Municipio = e.Municipio,
                            Ciudad = e.Ciudad,
                            CP = e.CP,
                            Titular = e.Titular,
                            PuestoTitular = e.PuestoTitular
                        };

                        _context.Empresas.Add(nueva);
                        await _context.SaveChangesAsync();
                        idEmpresaFinal = nueva.Id;
                    }
                }

                // ========= Crear proyecto =========
                var proyecto = new Proyectos
                {
                    IdEmpresa = idEmpresaFinal,
                    idEspecializcion = idEsp,
                    Titulo = dto.Titulo?.Trim(),
                    Descripcion = dto.Descripcion?.Trim(),
                    Objetivo = dto.Objetivo,
                    FechaRegistro = System.DateTime.UtcNow,
                    NoResidentes = dto.NoResidentes,
                    HorarioInicio = dto.HorarioInicio,
                    HorarioFinal = dto.HorarioFinal,
                    idModalidad = idMod,
                    idEstado = ESTADO_PUBLICADO_ID,

                    IdPeriodoAcademico = periodoActivo.Id,
                    PropuestaAlumno = true,

                    // ✅ aquí
                    IdEstudianteCreador = est.id
                };


                _context.Proyectos.Add(proyecto);
                await _context.SaveChangesAsync();

                // ========= Asignar proyecto al estudiante dueño =========
                est.idProyecto = proyecto.Id;
                await _context.SaveChangesAsync();

                await tx.CommitAsync();

                return Ok(new PropuestaCreateResultDto
                {
                    IdProyecto = proyecto.Id,
                    IdEmpresa = idEmpresaFinal
                });
            }
            catch
            {
                // rollback automático al dispose si no se commitea, pero lo dejamos explícito
                await tx.RollbackAsync();
                throw; // deja que tu middleware muestre el error en dev
            }
        }

        [Authorize]
        [HttpPost("{idProyecto:int}/Unirse")]
        public async Task<IActionResult> Unirse(int idProyecto)
        {
            var idUsuario = GetUserId();
            if (idUsuario <= 0) return Unauthorized("No se pudo leer idUsuario del token.");

            var est = await _context.Estudiantes.FirstOrDefaultAsync(e => e.idUsuario == idUsuario);
            if (est == null) return BadRequest("No eres estudiante.");

            var libre = await EstudianteEstaLibreAsync(est);
            if (!libre)
                return BadRequest("Ya tienes un proyecto asignado (no cancelado). No puedes unirte.");

            var proyecto = await _context.Proyectos.FirstOrDefaultAsync(p => p.Id == idProyecto);
            if (proyecto == null) return NotFound("Proyecto no existe.");

            var registrados = await _context.Estudiantes.CountAsync(e => e.idProyecto == idProyecto);
            if (registrados >= proyecto.NoResidentes)
                return BadRequest("Proyecto lleno.");

            // ✅ Asignar proyecto al estudiante
            est.idProyecto = idProyecto;

            // ✅ SOLO si es proyecto del banco y está "Disponible/Publicado" => pasar a estado 3
            // (Ajusta el estado "Disponible" si en tu BD es otro, aquí asumo 2 = Publicado)
            if (!proyecto.PropuestaAlumno && proyecto.idEstado == ESTADO_PUBLICADO_ID)
            {
                proyecto.idEstado = ESTADO_ESPERA_ASIGNANDO_REVISOR;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Te uniste al proyecto.",
                idProyecto = proyecto.Id,
                idEstado = proyecto.idEstado
            });
        }

        private static bool EsEstadoValido(string estado)
        {
            return estado == "PENDIENTE" || estado == "ACEPTADA" || estado == "RECHAZADA" || estado == "CANCELADA";
        }

       [Authorize]
[HttpPost("{idProyecto:int}/Invitaciones")]
public async Task<IActionResult> CrearInvitaciones(int idProyecto, [FromBody] List<CrearInvitacionDto> invitados)
{
    var idUsuario = GetUserId();
    if (idUsuario <= 0) return Unauthorized("No se pudo leer idUsuario del token.");

    var creador = await _context.Estudiantes.FirstOrDefaultAsync(e => e.idUsuario == idUsuario);
    if (creador == null) return BadRequest("No eres estudiante.");

    var proyecto = await _context.Proyectos.FirstOrDefaultAsync(p => p.Id == idProyecto);
    if (proyecto == null) return NotFound("Proyecto no existe.");

    if (creador.idProyecto != idProyecto)
        return Forbid("Solo el dueño del proyecto puede enviar invitaciones.");

    var cupo = proyecto.NoResidentes;
    if (cupo > 0)
    {
        var integrantesActuales = await _context.Estudiantes
            .CountAsync(e => e.idProyecto == idProyecto);

        if (integrantesActuales >= cupo)
            return BadRequest("El proyecto ya alcanzó su cupo máximo.");
    }

    if (invitados == null || invitados.Count == 0)
        return BadRequest("Lista de invitados vacía.");

    if (!proyecto.PropuestaAlumno)
        return BadRequest("Los proyectos del banco no manejan invitaciones.");

    if (proyecto.IdEstudianteCreador == null || proyecto.IdEstudianteCreador != creador.id)
        return Forbid("Solo el líder del proyecto puede enviar invitaciones.");

    var idsInvitados = invitados
        .Select(x => x.IdEstudianteInvitado)
        .Where(x => x > 0 && x != creador.id)
        .Distinct()
        .ToList();

    if (idsInvitados.Count == 0)
        return BadRequest("No hay invitados válidos.");

    var estudiantesValidos = await _context.Estudiantes
        .Where(e => idsInvitados.Contains(e.id))
        .Select(e => new
        {
            e.id,
            e.idProyecto,
            EstadoProyecto = e.idProyecto == null
                ? (int?)null
                : _context.Proyectos
                    .Where(p => p.Id == e.idProyecto)
                    .Select(p => (int?)p.idEstado)
                    .FirstOrDefault()
        })
        .ToListAsync();

    const int ESTADO_CANCELADO_ID = 9;

    var idsDisponibles = estudiantesValidos
        .Where(x =>
            x.idProyecto == null || x.idProyecto == 0 ||
            x.EstadoProyecto == ESTADO_CANCELADO_ID
        )
        .Select(x => x.id)
        .ToList();

    if (idsDisponibles.Count == 0)
        return BadRequest("Ningún invitado está disponible (ya tienen proyecto no cancelado o no existen).");

    var proyectoActualPorEstudiante = estudiantesValidos
        .ToDictionary(x => x.id, x => x.idProyecto);

    var existentes = await _context.InvitacionProyectos
        .Where(i => i.IdProyecto == idProyecto && idsDisponibles.Contains(i.IdEstudianteInvitado))
        .ToListAsync();

    static string NormEstado(string? s) => (s ?? "").Trim().ToUpperInvariant();

    bool EsAceptadaHuerfana(InvitacionProyecto inv)
    {
        var estado = NormEstado(inv.Estado);
        if (estado != "ACEPTADA" && estado != "ACEPTADO") return false;

        proyectoActualPorEstudiante.TryGetValue(inv.IdEstudianteInvitado, out var idProyectoActual);
        return (idProyectoActual ?? 0) != idProyecto;
    }

    bool EsReinvitable(InvitacionProyecto inv)
    {
        var e = NormEstado(inv.Estado);

        if (e == "RECHAZADO" || e == "RECHAZADA" || e == "RECHAZO")
            return true;

        if (e == "CANCELADO" || e == "CANCELADA" || e == "CANCELACION")
            return true;

        // ✅ clave del fix:
        // si estaba aceptada pero el alumno ya no pertenece al proyecto,
        // se puede reutilizar/reactivar
        if (EsAceptadaHuerfana(inv))
            return true;

        return false;
    }

    var reactivadas = 0;
    var now = DateTime.UtcNow;

    foreach (var inv in existentes)
    {
        if (EsReinvitable(inv))
        {
            inv.Estado = "PENDIENTE";
            inv.FechaCreacion = now;
            inv.FechaRespuesta = null;
            inv.IdEstudianteCreador = creador.id;
            reactivadas++;
        }
    }

    var idsConInvActiva = existentes
        .Where(i => !EsReinvitable(i))
        .Select(i => i.IdEstudianteInvitado)
        .ToHashSet();

    var nuevos = idsDisponibles
        .Where(idEst => !idsConInvActiva.Contains(idEst))
        .Select(idEst => new InvitacionProyecto
        {
            IdProyecto = idProyecto,
            IdEstudianteInvitado = idEst,
            IdEstudianteCreador = creador.id,
            Estado = "PENDIENTE",
            FechaCreacion = now
        })
        .ToList();

    if (reactivadas == 0 && nuevos.Count == 0)
        return Ok(new
        {
            creadas = 0,
            reactivadas = 0,
            mensaje = "Ya existían invitaciones activas para todos esos alumnos."
        });

    if (nuevos.Count > 0)
        _context.InvitacionProyectos.AddRange(nuevos);

    await _context.SaveChangesAsync();

    return Ok(new { creadas = nuevos.Count, reactivadas });
}
        
        
        public class InvitacionMiaDto
        {
            public int Id { get; set; }
            public int IdProyecto { get; set; }
            public string Estado { get; set; }
            public DateTime FechaCreacion { get; set; }
            public int IdEstudianteCreador { get; set; }

            public string? TituloProyecto { get; set; }
            public string? DescripcionProyecto { get; set; }

            public int IdEmpresa { get; set; }
            public string? NombreEmpresa { get; set; }

            public string? NombreCreador { get; set; }
        }


        [Authorize]
        [HttpGet("Invitaciones/Mias")]
        public async Task<ActionResult<IEnumerable<InvitacionMiaDto>>> MisInvitaciones([FromQuery] string estado = "PENDIENTE")
        {
            var idUsuario = GetUserId();
            if (idUsuario <= 0) return Unauthorized("No se pudo leer idUsuario del token.");

            var est = await _context.Estudiantes.FirstOrDefaultAsync(e => e.idUsuario == idUsuario);
            if (est == null) return BadRequest("No eres estudiante.");

            estado = (estado ?? "PENDIENTE").Trim().ToUpperInvariant();
            if (!EsEstadoValido(estado)) return BadRequest("Estado inválido.");

            var data = await (
                from i in _context.InvitacionProyectos
                join p in _context.Proyectos on i.IdProyecto equals p.Id
                join emp in _context.Empresas on p.IdEmpresa equals emp.Id
                join creador in _context.Estudiantes on i.IdEstudianteCreador equals creador.id into creadorJoin
                from c in creadorJoin.DefaultIfEmpty() // por si el creador no existe (evita truene)
                where i.IdEstudianteInvitado == est.id && i.Estado == estado
                orderby i.FechaCreacion descending
                select new InvitacionMiaDto
                {
                    Id = i.Id,
                    IdProyecto = i.IdProyecto,
                    Estado = i.Estado,
                    FechaCreacion = i.FechaCreacion,
                    IdEstudianteCreador = i.IdEstudianteCreador,

                    TituloProyecto = p.Titulo,
                    DescripcionProyecto = p.Descripcion,

                    IdEmpresa = emp.Id,
                    NombreEmpresa = emp.Nombre,

                    NombreCreador = c == null
                        ? null
                        : (c.Nombre + " " + c.ApellidoPaterno + " " + c.ApellidoMaterno)
                }
            ).ToListAsync();

            return Ok(data);
        }

        [Authorize]
        [HttpGet("{idProyecto:int}/Invitaciones/Enviadas")]
        public async Task<ActionResult<IEnumerable<InvitacionEnviadaDto>>> InvitacionesEnviadas(int idProyecto)
        {

            var idUsuario = GetUserId();
            if (idUsuario <= 0) return Unauthorized("No se pudo leer idUsuario del token.");

            var creador = await _context.Estudiantes.FirstOrDefaultAsync(e => e.idUsuario == idUsuario);
            if (creador == null) return BadRequest("No eres estudiante.");

            var proyecto = await _context.Proyectos.FirstOrDefaultAsync(p => p.Id == idProyecto);
            if (proyecto == null) return NotFound("Proyecto no existe.");

            if (!proyecto.PropuestaAlumno)
                return BadRequest("Los proyectos del banco no manejan invitaciones.");

            if (proyecto.IdEstudianteCreador == null || proyecto.IdEstudianteCreador != creador.id)
                return Forbid("Solo el líder del proyecto puede ver invitaciones enviadas.");

            // Solo el que pertenece al proyecto puede ver invitaciones enviadas de ese proyecto
            if (creador.idProyecto != idProyecto)
                return Forbid("Solo el dueño del proyecto puede ver las invitaciones enviadas.");

            if (proyecto == null) return NotFound("Proyecto no existe.");

            if (proyecto.IdEstudianteCreador != creador.id)
                return Forbid("Solo el líder del proyecto puede ver las invitaciones enviadas.");

            var data = await _context.InvitacionProyectos
                .Where(i => i.IdProyecto == idProyecto && i.IdEstudianteCreador == creador.id)
                .OrderByDescending(i => i.FechaCreacion)
                .Select(i => new InvitacionEnviadaDto
                {
                    Id = i.Id,
                    IdProyecto = i.IdProyecto,
                    IdEstudianteInvitado = i.IdEstudianteInvitado,
                    IdEstudianteCreador = i.IdEstudianteCreador,
                    Estado = i.Estado,
                    FechaCreacion = i.FechaCreacion,
                    FechaRespuesta = i.FechaRespuesta,

                    NoControlInvitado = _context.Estudiantes
                        .Where(e => e.id == i.IdEstudianteInvitado)
                        .Select(e => e.noControl)
                        .FirstOrDefault(),

                    NombreInvitado = _context.Estudiantes
                        .Where(e => e.id == i.IdEstudianteInvitado)
                        .Select(e => (e.Nombre + " " + e.ApellidoPaterno + " " + e.ApellidoMaterno))
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(data);
        }

        [Authorize]
        [HttpPost("Invitaciones/{idInv:int}/Responder")]
        public async Task<IActionResult> ResponderInvitacion(int idInv, [FromBody] ResponderInvitacionDto dto)
        {
            var idUsuario = GetUserId();
            if (idUsuario <= 0) return Unauthorized("No se pudo leer idUsuario del token.");

            var est = await _context.Estudiantes.FirstOrDefaultAsync(e => e.idUsuario == idUsuario);
            if (est == null) return BadRequest("No eres estudiante.");

            var libre = await EstudianteEstaLibreAsync(est);
            if (!libre)
                return BadRequest("Ya tienes un proyecto asignado (no cancelado), no puedes aceptar.");


            var inv = await _context.InvitacionProyectos.FirstOrDefaultAsync(i => i.Id == idInv);
            if (inv == null) return NotFound("Invitación no existe.");

            // Solo el invitado puede responder
            if (inv.IdEstudianteInvitado != est.id)
                return Forbid("No puedes responder invitaciones que no son tuyas.");

            if (inv.Estado != "PENDIENTE")
                return BadRequest("Esta invitación ya fue respondida.");

            var accion = (dto?.Accion ?? "").Trim().ToUpperInvariant();
            if (accion != "ACEPTAR" && accion != "RECHAZAR")
                return BadRequest("Acción inválida (ACEPTAR/RECHAZAR).");

            if (accion == "RECHAZAR")
            {
                inv.Estado = "RECHAZADA";
                inv.FechaRespuesta = System.DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return Ok(new { estado = inv.Estado });
            }

            // ===== ACEPTAR (transacción para cupo) =====
            // ===== ACEPTAR (transacción para cupo) =====
            // ===== ACEPTAR (transacción para cupo) =====
            using var tx = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            try
            {
                // Refrescar invitación dentro de la transacción
                var inv2 = await _context.InvitacionProyectos.FirstOrDefaultAsync(i => i.Id == idInv);
                if (inv2 == null) return NotFound("Invitación no existe.");

                if (inv2.IdEstudianteInvitado != est.id)
                    return Forbid("No puedes responder invitaciones que no son tuyas.");

                var estadoInv = (inv2.Estado ?? "").Trim().ToUpperInvariant();
                if (estadoInv != "PENDIENTE")
                    return BadRequest("Esta invitación ya fue respondida.");

                // Revalidar que sigue libre dentro de transacción
                var libre2 = await EstudianteEstaLibreAsync(est);
                if (!libre2)
                    return BadRequest("Ya tienes un proyecto asignado (no cancelado), no puedes aceptar.");

                var proyecto = await _context.Proyectos.FirstOrDefaultAsync(p => p.Id == inv2.IdProyecto);
                if (proyecto == null) return NotFound("Proyecto no existe.");

                // ✅ Validación de cupo ANTES de asignar
                var registrados = await _context.Estudiantes.CountAsync(e => e.idProyecto == inv2.IdProyecto);

                var cupo = proyecto.NoResidentes; // <-- usa el campo REAL del modelo

                if (cupo > 0 && registrados >= cupo)
                    return BadRequest("Proyecto lleno.");

                // ✅ Asignar ya que hay cupo
                est.idProyecto = inv2.IdProyecto;

                inv2.Estado = "ACEPTADA";
                inv2.FechaRespuesta = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return Ok(new { estado = inv2.Estado, idProyecto = inv2.IdProyecto });
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }


        }


        [Authorize]
        [HttpGet("{idProyecto:int}/EsLider")]
        public async Task<ActionResult<EsLiderDto>> EsLider(int idProyecto)
        {
            var idUsuario = GetUserId();
            if (idUsuario <= 0) return Unauthorized("No se pudo leer idUsuario del token.");

            var est = await _context.Estudiantes.FirstOrDefaultAsync(e => e.idUsuario == idUsuario);
            if (est == null) return BadRequest("No eres estudiante.");

            var proyecto = await _context.Proyectos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == idProyecto);

            if (proyecto == null) return NotFound("Proyecto no existe.");

            var esLider = proyecto.IdEstudianteCreador.HasValue && proyecto.IdEstudianteCreador.Value == est.id;

            return Ok(new EsLiderDto
            {
                IdProyecto = idProyecto,
                EsLider = esLider,
                IdEstudianteCreador = proyecto.IdEstudianteCreador
            });
        }

        [Authorize]
        [HttpPost("{idProyecto:int}/Docentes/Asignar")]
        public async Task<IActionResult> AsignarDocenteRelacion(int idProyecto, [FromBody] AsignarDocenteRelacionDto dto)
        {
            if (idProyecto <= 0) return BadRequest("idProyecto inválido.");
            if (dto == null) return BadRequest("Body requerido.");
            if (dto.IdDocente <= 0) return BadRequest("IdDocente inválido.");
            if (string.IsNullOrWhiteSpace(dto.TipoClave)) return BadRequest("TipoClave es obligatorio.");

            var proyecto = await _context.Proyectos.FirstOrDefaultAsync(p => p.Id == idProyecto);
            if (proyecto == null) return NotFound("Proyecto no existe.");

            var docente = await _context.Docentes.FirstOrDefaultAsync(d => d.Id == dto.IdDocente);
            if (docente == null) return NotFound("Docente no existe.");

            var clave = dto.TipoClave.Trim().ToUpperInvariant();

            var tipo = await _context.TipoRelacionDocenteProyecto
                .FirstOrDefaultAsync(t => t.Clave == clave && t.Activo);

            if (tipo == null) return BadRequest("Tipo de relación inválido o inactivo.");

            // ==========================
            // ✅ Reglas de negocio por estado
            // ==========================

            // Revisor de anteproyecto SOLO en estado 3
           
            // Asesor interno y Revisores residencia SOLO en 6 (Espera Asignando Asesor), 7 (En curso) o 10 (Prórroga)
            if ((tipo.Clave == "ASESOR_INTERNO" || tipo.Clave == "REVISOR_RESIDENCIA")
                && !(proyecto.idEstado == ESTADO_ESPERA_ASIGNANDO_ASESOR
                     || proyecto.idEstado == ESTADO_EN_CURSO
                     || proyecto.idEstado == ESTADO_PRORROGA))
                return Conflict("Solo puedes asignar/cambiar asesor y revisores de residencia cuando el proyecto esté en 6 (Espera Asignando Asesor), 7 (En curso) o 10 (Prórroga).");

            // ==========================
            // ✅ Guardado (SINGLE vs MULTI)
            // ==========================

            if (tipo.Clave == "REVISOR_RESIDENCIA")
            {
                // MULTI: permitir varios revisores, pero sin duplicar el mismo docente
                var yaExiste = await _context.ProyectoDocente.AnyAsync(x =>
                    x.idProyecto == idProyecto &&
                    x.IdTipoRelacion == tipo.Id &&
                    x.idDocente == dto.IdDocente
                );

                if (!yaExiste)
                {
                    _context.ProyectoDocente.Add(new ProyectoDocente
                    {
                        idProyecto = idProyecto,
                        idDocente = dto.IdDocente,
                        IdTipoRelacion = tipo.Id,
                        FechaInscripcion = DateOnly.FromDateTime(DateTime.Now)
                    });
                }
            }
            else
            {
                // SINGLE: ASESOR_INTERNO o REVISOR_ANTEPROYECTO (1 por proyecto)
                var existentes = await _context.ProyectoDocente
                    .Where(x => x.idProyecto == idProyecto && x.IdTipoRelacion == tipo.Id)
                    .ToListAsync();

                if (existentes.Count == 0)
                {
                    _context.ProyectoDocente.Add(new ProyectoDocente
                    {
                        idProyecto = idProyecto,
                        idDocente = dto.IdDocente,
                        IdTipoRelacion = tipo.Id,
                        FechaInscripcion = DateOnly.FromDateTime(DateTime.Now)
                    });
                }
                else
                {
                    // actualizar el primero y limpiar duplicados (por seguridad)
                    existentes[0].idDocente = dto.IdDocente;
                    existentes[0].FechaInscripcion = DateOnly.FromDateTime(DateTime.Now);

                    if (existentes.Count > 1)
                        _context.ProyectoDocente.RemoveRange(existentes.Skip(1));
                }

                // ✅ Al asignar REVISOR_ANTEPROYECTO avanzas a estado 4
                if (tipo.Clave == "REVISOR_ANTEPROYECTO")
                    proyecto.idEstado = ESTADO_ESPERA_REVISION_ANTEPROYECTO;
            }

            // ✅ Guardar relaciones (y el posible estado 4)
            await _context.SaveChangesAsync();

            // ==========================
            // ✅ Avance automático: si estamos en 6 y ya hay asesor + ≥1 revisor residencia => estado 7
            // ==========================
            if (proyecto.idEstado == ESTADO_ESPERA_ASIGNANDO_ASESOR)
            {
                var tipoAsesor = await _context.TipoRelacionDocenteProyecto
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Clave == "ASESOR_INTERNO" && t.Activo);

                var tipoRevRes = await _context.TipoRelacionDocenteProyecto
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Clave == "REVISOR_RESIDENCIA" && t.Activo);

                if (tipoAsesor != null && tipoRevRes != null)
                {
                    var hayAsesor = await _context.ProyectoDocente.AnyAsync(x =>
                        x.idProyecto == idProyecto && x.IdTipoRelacion == tipoAsesor.Id
                    );

                    var numRevisoresResidencia = await _context.ProyectoDocente.CountAsync(x =>
                        x.idProyecto == idProyecto && x.IdTipoRelacion == tipoRevRes.Id
                    );

                    if (hayAsesor && numRevisoresResidencia >= 1)
                    {
                        proyecto.idEstado = ESTADO_EN_CURSO;
                        await _context.SaveChangesAsync();
                    }
                }
            }

            return Ok(new { ok = true, estadoNuevo = proyecto.idEstado });
        }

        [Authorize]
        [HttpGet("{idProyecto:int}/Docentes/Relacion")]
        public async Task<IActionResult> GetDocenteRelacion(int idProyecto, [FromQuery] string tipoClave = "REVISOR_ANTEPROYECTO")
        {
            var clave = (tipoClave ?? "").Trim().ToUpperInvariant();

            var tipo = await _context.TipoRelacionDocenteProyecto.FirstOrDefaultAsync(t => t.Clave == clave);
            if (tipo == null) return BadRequest("Tipo de relación inválido.");

            var data = await _context.ProyectoDocente
                .Where(x => x.idProyecto == idProyecto && x.IdTipoRelacion == tipo.Id)
                .Select(x => new
                {
                    x.id,
                    x.idProyecto,
                    x.idDocente,
                    Tipo = tipo.Clave,
                    DocenteNombre = _context.Docentes
                        .Where(d => d.Id == x.idDocente)
                        .Select(d => (d.Nombre + " " + d.ApellidoPaterno + " " + d.ApellidoMaterno))
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync();

            return Ok(data); // puede ser null si no hay asignación
        }

        [Authorize]
        [HttpDelete("{idProyecto:int}/Docentes/Relacion")]
        public async Task<IActionResult> QuitarDocenteRelacion(
    int idProyecto,
    [FromQuery] string tipoClave = "REVISOR_ANTEPROYECTO",
    [FromQuery] int? idDocente = null)
        {
            var clave = (tipoClave ?? "").Trim().ToUpperInvariant();
            var tipo = await _context.TipoRelacionDocenteProyecto
                .FirstOrDefaultAsync(t => t.Clave == clave);
            if (tipo == null) return BadRequest("Tipo de relación inválido.");

            IQueryable<ProyectoDocente> q = _context.ProyectoDocente
                .Where(x => x.idProyecto == idProyecto && x.IdTipoRelacion == tipo.Id);

            if (idDocente.HasValue && idDocente.Value > 0)
                q = q.Where(x => x.idDocente == idDocente.Value);

            var row = await q.FirstOrDefaultAsync();

            if (row == null) return NotFound("No existe asignación para ese tipo (o docente).");

            _context.ProyectoDocente.Remove(row);
            await _context.SaveChangesAsync();

            return Ok(new { eliminado = true });
        }


        [HttpGet("{idProyecto:int}/Integrantes")]
        public async Task<IActionResult> GetIntegrantes(int idProyecto)
        {
            var alumnos = await (
                from e in _context.Estudiantes.AsNoTracking()
                where e.idProyecto == idProyecto

                // LEFT JOIN Usuarios (correo institucional)
                join u0 in _context.Usuarios.AsNoTracking()
                    on e.idUsuario equals u0.Id into uu
                from u in uu.DefaultIfEmpty()

                    // LEFT JOIN Carreras (nombre carrera)
                join c0 in _context.Carreras.AsNoTracking()
                    on e.idcarrera equals c0.Id into cc
                from c in cc.DefaultIfEmpty()

                select new IntegranteProyectoDto
                {
                    id = e.id,
                    idUsuario = e.idUsuario,
                    idProyecto = e.idProyecto,

                    nombre = e.Nombre,
                    apellidoPaterno = e.ApellidoPaterno,
                    apellidoMaterno = e.ApellidoMaterno,

                    noControl = e.noControl,
                    correo = u != null ? u.Correo : null,

                    // perfil
                    idcarrera = e.idcarrera,
                    domicilio = e.domicilio,
                    ciudad = e.ciudad,
                    cp = e.cp   ,
                    correoPersonal = e.correoPersonal,
                    noSeguroSocial = e.noSeguroSocial,
                    idDependenciaMedica = e.idDependenciaMedica,
                    telefonoCelular = e.telefonoCelular,
                    idContactoEmergencia = e.idContactoEmergencia,

                    // ✅ carrera “bonita”
                    carreraId = e.idcarrera,
                    carreraNombre = c != null ? c.Descripcion : null
                }
            )
            .OrderBy(x => x.noControl)
            .ToListAsync();

            return Ok(alumnos);
        }



        private async Task<Docentes?> GetDocenteFromTokenAsync()
        {
            var idUsuario = GetUserId();
            if (idUsuario <= 0) return null;

            return await _context.Docentes.FirstOrDefaultAsync(d => d.idUsuario == idUsuario);
        }

        private async Task<bool> DocenteTieneRelacionAsync(int idProyecto, int idDocente, string tipoClave)
        {
            var clave = (tipoClave ?? "").Trim().ToUpperInvariant();

            var tipo = await _context.TipoRelacionDocenteProyecto
                .FirstOrDefaultAsync(t => t.Clave == clave && t.Activo);

            if (tipo == null) return false;

            return await _context.ProyectoDocente
                .AnyAsync(pd => pd.idProyecto == idProyecto
                             && pd.idDocente == idDocente
                             && pd.IdTipoRelacion == tipo.Id);
        }

        [Authorize]
        [HttpGet("{idProyecto:int}/Docentes/Relaciones")]
        public async Task<IActionResult> GetDocentesRelaciones(
    int idProyecto,
    [FromQuery] string tipoClave = "REVISOR_RESIDENCIA")
        {
            var clave = (tipoClave ?? "").Trim().ToUpperInvariant();

            var tipo = await _context.TipoRelacionDocenteProyecto
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Clave == clave);
            if (tipo == null) return BadRequest("Tipo de relación inválido.");

            var data = await _context.ProyectoDocente
                .AsNoTracking()
                .Where(x => x.idProyecto == idProyecto && x.IdTipoRelacion == tipo.Id)
                .Select(x => new
                {
                    x.id,
                    x.idProyecto,
                    x.idDocente,
                    Tipo = tipo.Clave,
                    DocenteNombre = _context.Docentes
                        .Where(d => d.Id == x.idDocente)
                        .Select(d => (d.Nombre + " " + d.ApellidoPaterno + " " + d.ApellidoMaterno))
                        .FirstOrDefault()
                })
                .OrderBy(x => x.DocenteNombre)
                .ToListAsync();

            return Ok(data);
        }

       

        // ✅ GET: /api/Proyectos/{idProyecto}/Documento/Meta
        [HttpGet("{idProyecto:int}/Documento/Meta")]
        public async Task<ActionResult<ProyectoDocumentoMetaDto>> GetDocumentoMeta(int idProyecto)
        {
            var doc = await _context.ProyectoDocumentos
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.IdProyecto == idProyecto);

            if (doc == null)
            {
                return Ok(new ProyectoDocumentoMetaDto { Exists = false });
            }

            return Ok(new ProyectoDocumentoMetaDto
            {
                Exists = true,
                Id = doc.Id,
                NombreOriginal = doc.NombreOriginal,
                ContentType = doc.ContentType,
                TamanoBytes = doc.TamanoBytes,
                FechaSubida = doc.FechaSubida
            });
        }

        // ✅ POST: /api/Proyectos/{idProyecto}/Documento
        // Sube o reemplaza (1 por proyecto)
        [Authorize]
        [HttpPost("{idProyecto:int}/Documento")]
        [RequestSizeLimit(15_000_000)] // 15MB (ajusta)
        public async Task<IActionResult> UploadDocumento(int idProyecto, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Archivo vacío.");

            // Verifica que exista el proyecto
            var proyectoExists = await _context.Proyectos.AnyAsync(p => p.Id == idProyecto);
            if (!proyectoExists)
                return NotFound("Proyecto no existe.");

            // Valida tipos permitidos (ajusta a tu regla)
            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "application/pdf",
            "image/jpeg",
            "image/png"
        };
            if (!allowed.Contains(file.ContentType))
                return BadRequest("Tipo de archivo no permitido. Solo PDF/JPG/PNG.");

            if (file.Length > 15_000_000)
                return BadRequest("Archivo demasiado grande (máx 15MB).");

            // Carpeta física: <ContentRoot>/uploads/proyectos/{idProyecto}
            var baseFolder = Path.Combine(_env.ContentRootPath, "uploads", "proyectos", idProyecto.ToString());
            Directory.CreateDirectory(baseFolder);

            // Nombre único en servidor
            var ext = Path.GetExtension(file.FileName);
            var serverName = $"PROY_DOC_{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(baseFolder, serverName);

            // Guardar archivo
            await using (var fs = new FileStream(fullPath, FileMode.Create))
                await file.CopyToAsync(fs);

            // Ruta relativa (portable) ✅
            var relativePath = Path.Combine("uploads", "proyectos", idProyecto.ToString(), serverName)
                                .Replace("\\", "/");

            // Upsert: si ya existe doc para el proyecto, reemplaza
            var old = await _context.ProyectoDocumentos
                .FirstOrDefaultAsync(x => x.IdProyecto == idProyecto);

            if (old != null)
            {
                // Borra archivo anterior si existe (evita basura)
                try
                {
                    if (!string.IsNullOrWhiteSpace(old.RutaFisica))
                    {
                        var oldFull = Path.Combine(_env.ContentRootPath, old.RutaFisica.Replace("/", Path.DirectorySeparatorChar.ToString()));
                        if (System.IO.File.Exists(oldFull))
                            System.IO.File.Delete(oldFull);
                    }
                }
                catch
                {
                    // No rompas el flujo por fallo al borrar archivo viejo
                }

                old.NombreOriginal = file.FileName;
                old.NombreServidor = serverName;
                old.ContentType = file.ContentType;
                old.TamanoBytes = file.Length;
                old.RutaFisica = relativePath;
                old.FechaSubida = DateTime.UtcNow;
                old.UploadedByUserId = GetUserIdOrNull();
            }
            else
            {
                var doc = new ProyectoDocumento
                {
                    IdProyecto = idProyecto,
                    NombreOriginal = file.FileName,
                    NombreServidor = serverName,
                    ContentType = file.ContentType,
                    TamanoBytes = file.Length,
                    RutaFisica = relativePath,
                    FechaSubida = DateTime.UtcNow,
                    UploadedByUserId = GetUserIdOrNull()
                };

                _context.ProyectoDocumentos.Add(doc);
            }

            await _context.SaveChangesAsync();
            return Ok(new { ok = true });
        }

        // ✅ GET: /api/Proyectos/{idProyecto}/Documento/Download
        [HttpGet("{idProyecto:int}/Documento/Download")]
        public async Task<IActionResult> DownloadDocumento(int idProyecto)
        {
            var doc = await _context.ProyectoDocumentos
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.IdProyecto == idProyecto);

            if (doc == null)
                return NotFound("No hay documento para este proyecto.");

            if (string.IsNullOrWhiteSpace(doc.RutaFisica))
                return NotFound("Ruta del archivo no registrada.");

            // RutaFisica guardada como relativa => conviértela a absoluta
            var fullPath = Path.Combine(_env.ContentRootPath,
                doc.RutaFisica.Replace("/", Path.DirectorySeparatorChar.ToString()));

            if (!System.IO.File.Exists(fullPath))
                return NotFound("Archivo no encontrado en el servidor.");

            // Stream para no cargar todo en RAM
            var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return File(stream, doc.ContentType, doc.NombreOriginal);
        }

        // ✅ DELETE: /api/Proyectos/{idProyecto}/Documento
        [Authorize]
        [HttpDelete("{idProyecto:int}/Documento")]
        public async Task<IActionResult> DeleteDocumento(int idProyecto)
        {
            var doc = await _context.ProyectoDocumentos
                .FirstOrDefaultAsync(x => x.IdProyecto == idProyecto);

            if (doc == null)
                return NotFound("No hay documento para este proyecto.");

            // Borrar archivo físico
            if (!string.IsNullOrWhiteSpace(doc.RutaFisica))
            {
                var fullPath = Path.Combine(_env.ContentRootPath,
                    doc.RutaFisica.Replace("/", Path.DirectorySeparatorChar.ToString()));

                if (System.IO.File.Exists(fullPath))
                    System.IO.File.Delete(fullPath);
            }

            _context.ProyectoDocumentos.Remove(doc);
            await _context.SaveChangesAsync();

            return Ok(new { ok = true });
        }

        // ===== helpers =====

        // ════════════════════════════════════════════════════════════════════
        // GET /api/Proyectos/disponibles-para-asignacion
        // Proyectos en estado 3 o 6.
        // Regla equitativa: límite por docente = Round(n / x)
        //   n = proyectos disponibles, x = docentes activos en el sistema.
        // ════════════════════════════════════════════════════════════════════
        [Authorize]
        [HttpGet("disponibles-para-asignacion")]
            public async Task<IActionResult> DisponiblesParaAsignacion()
            {
                var userId = GetUserIdOrNull();
                if (userId == null) return Unauthorized();

                var docente = await _context.Docentes.AsNoTracking()
                    .FirstOrDefaultAsync(d => d.idUsuario == userId.Value);

                if (docente == null)
                    return NotFound("No existe docente para este usuario.");

                // ─────────────────────────────────────────────
                // Tipos
                // ─────────────────────────────────────────────
                var tipos = await _context.TipoRelacionDocenteProyecto
                    .AsNoTracking()
                    .Where(t =>
                        (t.Clave == "REVISOR_ANTEPROYECTO" ||
                        t.Clave == "ASESOR_INTERNO") &&
                        t.Activo)
                    .ToListAsync();

                var tipoRevisor = tipos.First(t => t.Clave == "REVISOR_ANTEPROYECTO");
                var tipoAsesor  = tipos.First(t => t.Clave == "ASESOR_INTERNO");

                // ─────────────────────────────────────────────
                // Mis asignaciones
                // ─────────────────────────────────────────────
                var misAsignacionesActuales = await _context.ProyectoDocente
                    .AsNoTracking()
                    .Where(pd =>
                        pd.idDocente == docente.Id &&
                        (pd.IdTipoRelacion == tipoRevisor.Id ||
                        pd.IdTipoRelacion == tipoAsesor.Id))
                    .ToListAsync();

                int misRevisiones = misAsignacionesActuales
                    .Count(x => x.IdTipoRelacion == tipoRevisor.Id);

                int misAsesorias = misAsignacionesActuales
                    .Count(x => x.IdTipoRelacion == tipoAsesor.Id);

                // ─────────────────────────────────────────────
                // Estados válidos
                // ─────────────────────────────────────────────
                var estadosFiltro = new[]
                {
                    ESTADO_ESPERA_ASIGNANDO_REVISOR,      // 3
                    ESTADO_ESPERA_REVISION_ANTEPROYECTO, // 4
                    ESTADO_ESPERA_ASIGNANDO_ASESOR       // 6
                };

                var proyectosBase = await _context.Proyectos
                    .AsNoTracking()
                    .Where(p =>
                        p.idEstado != null &&
                        estadosFiltro.Contains(p.idEstado.Value))
                    .Select(p => new
                    {
                        p.Id,
                        p.Titulo,
                        p.Descripcion,
                        p.idEstado,
                        p.IdPeriodoAcademico
                    })
                    .ToListAsync();

                // ─────────────────────────────────────────────
                // Separación por rol
                // ─────────────────────────────────────────────
                var proyectosRevisor = proyectosBase
                    .Where(p =>
                        p.idEstado == ESTADO_ESPERA_ASIGNANDO_REVISOR ||
                        p.idEstado == ESTADO_ESPERA_REVISION_ANTEPROYECTO)
                    .ToList();

                var proyectosAsesor = proyectosBase
                    .Where(p =>
                        p.idEstado == ESTADO_ESPERA_ASIGNANDO_ASESOR)
                    .ToList();

                // ─────────────────────────────────────────────
                // Distribución equitativa
                // ─────────────────────────────────────────────
                int totalDocentes = await _context.Docentes
                    .AsNoTracking()
                    .CountAsync();

                int limiteRevisor = totalDocentes > 0
                    ? (int)Math.Round(
                        (double)proyectosRevisor.Count / totalDocentes,
                        MidpointRounding.AwayFromZero)
                    : proyectosRevisor.Count;

                int limiteAsesor = totalDocentes > 0
                    ? (int)Math.Round(
                        (double)proyectosAsesor.Count / totalDocentes,
                        MidpointRounding.AwayFromZero)
                    : proyectosAsesor.Count;

                limiteRevisor = Math.Max(limiteRevisor, 1);
                limiteAsesor  = Math.Max(limiteAsesor, 1);

                bool limiteRevisorAlcanzado =
                    misRevisiones >= limiteRevisor;

                bool limiteAsesorAlcanzado =
                    misAsesorias >= limiteAsesor;

                // ─────────────────────────────────────────────
                // Periodos
                // ─────────────────────────────────────────────
                var periodoIds = proyectosBase
                    .Where(p => p.IdPeriodoAcademico.HasValue)
                    .Select(p => p.IdPeriodoAcademico!.Value)
                    .Distinct()
                    .ToList();

                var periodos = await _context.PeriodosAcademicos
                    .AsNoTracking()
                    .Where(p => periodoIds.Contains(p.Id))
                    .Select(p => new
                    {
                        p.Id,
                        p.Nombre
                    })
                    .ToListAsync();

                var periodoDict = periodos
                    .ToDictionary(p => p.Id, p => p.Nombre);

                // ─────────────────────────────────────────────
                // Conteo docentes por proyecto
                // ─────────────────────────────────────────────
                var proyectoIds = proyectosBase
                    .Select(p => p.Id)
                    .ToList();

                var conteos = await _context.ProyectoDocente
                    .AsNoTracking()
                    .Where(pd => proyectoIds.Contains(pd.idProyecto))
                    .GroupBy(pd => pd.idProyecto)
                    .Select(g => new
                    {
                        idProyecto = g.Key,
                        Count = g.Count()
                    })
                    .ToListAsync();

                var conteoDict = conteos
                    .ToDictionary(c => c.idProyecto, c => c.Count);

                // Sets separados por tipo de relación
                var misAsignSetRevisor = misAsignacionesActuales
                    .Where(a => a.IdTipoRelacion == tipoRevisor.Id)
                    .Select(a => a.idProyecto)
                    .ToHashSet();

                var misAsignSetAsesor = misAsignacionesActuales
                    .Where(a => a.IdTipoRelacion == tipoAsesor.Id)
                    .Select(a => a.idProyecto)
                    .ToHashSet();

                var misAsignSet = misAsignacionesActuales
                    .Select(a => a.idProyecto)
                    .ToHashSet();

                // ─────────────────────────────────────────────
                // Resultado final
                // ─────────────────────────────────────────────
                var resultado = proyectosBase
                    .OrderBy(p => conteoDict.GetValueOrDefault(p.Id, 0))
                    .ThenBy(p => p.Titulo)
                    .Select(p => new
                    {
                        p.Id,
                        p.Titulo,
                        p.Descripcion,
                        idEstado = p.idEstado,

                        p.IdPeriodoAcademico,

                        PeriodoNombre =
                            p.IdPeriodoAcademico.HasValue
                                ? periodoDict.GetValueOrDefault(
                                    p.IdPeriodoAcademico.Value)
                                : null,

                        DocentesAsignados =
                            conteoDict.GetValueOrDefault(p.Id, 0),

                        YoSoyElAsignado =
                            misAsignSet.Contains(p.Id),

                        YoSoyElRevisor =
                            misAsignSetRevisor.Contains(p.Id),

                        YoSoyElAsesor =
                            misAsignSetAsesor.Contains(p.Id),

                        PuedeSerRevisor =
                            p.idEstado == ESTADO_ESPERA_ASIGNANDO_REVISOR ||
                            p.idEstado == ESTADO_ESPERA_REVISION_ANTEPROYECTO,

                        PuedeSerAsesor =
                            p.idEstado == ESTADO_ESPERA_ASIGNANDO_ASESOR
                    })
                    .ToList();

                // ─────────────────────────────────────────────
                // Campos extra para el frontend (ts)
                // ─────────────────────────────────────────────
                bool yaAlcanceLimite = limiteRevisorAlcanzado && limiteAsesorAlcanzado;

                int limitePorDocente = Math.Max(limiteRevisor, limiteAsesor);

                // Rol ya elegido (primer proyecto donde soy revisor o asesor)
                string? rolElegido = null;
                int? proyectoElegidoId = null;

                var primerRevisor = misAsignacionesActuales
                    .FirstOrDefault(a => a.IdTipoRelacion == tipoRevisor.Id);
                var primerAsesor = misAsignacionesActuales
                    .FirstOrDefault(a => a.IdTipoRelacion == tipoAsesor.Id);

                if (primerRevisor != null)
                {
                    rolElegido = "REVISOR_ANTEPROYECTO";
                    proyectoElegidoId = primerRevisor.idProyecto;
                }
                else if (primerAsesor != null)
                {
                    rolElegido = "ASESOR_INTERNO";
                    proyectoElegidoId = primerAsesor.idProyecto;
                }

                return Ok(new
                {
                    LimiteRevisor = limiteRevisor,
                    LimiteAsesor = limiteAsesor,
                    LimitePorDocente = limitePorDocente,

                    MisRevisiones = misRevisiones,
                    MisAsesorias = misAsesorias,

                    LimiteRevisorAlcanzado = limiteRevisorAlcanzado,
                    LimiteAsesorAlcanzado = limiteAsesorAlcanzado,
                    YaAlcanceLimite = yaAlcanceLimite,

                    RolElegido = rolElegido,
                    ProyectoElegidoId = proyectoElegidoId,

                    TotalDocentes = totalDocentes,

                    Proyectos = resultado
                });
            }

        // ════════════════════════════════════════════════════════════════════
        // POST /api/Proyectos/{idProyecto}/AutoAsignarme
        // El docente logueado se asigna (1 por docente) y recibe el oficio PDF.
        // Body: { "tipoClave": "ASESOR_INTERNO" | "REVISOR_ANTEPROYECTO" }
        // ════════════════════════════════════════════════════════════════════
        [Authorize]
        [HttpPost("{idProyecto:int}/AutoAsignarme")]
        public async Task<IActionResult> AutoAsignarme(int idProyecto, [FromBody] AutoAsignarmeDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.TipoClave))
                return BadRequest("TipoClave es obligatorio: ASESOR_INTERNO o REVISOR_ANTEPROYECTO.");

            var clave = dto.TipoClave.Trim().ToUpperInvariant();
            if (clave != "ASESOR_INTERNO" && clave != "REVISOR_ANTEPROYECTO" && clave != "REVISOR_RESIDENCIA")
                return BadRequest("TipoClave debe ser ASESOR_INTERNO, REVISOR_ANTEPROYECTO o REVISOR_RESIDENCIA.");

            var userId = GetUserIdOrNull();
            if (userId == null) return Unauthorized();

            var docente = await _context.Docentes
                .FirstOrDefaultAsync(d => d.idUsuario == userId.Value);
            if (docente == null) return NotFound("No existe docente para este usuario.");

            // ── Límite equitativo dinámico: Round(n / x) ────────────────────
            var clavesBloqueantes = new[] { "REVISOR_ANTEPROYECTO", "ASESOR_INTERNO" };
            var tipoIds = await _context.TipoRelacionDocenteProyecto.AsNoTracking()
                .Where(t => clavesBloqueantes.Contains(t.Clave) && t.Activo)
                .Select(t => t.Id).ToListAsync();

            // n = proyectos disponibles, x = docentes activos
            var estadosFiltroLimite = new[] { ESTADO_ESPERA_ASIGNANDO_REVISOR, ESTADO_ESPERA_ASIGNANDO_ASESOR };
            int n = await _context.Proyectos.AsNoTracking()
                .CountAsync(p => p.idEstado != null && estadosFiltroLimite.Contains(p.idEstado.Value));
            int x = await _context.Docentes.AsNoTracking().CountAsync();
            int limitePorDocente = (x > 0)
                ? (int)Math.Round((double)n / x, MidpointRounding.AwayFromZero)
                : n;
            limitePorDocente = Math.Max(limitePorDocente, 1);

            // Cuántos proyectos tiene ya asignados este docente (sin contar el actual)
            int misProyectosCount = await _context.ProyectoDocente.AsNoTracking()
                .CountAsync(pd => pd.idDocente == docente.Id
                               && tipoIds.Contains(pd.IdTipoRelacion)
                               && pd.idProyecto != idProyecto);

            if (misProyectosCount >= limitePorDocente)
                return Conflict($"Ya alcanzaste el límite de {limitePorDocente} proyecto(s) por docente " +
                                $"(distribución equitativa: {n} proyectos / {x} docentes).");

            var proyecto = await _context.Proyectos
                .FirstOrDefaultAsync(p => p.Id == idProyecto);
            if (proyecto == null) return NotFound("Proyecto no encontrado.");

            if (clave == "REVISOR_ANTEPROYECTO" && proyecto.idEstado != ESTADO_ESPERA_ASIGNANDO_REVISOR)
                return Conflict("El proyecto debe estar en estado Espera Asignando Revisor.");

            if (clave == "ASESOR_INTERNO"
                && proyecto.idEstado != ESTADO_ESPERA_ASIGNANDO_ASESOR
                && proyecto.idEstado != ESTADO_EN_CURSO
                && proyecto.idEstado != ESTADO_PRORROGA)
                return Conflict("El proyecto no está en un estado que permita asignación de asesor.");

            var tipo = await _context.TipoRelacionDocenteProyecto
                .FirstOrDefaultAsync(t => t.Clave == clave && t.Activo);
            if (tipo == null) return BadRequest($"Tipo de relación '{clave}' no encontrado.");

            // ── Guardar relación ─────────────────────────────────────────────
            if (clave == "REVISOR_RESIDENCIA")
            {
                var existente = await _context.ProyectoDocente
                    .FirstOrDefaultAsync(x => x.idProyecto == idProyecto && x.IdTipoRelacion == tipo.Id && x.idDocente == docente.Id);

                if (existente == null)
                {
                    _context.ProyectoDocente.Add(new ProyectoDocente {
                        idProyecto = idProyecto,
                        idDocente = docente.Id,
                        IdTipoRelacion = tipo.Id,
                        FechaInscripcion = DateOnly.FromDateTime(DateTime.Now)
                    });
                }
                else
                {
                    existente.FechaInscripcion = DateOnly.FromDateTime(DateTime.Now);
                }

                await _context.SaveChangesAsync();

                if (proyecto.idEstado == ESTADO_ESPERA_ASIGNANDO_ASESOR)
                {
                    var tipoAsesor = await _context.TipoRelacionDocenteProyecto
                        .AsNoTracking()
                        .FirstOrDefaultAsync(t => t.Clave == "ASESOR_INTERNO" && t.Activo);

                    if (tipoAsesor != null)
                    {
                        var hayAsesor = await _context.ProyectoDocente.AnyAsync(x =>
                            x.idProyecto == idProyecto && x.IdTipoRelacion == tipoAsesor.Id);

                        var numRevisoresResidencia = await _context.ProyectoDocente.CountAsync(x =>
                            x.idProyecto == idProyecto && x.IdTipoRelacion == tipo.Id);

                        if (hayAsesor && numRevisoresResidencia >= 1)
                        {
                            proyecto.idEstado = ESTADO_EN_CURSO;
                            await _context.SaveChangesAsync();
                        }
                    }
                }
            }
            else
            {
                var existentes = await _context.ProyectoDocente
                    .Where(x => x.idProyecto == idProyecto && x.IdTipoRelacion == tipo.Id)
                    .ToListAsync();

                if (existentes.Count == 0)
                    _context.ProyectoDocente.Add(new ProyectoDocente {
                        idProyecto      = idProyecto,
                        idDocente       = docente.Id,
                        IdTipoRelacion  = tipo.Id,
                        FechaInscripcion = DateOnly.FromDateTime(DateTime.Now)
                    });
                else
                {
                    existentes[0].idDocente       = docente.Id;
                    existentes[0].FechaInscripcion = DateOnly.FromDateTime(DateTime.Now);
                    if (existentes.Count > 1) _context.ProyectoDocente.RemoveRange(existentes.Skip(1));
                }

                if (clave == "REVISOR_ANTEPROYECTO")
                    proyecto.idEstado = ESTADO_ESPERA_REVISION_ANTEPROYECTO;
                if (clave == "ASESOR_INTERNO" && proyecto.idEstado == ESTADO_ESPERA_ASIGNANDO_ASESOR)
                    proyecto.idEstado = ESTADO_EN_CURSO;

                await _context.SaveChangesAsync();
            }

            // ── Generar oficio PDF ───────────────────────────────────────────
            var periodo = await _context.PeriodosAcademicos.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == proyecto.IdPeriodoAcademico);

            var mem = await _context.PeriodosMembrentados.AsNoTracking()
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync(x => x.PeriodoAcademicoId == proyecto.IdPeriodoAcademico);

            if (mem == null || mem.PdfBytes == null || mem.PdfBytes.Length == 0)
                return NotFound("No se encontró el membrentado para el periodo académico del proyecto. Sube el PDF en Períodos.");

            // Datos del proyecto
            var periodoTxt   = periodo?.Nombre?.ToUpperInvariant() ?? "—";
            var firmaNombre  = periodo?.JefeDepartamentoNombre?.Trim() ?? "JEFA(E) DEL DEPARTAMENTO";
            var docenteNombre = $"{docente.Nombre} {docente.ApellidoPaterno} {docente.ApellidoMaterno}".Trim().ToUpperInvariant();

            var numeroOficio = $"{(periodo?.PrefijoOficio ?? "JV")}-{(periodo?.ConsecutivoOficio ?? 1):000}/{(DateTime.Today.Year % 100):00}";
            if (periodo != null)
            {
                var periodoUpd = await _context.PeriodosAcademicos.FindAsync(periodo.Id);
                if (periodoUpd != null) { periodoUpd.ConsecutivoOficio++; await _context.SaveChangesAsync(); }
            }

            // Estudiantes del proyecto
            var estudianteRows = await (
                from e in _context.Estudiantes.AsNoTracking()
                where e.idProyecto == idProyecto
                select new {
                    e.noControl,
                    Nombre = (e.Nombre ?? "") + " " + (e.ApellidoPaterno ?? "") + " " + (e.ApellidoMaterno ?? "")
                }
            ).ToListAsync();

            var empresa = await _context.Empresas.AsNoTracking()
                .Where(em => em.Id == proyecto.IdEmpresa).Select(em => em.Nombre)
                .FirstOrDefaultAsync() ?? "—";

            var carrera = await (
                from est in _context.Estudiantes.AsNoTracking()
                join car in _context.Carreras.AsNoTracking() on est.idcarrera equals car.Id into cars
                from car in cars.DefaultIfEmpty()
                where est.idProyecto == idProyecto
                select car != null ? car.Descripcion : null
            ).FirstOrDefaultAsync() ?? "—";

        var (pdfBytes, pdfFileName) = await GenerarOficioConsolidadoAsync(docente.Id, clave);
        return File(pdfBytes, "application/pdf", pdfFileName);
        }

                private int? GetUserIdOrNull()
        {
            // Ajusta el claim según cómo emites el JWT.
            // Comunes: ClaimTypes.NameIdentifier o "sub"
            var v = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (int.TryParse(v, out var id)) return id;
            return null;
        }


        public class UpdateCupoDto
        {
            public int NoResidentes { get; set; }
        }

        [Authorize]
        [HttpPut("{idProyecto:int}/Cupo")]
        public async Task<IActionResult> UpdateCupo(int idProyecto, [FromBody] UpdateCupoDto dto)
        {
            if (dto == null) return BadRequest("Body requerido.");
            if (dto.NoResidentes <= 0) return BadRequest("NoResidentes debe ser > 0.");

            var proyecto = await _context.Proyectos.FirstOrDefaultAsync(p => p.Id == idProyecto);
            if (proyecto == null) return NotFound("Proyecto no existe.");

            // ✅ regla mínima de seguridad (ajústala):
            // aquí podrías validar que el docente sea asesor/revisor del proyecto.
            // Por ahora: solo autenticado.
            proyecto.NoResidentes = dto.NoResidentes;

            await _context.SaveChangesAsync();
            return Ok(new { ok = true, proyecto.Id, proyecto.NoResidentes });
        }


        [HttpGet("{idDocente:int}/proyectos-asignados")]
        public async Task<IActionResult> GetProyectosAsignados(int idDocente)
        {
            if (idDocente <= 0)
                return BadRequest(new { message = "idDocente inválido" });

            var items = await (
                from pd in _context.ProyectoDocente.AsNoTracking()
                join p in _context.Proyectos.AsNoTracking()
                    on pd.idProyecto equals p.Id

                // ✅ LEFT JOIN a TipoRelacion para NO perder filas
                join tr0 in _context.TipoRelacionDocenteProyecto.AsNoTracking()
                    on pd.IdTipoRelacion equals tr0.Id into trJoin
                from tr in trJoin.DefaultIfEmpty()

                    // ✅ LEFT JOIN a Estado
                join e0 in _context.Estado.AsNoTracking()
                    on p.idEstado equals e0.Id into estadoJoin
                from e in estadoJoin.DefaultIfEmpty()

                where pd.idDocente == idDocente
                orderby pd.FechaInscripcion descending
                select new DocenteProyectoDashboardDto
                {
                    IdProyecto = p.Id,
                    Titulo = p.Titulo,
                    Descripcion = p.Descripcion,

                    IdTipoRelacion = pd.IdTipoRelacion,
                    TipoRelacionClave = tr != null ? tr.Clave : "SIN_RELACION",
                    TipoRelacionDescripcion = tr != null ? tr.Descripcion : "Sin relación",

                    FechaInscripcion = pd.FechaInscripcion,

                    EstadoId = p.idEstado,
                    EstadoDescripcion = e != null ? e.Descripcion : "Sin estado"
                }
            ).ToListAsync();

            return Ok(items);
        }


        [Authorize]
        [HttpPost("{idProyecto:int}/Cancelar")]
        public async Task<IActionResult> CancelarProyecto(int idProyecto)
        {
            if (idProyecto <= 0) return BadRequest("idProyecto inválido.");

            var idUsuario = GetUserId();
            if (idUsuario <= 0) return Unauthorized("No se pudo leer idUsuario del token.");

            // Solo estudiantes (líder) pueden cancelar propuestas de alumno
            var est = await _context.Estudiantes.FirstOrDefaultAsync(e => e.idUsuario == idUsuario);
            if (est == null) return BadRequest("No eres estudiante.");

            var proyecto = await _context.Proyectos.FirstOrDefaultAsync(p => p.Id == idProyecto);
            if (proyecto == null) return NotFound("Proyecto no existe.");

            // ✅ Regla: solo proyectos PropuestaAlumno manejan invitaciones
            if (!proyecto.PropuestaAlumno)
                return BadRequest("Este proyecto no maneja invitaciones (no es PropuestaAlumno).");

            // ✅ Permiso: solo líder
            if (!proyecto.IdEstudianteCreador.HasValue || proyecto.IdEstudianteCreador.Value != est.id)
                return Forbid("Solo el líder del proyecto puede cancelarlo.");

            // ✅ Permiso extra: el líder debe pertenecer al proyecto (seguridad)
            if (est.idProyecto != idProyecto)
                return Forbid("No perteneces a este proyecto.");

            // Si ya está cancelado, no hagas nada (idempotente)
            if (proyecto.idEstado == ESTADO_CANCELADO_ID)
            {
                return Ok(new
                {
                    ok = true,
                    yaEstabaCancelado = true,
                    invitacionesCanceladas = 0
                });
            }

            // ✅ Transacción (proyecto + invitaciones)
            await using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1) Cancelar proyecto
                proyecto.idEstado = ESTADO_CANCELADO_ID;

                // (Opcional) si tienes un campo para motivo, guárdalo
                // proyecto.MotivoCancelacion = dto?.Motivo;

                // 2) Cancelar invitaciones PENDIENTES del proyecto
                var invitaciones = await _context.InvitacionProyectos
                    .Where(i => i.IdProyecto == idProyecto && i.Estado == "PENDIENTE")
                    .ToListAsync();

                var now = DateTime.UtcNow;

                foreach (var inv in invitaciones)
                {
                    // 👇 Semánticamente correcto: el proyecto se canceló, no es “rechazo del invitado”
                    inv.Estado = "CANCELADA";
                    inv.FechaRespuesta = now;
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return Ok(new
                {
                    ok = true,
                    idProyecto = proyecto.Id,
                    estadoNuevo = proyecto.idEstado,
                    invitacionesCanceladas = invitaciones.Count,
                    motivo = "Proyecto Cancelado"
                });
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public class AgregarIntegranteDto
{
    public int IdEstudiante { get; set; }
}

[Authorize]
[HttpPost("{idProyecto:int}/Integrantes/Agregar")]
public async Task<IActionResult> AgregarIntegrante(int idProyecto, [FromBody] AgregarIntegranteDto dto)
{
    if (dto == null || dto.IdEstudiante <= 0)
        return BadRequest("IdEstudiante inválido.");

    var proyecto = await _context.Proyectos.FirstOrDefaultAsync(p => p.Id == idProyecto);
    if (proyecto == null)
        return NotFound("Proyecto no existe.");

    var estudiante = await _context.Estudiantes.FirstOrDefaultAsync(e => e.id == dto.IdEstudiante);
    if (estudiante == null)
        return NotFound("Estudiante no existe.");

    // Validar que esté libre
    var libre = await EstudianteEstaLibreAsync(estudiante);
    if (!libre)
        return BadRequest("El estudiante ya tiene un proyecto asignado.");

    // Validar cupo
    var registrados = await _context.Estudiantes.CountAsync(e => e.idProyecto == idProyecto);
    if (proyecto.NoResidentes > 0 && registrados >= proyecto.NoResidentes)
        return BadRequest("Proyecto lleno.");

    // Asignar al proyecto
    estudiante.idProyecto = idProyecto;

    await _context.SaveChangesAsync();

    return Ok(new
    {
        ok = true,
        idProyecto,
        idEstudiante = estudiante.id
    });
}


        [Authorize]
[HttpPost("{idProyecto:int}/Salir")]
public async Task<IActionResult> SalirProyecto(int idProyecto)
{
    var idUsuario = GetUserId();
    if (idUsuario <= 0) return Unauthorized("No se pudo leer idUsuario del token.");

    var est = await _context.Estudiantes.FirstOrDefaultAsync(e => e.idUsuario == idUsuario);
    if (est == null) return BadRequest("No eres estudiante.");

    if ((est.idProyecto ?? 0) != idProyecto)
        return Forbid("No perteneces a este proyecto.");

    var proyecto = await _context.Proyectos.FirstOrDefaultAsync(p => p.Id == idProyecto);
    if (proyecto == null) return NotFound("Proyecto no existe.");

    if ((proyecto.idEstado ?? 0) >= ESTADO_ESPERA_ASIGNANDO_ASESOR)
        return Conflict("Solo puedes salir del proyecto durante la etapa 1.");

    if ((proyecto.IdEstudianteCreador ?? 0) == est.id)
        return Conflict("El líder no puede salir del proyecto.");

    await using var tx = await _context.Database.BeginTransactionAsync();

    try
    {
        est.idProyecto = null;

        var invitacionesDelAlumno = await _context.InvitacionProyectos
            .Where(i =>
                i.IdProyecto == idProyecto &&
                i.IdEstudianteInvitado == est.id &&
                (
                    i.Estado == "PENDIENTE" ||
                    i.Estado == "ACEPTADA" ||
                    i.Estado == "ACEPTADO"
                )
            )
            .ToListAsync();

        var now = DateTime.UtcNow;

        foreach (var inv in invitacionesDelAlumno)
        {
            inv.Estado = "CANCELADA";
            inv.FechaRespuesta = now;
        }

        await _context.SaveChangesAsync();
        await tx.CommitAsync();

        return Ok(new
        {
            ok = true,
            idProyecto,
            idEstudiante = est.id,
            invitacionesCanceladas = invitacionesDelAlumno.Count
        });
    }
    catch
    {
        await tx.RollbackAsync();
        throw;
    }
}
        
        private async Task<bool> UsuarioPuedeAccederProyectoAsync(int idProyecto)
        {
            var idUsuario = GetUserId();
            if (idUsuario <= 0) return false;

            // ✅ Caso 1: es estudiante y pertenece al proyecto
            var esIntegrante = await _context.Estudiantes
                .AsNoTracking()
                .AnyAsync(e => e.idUsuario == idUsuario && e.idProyecto == idProyecto);

            if (esIntegrante) return true;

            // ✅ Caso 2: es docente y tiene relación con el proyecto (cualquier tipo)
            var docente = await _context.Docentes
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.idUsuario == idUsuario);

            if (docente == null) return false;

            var esDocenteDelProyecto = await _context.ProyectoDocente
                .AsNoTracking()
                .AnyAsync(pd => pd.idProyecto == idProyecto && pd.idDocente == docente.Id);

            return esDocenteDelProyecto;
        }

        [Authorize]
[HttpDelete("{idProyecto:int}/Integrantes/Quitar/{idEstudiante:int}")]
public async Task<IActionResult> QuitarIntegrante(int idProyecto, int idEstudiante)
{
    if (idProyecto <= 0 || idEstudiante <= 0)
        return BadRequest("Datos inválidos.");

    var proyecto = await _context.Proyectos.FirstOrDefaultAsync(p => p.Id == idProyecto);
    if (proyecto == null)
        return NotFound("Proyecto no existe.");

    var estudiante = await _context.Estudiantes.FirstOrDefaultAsync(e => e.id == idEstudiante);
    if (estudiante == null)
        return NotFound("Estudiante no existe.");

    if ((estudiante.idProyecto ?? 0) != idProyecto)
        return BadRequest("El estudiante no pertenece a este proyecto.");

    // opcional: evita sacar al líder
    if ((proyecto.IdEstudianteCreador ?? 0) == idEstudiante)
        return Conflict("No puedes quitar al líder del proyecto.");

    estudiante.idProyecto = null;
    await _context.SaveChangesAsync();

    return Ok(new { ok = true, idProyecto, idEstudiante });
}



public class AutoAsignarmeDto
{
    public string TipoClave { get; set; } = string.Empty;
}
    // ══════════════════════════════════════════════════════════════════════
        // POST /api/Proyectos/{idProyecto}/Docentes/Sustituir
        // Sustituye a un docente ya asignado (asesor o revisor) por otro.
        // ⚠️  Solo la Jefa de Vinculación puede ejecutar esta acción
        //     (requiere permiso "Proyecto-Sustituir" asignado a su rol en la BD).
        // Body: { "tipoClave": "ASESOR_INTERNO"|"REVISOR_RESIDENCIA", "idDocenteSale": 5, "idDocenteEntra": 12, "motivo": "..." }
        // ══════════════════════════════════════════════════════════════════════
        [Authorize(Policy = "PERM:Proyecto-Sustituir")]
        [HttpPost("{idProyecto:int}/Docentes/Sustituir")]
        public async Task<IActionResult> SustituirDocente(int idProyecto, [FromBody] SustituirDocenteDto dto)
        {
            if (idProyecto <= 0)                           return BadRequest("idProyecto inválido.");
            if (dto == null)                               return BadRequest("Body requerido.");
            if (dto.IdDocenteSale <= 0)                   return BadRequest("IdDocenteSale inválido.");
            if (dto.IdDocenteEntra <= 0)                  return BadRequest("IdDocenteEntra inválido.");
            if (dto.IdDocenteSale == dto.IdDocenteEntra)  return BadRequest("El docente entrante debe ser diferente al saliente.");
            if (string.IsNullOrWhiteSpace(dto.TipoClave)) return BadRequest("TipoClave es obligatorio.");

            var proyecto = await _context.Proyectos.FirstOrDefaultAsync(p => p.Id == idProyecto);
            if (proyecto == null) return NotFound("Proyecto no encontrado.");

            // Validar que el proyecto esté en un estado que permita cambio
            if (proyecto.idEstado == ESTADO_CANCELADO_ID)
                return Conflict("No se puede sustituir docentes en un proyecto cancelado.");

            var clave = dto.TipoClave.Trim().ToUpperInvariant();
            var tipo = await _context.TipoRelacionDocenteProyecto
                .FirstOrDefaultAsync(t => t.Clave == clave && t.Activo);
            if (tipo == null) return BadRequest("Tipo de relación inválido o inactivo.");

            // Verificar que el docente entrante exista
            var docenteEntra = await _context.Docentes.FirstOrDefaultAsync(d => d.Id == dto.IdDocenteEntra);
            if (docenteEntra == null) return NotFound("El docente entrante no existe.");

            // Buscar la relación actual del docente que sale
            var relacionActual = await _context.ProyectoDocente
                .FirstOrDefaultAsync(pd =>
                    pd.idProyecto == idProyecto &&
                    pd.IdTipoRelacion == tipo.Id &&
                    pd.idDocente == dto.IdDocenteSale);

            if (relacionActual == null)
                return NotFound($"El docente #{dto.IdDocenteSale} no tiene una relación '{clave}' en este proyecto.");

            // Verificar que el docente entrante no esté ya asignado en ese rol
            var yaAsignado = await _context.ProyectoDocente.AnyAsync(pd =>
                pd.idProyecto == idProyecto &&
                pd.IdTipoRelacion == tipo.Id &&
                pd.idDocente == dto.IdDocenteEntra);
            if (yaAsignado)
                return Conflict($"El docente entrante ya está asignado como {tipo.Descripcion} en este proyecto.");

            // Realizar la sustitución
            relacionActual.idDocente        = dto.IdDocenteEntra;
            relacionActual.FechaInscripcion = DateOnly.FromDateTime(DateTime.Now);

            await _context.SaveChangesAsync();

            var nombreEntra = $"{docenteEntra.Nombre} {docenteEntra.ApellidoPaterno} {docenteEntra.ApellidoMaterno}".Trim();

            return Ok(new
            {
                ok = true,
                mensaje = $"Sustitución realizada. Ahora {nombreEntra} es {tipo.Descripcion} del proyecto.",
                idDocenteNuevo = dto.IdDocenteEntra,
                nombreDocenteNuevo = nombreEntra
            });
        }

        // ══════════════════════════════════════════════════════════════════════
        // POST /api/Proyectos/{idProyecto}/MarcarRevisionCompletada
        // El revisor de anteproyecto marca que terminó su revisión.
        // Estado 4 (En Espera de Revisión) → 5 (Anteproyecto Revisado) → 6 (En Espera de Asesor Interno)
        // ══════════════════════════════════════════════════════════════════════
        [Authorize]
        [HttpPost("{idProyecto:int}/MarcarRevisionCompletada")]
        public async Task<IActionResult> MarcarRevisionCompletada(int idProyecto)
        {
            const int ESTADO_ESPERA_REVISION     = 4;
            const int ESTADO_REVISADO            = 5;
            const int ESTADO_ESPERA_ASESOR       = 6;

            if (idProyecto <= 0) return BadRequest("idProyecto inválido.");

            // Identificar al docente actual
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var docente = await _context.Docentes
                .FirstOrDefaultAsync(d => d.idUsuario == userId);
            if (docente == null) return NotFound("No existe un docente para este usuario.");

            // Cargar el proyecto
            var proyecto = await _context.Proyectos.FirstOrDefaultAsync(p => p.Id == idProyecto);
            if (proyecto == null) return NotFound("Proyecto no encontrado.");

            // Solo se puede marcar cuando está en estado 4
            if (proyecto.idEstado != ESTADO_ESPERA_REVISION)
                return Conflict($"El proyecto debe estar en estado 4 (En Espera de Revisión de Anteproyecto). Estado actual: {proyecto.idEstado}.");

            // Verificar que el docente actual es el revisor del anteproyecto
            var tipoRevisor = await _context.TipoRelacionDocenteProyecto
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Clave == "REVISOR_ANTEPROYECTO" && t.Activo);

            if (tipoRevisor == null) return BadRequest("Tipo de relación REVISOR_ANTEPROYECTO no configurado.");

            var esRevisor = await _context.ProyectoDocente.AnyAsync(pd =>
                pd.idProyecto == idProyecto &&
                pd.IdTipoRelacion == tipoRevisor.Id &&
                pd.idDocente == docente.Id);

            if (!esRevisor)
                return Forbid(); // Solo el revisor asignado puede marcar revisión completada

            // Avanzar: 4 → 5 (Revisado) → 6 (En Espera de Asesor)
            // Se hace en un solo paso para no dejar el proyecto en estado intermedio innecesario
            proyecto.idEstado = ESTADO_REVISADO;
            await _context.SaveChangesAsync();

            proyecto.idEstado = ESTADO_ESPERA_ASESOR;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                ok = true,
                estadoNuevo = proyecto.idEstado,
                mensaje = "Revisión completada. El proyecto avanzó a 'En Espera de Asignación de Asesor Interno'."
            });
        }
    }
}
    