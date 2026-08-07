using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApiVinculacionProyectosV2.Models.DTOs;
using WebApiVinculacionProyectosV2.Models;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using WebApiVinculacionProyectosV2.Servicios;
using WebApiVinculacionProyectosV2.Services;


public class EntregableEstadoDto
{
    public int Id { get; set; }
    public string Clave { get; set; } = "";
    public string Descripcion { get; set; } = "";
}

public class UpdateEntregableEstadoFormDto
{
    public string EstadoClave { get; set; } = ""; // EN_REVISION | CAMBIOS | APROBADO (en parciales/final no hay RECHAZADO/CANCELADO)
}

public class CreateRevisionFormDto
{
    public string Dictamen { get; set; } = "";       // CAMBIOS | APROBADO | RECHAZADO
    public string Observaciones { get; set; } = "";  // requerido
    public IFormFile? Archivo { get; set; }          // opcional
}


namespace WebApiVinculacionProyectosV2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EntregablesController : ControllerBase
    {
        private readonly ResidenciasDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly INotificacionesService _notificaciones;
        private readonly IConstanciasPdfService _pdf;
        private readonly IServicioEmail _email;

        public EntregablesController(ResidenciasDbContext db, IWebHostEnvironment env, INotificacionesService notificaciones, IConstanciasPdfService pdf, IServicioEmail email)
        {
            _db = db;
            _env = env;
            _notificaciones = notificaciones;
            _pdf = pdf;
            _email = email;
        }

        private static readonly Dictionary<string, int> EstadoEntregableMap = new()
        {
            ["PENDIENTE"] = 1,
            ["EN_REVISION"] = 2,
            ["CAMBIOS"] = 3,
            ["APROBADO"] = 4,
            ["RECHAZADO"] = 5,
            ["CANCELADO"] = 6
        };

        private int EstadoIdOrThrow(string clave)
        {
            clave = (clave ?? "").Trim().ToUpperInvariant();
            if (!EstadoEntregableMap.TryGetValue(clave, out var id))
                throw new InvalidOperationException($"EstadoEntregable inválido: {clave}");
            return id;
        }


        // ===============================
        // GET: api/Entregables/proyecto/5
        // Lista entregables de un proyecto (cabeceras)
        // ===============================
        [HttpGet("proyecto/{idProyecto:int}")]
        public async Task<IActionResult> GetByProyecto(int idProyecto)
        {
            var existeProyecto = await _db.Proyectos.AnyAsync(p => p.Id == idProyecto);
            if (!existeProyecto) return NotFound("Proyecto no existe.");

            var data = await _db.Entregables
                .Where(e => e.IdProyecto == idProyecto)
                .OrderBy(e => e.IdTipoEntregable)
                .Join(_db.EstadoEntregables,
                      e => e.IdEstadoEntregable,
                      s => s.Id,
                      (e, s) => new
                      {
                          e.Id,
                          e.IdProyecto,
                          e.IdTipoEntregable,
                          e.IdEstudianteAutor,
                          e.VersionActual,
                          e.IdEstadoEntregable,
                          EstadoClave = s.Clave,
                          EstadoDescripcion = s.Descripcion,
                          e.FechaCreacion
                      })
                .ToListAsync();

            return Ok(data);
        }

        // GET: api/Entregables/5
        // Detalle: entregable + versiones + revisiones
        // ===============================
        [HttpGet("{idEntregable:int}")]
        public async Task<IActionResult> GetDetalle(int idEntregable)
        {
            var entregable = await _db.Entregables
    .Where(e => e.Id == idEntregable)
    .Join(_db.EstadoEntregables,
          e => e.IdEstadoEntregable,
          s => s.Id,
          (e, s) => new
          {
              e.Id,
              e.IdProyecto,
              e.IdTipoEntregable,
              e.IdEstudianteAutor,
              e.VersionActual,
              e.IdEstadoEntregable,
              EstadoClave = s.Clave,
              EstadoDescripcion = s.Descripcion,
              e.FechaCreacion
          })
    .FirstOrDefaultAsync();


            if (entregable == null) return NotFound("Entregable no existe.");

            var versiones = await (
    from v in _db.EntregableVersiones
    where v.IdEntregable == idEntregable
    orderby v.NumeroVersion
    join e in _db.Estudiantes on v.IdEstudianteSubio equals e.id into ej
    from est in ej.DefaultIfEmpty()

    select new
    {
        v.Id,
        v.IdEntregable,
        v.NumeroVersion,
        v.IdEstudianteSubio,
        v.FechaSubida,
        v.NombreOriginal,
        v.NombreServidor,
        v.ContentType,
        v.TamanoBytes,
        v.RutaFisica,

        SubidoPor = est == null ? null
            : (est.Nombre + " " + est.ApellidoPaterno + " " + est.ApellidoMaterno).Trim(),

        NoControlSubio = est == null ? null : est.noControl,

        // ✅ NUEVO:
        TotalRevisiones = _db.RevisionEntregables.Count(r => r.IdEntregableVersion == v.Id),

        // ✅ Opcional útil para UI:
        UltimoDictamen = _db.RevisionEntregables
            .Where(r => r.IdEntregableVersion == v.Id)
            .OrderByDescending(r => r.NumeroRevision)
            .Select(r => r.Dictamen)
            .FirstOrDefault()
    }
).ToListAsync();




            var versionIds = versiones.Select(v => v.Id).ToList();

            var revisiones = await _db.RevisionEntregables
    .Where(r => versionIds.Contains(r.IdEntregableVersion))
    .OrderBy(r => r.IdEntregableVersion)
    .ThenBy(r => r.NumeroRevision)
    .Select(r => new
    {
        r.Id,
        r.IdEntregableVersion,
        r.NumeroRevision,
        r.IdDocenteRevisor,
        r.Dictamen,
        r.Observaciones,
        r.FechaRevision,

        // ✅ nuevo
        TieneArchivo = r.RutaFisica != null && r.RutaFisica != "",
        r.NombreOriginal,
        r.ContentType,
        r.TamanoBytes
    })
    .ToListAsync();


            return Ok(new { entregable, versiones, revisiones });
        }

        [Authorize]
        [HttpPost("{idProyecto:int}/Cancelar")]
        public async Task<IActionResult> CancelarProyecto(int idProyecto)
        {
            var idUsuario = GetUserId();
            if (idUsuario <= 0) return Unauthorized("No se pudo leer idUsuario del token.");

            var user = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == idUsuario);
            if (user == null) return Unauthorized("Usuario no existe.");

            // ✅ TODO: valida rol real (según tu esquema)
            // Ejemplo:
            // if (user.IdRol != 4) return Forbid("Solo Jefe de vinculación puede cancelar.");

            var proyecto = await _db.Proyectos.FirstOrDefaultAsync(p => p.Id == idProyecto);
            if (proyecto == null) return NotFound("Proyecto no existe.");

            const int ESTADO_CANCELADO_ID = 9;

            // ✅ Idempotente: si ya está cancelado, responde ok sin modificar nada
            if (proyecto.idEstado == ESTADO_CANCELADO_ID)
            {
                return Ok(new
                {
                    ok = true,
                    yaEstabaCancelado = true,
                    estadoNuevo = proyecto.idEstado,
                    invitacionesActualizadas = 0
                
                });
            }

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                // 1) Cancelar proyecto
                proyecto.idEstado = ESTADO_CANCELADO_ID;

                // (Opcional) si tienes campo en DB para guardar motivo, aquí lo asignas.
                // proyecto.MotivoCancelacion = dto?.Motivo;

                // 2) Cancelar invitaciones pendientes del proyecto (solo si tu sistema las usa)
                // ✅ IMPORTANTE: si tu DbSet se llama diferente, ajusta _db.InvitacionProyectos
                var invitacionesPendientes = await _db.InvitacionProyectos
                    .Where(i => i.IdProyecto == idProyecto && i.Estado == "PENDIENTE")
                    .ToListAsync();

                var now = DateTime.UtcNow;

                foreach (var inv in invitacionesPendientes)
                {
                    // ✅ Semántica correcta: el proyecto se canceló
                    inv.Estado = "CANCELADA";
                    inv.FechaRespuesta = now;
                }

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return Ok(new
                {
                    ok = true,
                    estadoNuevo = proyecto.idEstado,
                    invitacionesActualizadas = invitacionesPendientes.Count
                });
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }




        [Authorize]
        [HttpPost("{idProyecto:int}/Aceptar")]
        public async Task<IActionResult> AceptarProyecto(int idProyecto)
        {
            // 1) usuario -> docente
            var idUsuario = GetUserId();
            if (idUsuario <= 0) return Unauthorized("No se pudo leer idUsuario del token.");

            // 2) proyecto
            var proyecto = await _db.Proyectos.FirstOrDefaultAsync(p => p.Id == idProyecto);
            if (proyecto == null) return NotFound("Proyecto no existe.");

            if (proyecto.idEstado != 5)
                return Conflict("El anteproyecto no ha sido revisado.");

            // 3) buscar entregable ANTEPROYECTO aprobado
            var anteTipoId = await _db.TipoEntregables
                .Where(t => t.Activo && t.Descripcion.ToUpper() == "ANTEPROYECTO")
                .Select(t => t.Id)
                .FirstOrDefaultAsync();

            if (anteTipoId <= 0) return BadRequest("No existe TipoEntregable 'ANTEPROYECTO' activo.");

            var ante = await _db.Entregables
                .FirstOrDefaultAsync(e => e.IdProyecto == idProyecto && e.IdTipoEntregable == anteTipoId);

            if (ante == null) return Conflict("Este proyecto aún no tiene entregable de anteproyecto.");
            if (ante.IdEstadoEntregable != 4)
                return Conflict("No puedes aceptar: el anteproyecto no está APROBADO.");

            // 4) Avanzar estado del proyecto
            proyecto.idEstado = 6;
            await _db.SaveChangesAsync();

            // ─────────────────────────────────────────────────────────────────
            // 5) Generar PDF de Aceptación de Reporte Preliminar
            // ─────────────────────────────────────────────────────────────────
            string? rutaFisicaPdf = null;
            string? nombreArchivoPdf = null;

            try
            {
                // Datos del asesor interno (el que llama a este endpoint)
                var docenteRevisor = await _db.Docentes.AsNoTracking()
                    .FirstOrDefaultAsync(d => d.idUsuario == idUsuario);

                var asesorNombre = docenteRevisor != null
                    ? $"{docenteRevisor.Nombre} {docenteRevisor.ApellidoPaterno} {docenteRevisor.ApellidoMaterno}".Trim()
                    : "Asesor Interno";

                // Datos del estudiante
                var estRaw = await _db.Estudiantes.AsNoTracking()
                    .Where(e => e.idProyecto == idProyecto)
                    .Select(e => new {
                        e.noControl,
                        Nombre = (e.Nombre ?? "") + " " + (e.ApellidoPaterno ?? "") + " " + (e.ApellidoMaterno ?? ""),
                        e.idcarrera,
                        e.correoPersonal,
                        CorreoInst = _db.Usuarios.Where(u => u.Id == e.idUsuario).Select(u => u.Correo).FirstOrDefault()
                    })
                    .FirstOrDefaultAsync();

                // Carrera
                string carreraNombre = "—";
                if (estRaw?.idcarrera != null)
                {
                    carreraNombre = await _db.Carreras.AsNoTracking()
                        .Where(c => c.Id == estRaw.idcarrera)
                        .Select(c => c.Descripcion)
                        .FirstOrDefaultAsync() ?? "—";
                }

                // Fechas del proyecto
                string fechaInicio = "—";
                string fechaTermino = "—";
                if (proyecto.HorarioInicio.HasValue)
                    fechaInicio = proyecto.HorarioInicio.Value.ToString("dd/MM/yyyy");
                if (proyecto.HorarioFinal.HasValue)
                    fechaTermino = proyecto.HorarioFinal.Value.ToString("dd/MM/yyyy");

                // Membretado del periodo
                byte[]? membretePdf = null;
                if (proyecto.IdPeriodoAcademico.HasValue)
                {
                    var mem = await _db.PeriodosMembrentados.AsNoTracking()
                        .OrderByDescending(x => x.Id)
                        .FirstOrDefaultAsync(x => x.PeriodoAcademicoId == proyecto.IdPeriodoAcademico);
                    membretePdf = mem?.PdfBytes;
                }

                // Número de oficio del periodo activo
                string numeroOficio = "JV-000/00";
                if (proyecto.IdPeriodoAcademico.HasValue)
                {
                    var periodoUpd = await _db.PeriodosAcademicos
                        .FirstOrDefaultAsync(p => p.Id == proyecto.IdPeriodoAcademico);
                    if (periodoUpd != null)
                    {
                        numeroOficio = $"{periodoUpd.PrefijoOficio}-{periodoUpd.ConsecutivoOficio:000}/{DateTime.Today.Year % 100:00}";
                        periodoUpd.ConsecutivoOficio++;
                        await _db.SaveChangesAsync();
                    }
                }

                // Nombre del jefe de departamento
                string destinatarioNombre = "JEFE DEL DEPARTAMENTO DE SISTEMAS Y COMPUTACIÓN";
                if (proyecto.IdPeriodoAcademico.HasValue)
                {
                    var periodo = await _db.PeriodosAcademicos.AsNoTracking()
                        .FirstOrDefaultAsync(p => p.Id == proyecto.IdPeriodoAcademico);
                    if (!string.IsNullOrWhiteSpace(periodo?.JefeDepartamentoNombre))
                        destinatarioNombre = periodo.JefeDepartamentoNombre.Trim().ToUpperInvariant();
                }

                // Construir solicitud PDF
                var req = new ConstanciaAceptacionReportePreliminarRequest
                {
                    Fecha = DateTime.Today,
                    Oficio = numeroOficio,
                    DestinatarioNombre = destinatarioNombre,
                    Carrera = carreraNombre,
                    NoControl = estRaw?.noControl ?? "—",
                    Estudiante = estRaw?.Nombre.Trim() ?? "—",
                    TituloReporte = proyecto.Titulo ?? "—",
                    FechaInicio = fechaInicio,
                    FechaTermino = fechaTermino,
                    Dictamen = "APROBADO",
                    Comentarios = "",
                    AsesorInterno = asesorNombre
                };

                var pdfBytes = _pdf.BuildConstanciaAceptacionReportePreliminar(membretePdf ?? Array.Empty<byte>(), req);

                // ── Guardar en disco como ProyectoDocumento ──────────────────
                var carpeta = Path.Combine(_env.ContentRootPath, "uploads", "proyectos", idProyecto.ToString(), "aceptacion");
                Directory.CreateDirectory(carpeta);

                nombreArchivoPdf = $"Aceptacion_Anteproyecto_{idProyecto}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                var rutaCompleta = Path.Combine(carpeta, nombreArchivoPdf);
                await System.IO.File.WriteAllBytesAsync(rutaCompleta, pdfBytes);

                rutaFisicaPdf = Path.Combine("uploads", "proyectos", idProyecto.ToString(), "aceptacion", nombreArchivoPdf)
                    .Replace(Path.DirectorySeparatorChar, '/');

                // Registrar en BD
                _db.ProyectoDocumentos.Add(new ProyectoDocumento
                {
                    IdProyecto = idProyecto,
                    NombreOriginal = $"Aceptacion_Anteproyecto_{idProyecto}.pdf",
                    NombreServidor = nombreArchivoPdf,
                    TamanoBytes = pdfBytes.Length,
                    ContentType = "application/pdf",
                    RutaFisica = rutaFisicaPdf,
                    FechaSubida = DateTime.UtcNow,
                    UploadedByUserId = idUsuario
                });
                await _db.SaveChangesAsync();

                // ── Enviar por correo ─────────────────────────────────────────
                var asunto = $"Aceptación de Anteproyecto · {proyecto.Titulo ?? $"Proyecto #{idProyecto}"}";
                var cuerpoHtml = $@"
<p>El anteproyecto del proyecto <b>{System.Net.WebUtility.HtmlEncode(proyecto.Titulo ?? $"Proyecto #{idProyecto}")}</b> ha sido <b>aceptado</b>.</p>
<ul>
  <li><b>Estudiante:</b> {System.Net.WebUtility.HtmlEncode(estRaw?.Nombre.Trim() ?? "—")} ({System.Net.WebUtility.HtmlEncode(estRaw?.noControl ?? "—")})</li>
  <li><b>Asesor interno:</b> {System.Net.WebUtility.HtmlEncode(asesorNombre)}</li>
  <li><b>Fecha:</b> {DateTime.Today:dd/MM/yyyy}</li>
</ul>
<p>Se adjunta el documento de aceptación. También puedes descargarlo desde la plataforma.</p>";

                // Destinatarios: docente asesor + jefa(s) de vinculación (rol 4)
                const int ROL_JEFE_VINCULACION = 4;
                var correosVinculacion = await (
                    from ur in _db.UsuarioRol.AsNoTracking()
                    join u in _db.Usuarios.AsNoTracking() on ur.IdUsuario equals u.Id
                    where ur.IdRol == ROL_JEFE_VINCULACION && !string.IsNullOrEmpty(u.Correo)
                    select u.Correo
                ).Distinct().ToListAsync();

                var todosCorreos = new List<string>();
                if (docenteRevisor != null)
                {
                    var correoDocente = await _db.Usuarios.AsNoTracking()
                        .Where(u => u.Id == docenteRevisor.idUsuario)
                        .Select(u => u.Correo)
                        .FirstOrDefaultAsync();
                    if (!string.IsNullOrWhiteSpace(correoDocente))
                        todosCorreos.Add(correoDocente);
                }
                todosCorreos.AddRange(correosVinculacion);

                // Correo personal del estudiante también (si tiene)
                if (!string.IsNullOrWhiteSpace(estRaw?.correoPersonal))
                    todosCorreos.Add(estRaw.correoPersonal);
                else if (!string.IsNullOrWhiteSpace(estRaw?.CorreoInst))
                    todosCorreos.Add(estRaw.CorreoInst!);

                todosCorreos = todosCorreos
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .Select(c => c.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                // Enviar con PDF adjunto — extendemos el servicio en línea usando SmtpClient directo
                // porque IServicioEmail base no soporta adjuntos todavía
                var emailEmisor = System.Environment.GetEnvironmentVariable("CONFIGURACIONES_EMAIL__EMAIL")
                    ?? System.Environment.GetEnvironmentVariable("CONFIGURACIONES_EMAIL:EMAIL");

                // Fallback: intentar enviar sin adjunto si no tenemos config de email
                foreach (var correo in todosCorreos)
                {
                    try { await _email.EnviarEmail(correo, asunto, cuerpoHtml); }
                    catch { /* log idealmente; no tumbar el flujo */ }
                }
            }
            catch
            {
                // El estado ya se guardó — no revertir solo por el PDF/correo
                // pero devolvemos info para que el frontend sepa
                return Ok(new { ok = true, estadoNuevo = proyecto.idEstado, pdfDisponible = false, aviso = "Anteproyecto aceptado. El PDF o correo no pudo generarse en este momento." });
            }

            return Ok(new
            {
                ok = true,
                estadoNuevo = proyecto.idEstado,
                pdfDisponible = true,
                rutaPdf = rutaFisicaPdf,
                nombrePdf = nombreArchivoPdf
            });
        }

        // Tipo de relación docente-proyecto que identifica al "Asesor Interno"
        // (mismo valor usado en DocumentosController).
        private const int REL_ASESOR_INTERNO = 2;

        // ── Genera (si hace falta) y persiste la constancia de aceptación de un
        //    proyecto que YA fue aceptado (idEstado >= 6) pero que por alguna
        //    razón no quedó guardada como ProyectoDocumento (p. ej. proyectos
        //    aceptados antes de que existiera este flujo, o generados por la
        //    vía "masiva" que no persiste). Devuelve null si no se pudo generar.
        private async Task<(string rutaFisica, string nombreArchivo, byte[] pdfBytes)?> GenerarYGuardarConstanciaAceptacionAsync(Proyectos proyecto)
        {
            var idProyecto = proyecto.Id;

            try
            {
                // Asesor interno REAL del proyecto (no el usuario que está descargando)
                var idDocenteAsesor = await _db.ProyectoDocente.AsNoTracking()
                    .Where(pd => pd.idProyecto == idProyecto && pd.IdTipoRelacion == REL_ASESOR_INTERNO)
                    .Select(pd => pd.idDocente)
                    .FirstOrDefaultAsync();

                var docenteRevisor = idDocenteAsesor > 0
                    ? await _db.Docentes.AsNoTracking().FirstOrDefaultAsync(d => d.Id == idDocenteAsesor)
                    : null;

                var asesorNombre = docenteRevisor != null
                    ? $"{docenteRevisor.Nombre} {docenteRevisor.ApellidoPaterno} {docenteRevisor.ApellidoMaterno}".Trim()
                    : "Asesor Interno";

                var estRaw = await _db.Estudiantes.AsNoTracking()
                    .Where(e => e.idProyecto == idProyecto)
                    .Select(e => new {
                        e.noControl,
                        Nombre = (e.Nombre ?? "") + " " + (e.ApellidoPaterno ?? "") + " " + (e.ApellidoMaterno ?? ""),
                        e.idcarrera
                    })
                    .FirstOrDefaultAsync();

                string carreraNombre = "—";
                if (estRaw?.idcarrera != null)
                {
                    carreraNombre = await _db.Carreras.AsNoTracking()
                        .Where(c => c.Id == estRaw.idcarrera)
                        .Select(c => c.Descripcion)
                        .FirstOrDefaultAsync() ?? "—";
                }

                string fechaInicio = "—";
                string fechaTermino = "—";
                if (proyecto.HorarioInicio.HasValue)
                    fechaInicio = proyecto.HorarioInicio.Value.ToString("dd/MM/yyyy");
                if (proyecto.HorarioFinal.HasValue)
                    fechaTermino = proyecto.HorarioFinal.Value.ToString("dd/MM/yyyy");

                byte[]? membretePdf = null;
                if (proyecto.IdPeriodoAcademico.HasValue)
                {
                    var mem = await _db.PeriodosMembrentados.AsNoTracking()
                        .OrderByDescending(x => x.Id)
                        .FirstOrDefaultAsync(x => x.PeriodoAcademicoId == proyecto.IdPeriodoAcademico);
                    membretePdf = mem?.PdfBytes;
                }

                string numeroOficio = "JV-000/00";
                if (proyecto.IdPeriodoAcademico.HasValue)
                {
                    var periodoUpd = await _db.PeriodosAcademicos
                        .FirstOrDefaultAsync(p => p.Id == proyecto.IdPeriodoAcademico);
                    if (periodoUpd != null)
                    {
                        numeroOficio = $"{periodoUpd.PrefijoOficio}-{periodoUpd.ConsecutivoOficio:000}/{DateTime.Today.Year % 100:00}";
                        periodoUpd.ConsecutivoOficio++;
                        await _db.SaveChangesAsync();
                    }
                }

                string destinatarioNombre = "JEFE DEL DEPARTAMENTO DE SISTEMAS Y COMPUTACIÓN";
                if (proyecto.IdPeriodoAcademico.HasValue)
                {
                    var periodo = await _db.PeriodosAcademicos.AsNoTracking()
                        .FirstOrDefaultAsync(p => p.Id == proyecto.IdPeriodoAcademico);
                    if (!string.IsNullOrWhiteSpace(periodo?.JefeDepartamentoNombre))
                        destinatarioNombre = periodo.JefeDepartamentoNombre.Trim().ToUpperInvariant();
                }

                var req = new ConstanciaAceptacionReportePreliminarRequest
                {
                    Fecha = DateTime.Today,
                    Oficio = numeroOficio,
                    DestinatarioNombre = destinatarioNombre,
                    Carrera = carreraNombre,
                    NoControl = estRaw?.noControl ?? "—",
                    Estudiante = estRaw?.Nombre.Trim() ?? "—",
                    TituloReporte = proyecto.Titulo ?? "—",
                    FechaInicio = fechaInicio,
                    FechaTermino = fechaTermino,
                    Dictamen = "APROBADO",
                    Comentarios = "",
                    AsesorInterno = asesorNombre
                };

                var pdfBytes = _pdf.BuildConstanciaAceptacionReportePreliminar(membretePdf ?? Array.Empty<byte>(), req);

                var carpeta = Path.Combine(_env.ContentRootPath, "uploads", "proyectos", idProyecto.ToString(), "aceptacion");
                Directory.CreateDirectory(carpeta);

                var nombreArchivoPdf = $"Aceptacion_Anteproyecto_{idProyecto}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                var rutaCompleta = Path.Combine(carpeta, nombreArchivoPdf);
                await System.IO.File.WriteAllBytesAsync(rutaCompleta, pdfBytes);

                var rutaFisicaPdf = Path.Combine("uploads", "proyectos", idProyecto.ToString(), "aceptacion", nombreArchivoPdf)
                    .Replace(Path.DirectorySeparatorChar, '/');

                _db.ProyectoDocumentos.Add(new ProyectoDocumento
                {
                    IdProyecto = idProyecto,
                    NombreOriginal = $"Aceptacion_Anteproyecto_{idProyecto}.pdf",
                    NombreServidor = nombreArchivoPdf,
                    TamanoBytes = pdfBytes.Length,
                    ContentType = "application/pdf",
                    RutaFisica = rutaFisicaPdf,
                    FechaSubida = DateTime.UtcNow,
                    UploadedByUserId = docenteRevisor?.idUsuario
                });
                await _db.SaveChangesAsync();

                return (rutaFisicaPdf, nombreArchivoPdf, pdfBytes);
            }
            catch
            {
                // No tumbamos la descarga por esto; el caller decide qué responder.
                return null;
            }
        }

        // ── Descarga el PDF de aceptación de anteproyecto ────────────────────
        [Authorize]
        [HttpGet("{idProyecto:int}/Aceptacion/Descargar")]
        public async Task<IActionResult> DescargarAceptacionAnteproyecto(int idProyecto)
        {
            var doc = await _db.ProyectoDocumentos.AsNoTracking()
                .Where(d => d.IdProyecto == idProyecto && d.NombreOriginal.StartsWith("Aceptacion_Anteproyecto_"))
                .OrderByDescending(d => d.FechaSubida)
                .FirstOrDefaultAsync();

            // Caso 1: nunca se generó el documento (proyecto aceptado antes de
            // que existiera este flujo, o generado solo por la vía "masiva"
            // que no persiste). Si el proyecto ya está aceptado, lo generamos
            // retroactivamente aquí mismo.
            if (doc == null)
            {
                var proyecto = await _db.Proyectos.FirstOrDefaultAsync(p => p.Id == idProyecto);
                if (proyecto == null) return NotFound("Proyecto no existe.");

                if (proyecto.idEstado is null || proyecto.idEstado < 6)
                    return NotFound("No se ha generado aún el documento de aceptación.");

                var generado = await GenerarYGuardarConstanciaAceptacionAsync(proyecto);
                if (generado == null)
                    return NotFound("No se ha generado aún el documento de aceptación.");

                return File(generado.Value.pdfBytes, "application/pdf", $"Aceptacion_Anteproyecto_{idProyecto}.pdf");
            }

            var rutaCompleta = Path.Combine(_env.ContentRootPath, doc.RutaFisica!.Replace('/', Path.DirectorySeparatorChar));

            // Caso 2: había registro en BD pero el archivo ya no está en disco
            // (borrado, servidor migrado, etc.) → también lo regeneramos.
            if (!System.IO.File.Exists(rutaCompleta))
            {
                var proyecto = await _db.Proyectos.FirstOrDefaultAsync(p => p.Id == idProyecto);
                if (proyecto == null) return NotFound("Proyecto no existe.");

                var generado = await GenerarYGuardarConstanciaAceptacionAsync(proyecto);
                if (generado == null)
                    return NotFound("El archivo ya no existe en el servidor.");

                return File(generado.Value.pdfBytes, "application/pdf", $"Aceptacion_Anteproyecto_{idProyecto}.pdf");
            }

            var bytes = await System.IO.File.ReadAllBytesAsync(rutaCompleta);
            return File(bytes, "application/pdf", $"Aceptacion_Anteproyecto_{idProyecto}.pdf");
        }



        // ===============================
        // POST: api/Entregables/versiones/{idVersion}/revisiones
        // Crea una revisión para una versión (docente revisor)
        // ===============================

        [Authorize]
        [HttpPost("versiones/{idEntregableVersion:int}/revisiones")]
        [RequestSizeLimit(50_000_000)]
        public async Task<IActionResult> CreateRevision(
    int idEntregableVersion,
    [FromForm] CreateRevisionFormDto dto)
        {
            if (dto == null) return BadRequest("Body requerido.");

            var dictamen = (dto.Dictamen ?? "").Trim().ToUpperInvariant();
            if (dictamen is not ("CAMBIOS" or "APROBADO" or "RECHAZADO"))
                return BadRequest("Dictamen inválido. Usa: CAMBIOS | APROBADO | RECHAZADO.");

            var obs = (dto.Observaciones ?? "").Trim();
            if (string.IsNullOrWhiteSpace(obs))
                return BadRequest("Observaciones es requerido.");

            // 1) versión
            var version = await _db.EntregableVersiones.FirstOrDefaultAsync(v => v.Id == idEntregableVersion);
            if (version == null) return NotFound("Versión no existe.");

// 1.1) cabecera del entregable (necesaria para tipo/estado global)
var cabecera = await _db.Entregables.FirstOrDefaultAsync(e => e.Id == version.IdEntregable);
if (cabecera == null) return Conflict("Entregable no existe.");

// Tipo 1 = ANTEPROYECTO (ajusta si tu catálogo usa otro ID)
const int TIPO_ANTEPROYECTO = 1;
var esAnteproyecto = cabecera.IdTipoEntregable == TIPO_ANTEPROYECTO;

// En reportes (2/3/4) no se permite RECHAZADO (solo se acepta o se piden cambios)
if (!esAnteproyecto && dictamen == "RECHAZADO")
    return BadRequest("En reportes parciales/final no se permite RECHAZADO. Usa CAMBIOS o APROBADO.");

            // 2) docente desde token
            var idUsuario = GetUserId();
            if (idUsuario <= 0) return Unauthorized("No se pudo leer idUsuario del token.");

            var docente = await _db.Docentes.FirstOrDefaultAsync(d => d.idUsuario == idUsuario);
            if (docente == null) return Forbid("No eres docente.");

            // 3) número de revisión incremental
            var lastNum = await _db.RevisionEntregables
                .Where(r => r.IdEntregableVersion == idEntregableVersion)
                .Select(r => (int?)r.NumeroRevision)
                .MaxAsync();

            var nextNum = (lastNum ?? 0) + 1;

            // 4) preparar entidad
            var revision = new RevisionEntregable
            {
                IdEntregableVersion = idEntregableVersion,
                NumeroRevision = nextNum,
                IdDocenteRevisor = docente.Id,
                Dictamen = dictamen,
                Observaciones = obs
            };

            // 5) si viene archivo, guardarlo
            if (dto.Archivo != null && dto.Archivo.Length > 0)
            {
                // ✅ tipos permitidos (ajusta si quieres)
                var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "application/pdf",
            "application/msword", // .doc
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" // .docx
        };

                var ct = dto.Archivo.ContentType ?? "application/octet-stream";
                if (!allowed.Contains(ct))
                    return BadRequest("Archivo no permitido. Solo PDF/DOC/DOCX.");

                var root = Path.Combine(_env.ContentRootPath, "Uploads", "Entregables",
    cabecera.IdProyecto.ToString(),
    cabecera.Id.ToString(),
    version.NumeroVersion.ToString(),
    "revisiones");

                Directory.CreateDirectory(root);

                var ext = Path.GetExtension(dto.Archivo.FileName);
                var nombreServidor = $"REV_{Guid.NewGuid():N}{ext}";
                var rutaFisica = Path.Combine(root, nombreServidor);

                await using (var fs = new FileStream(rutaFisica, FileMode.Create))
                    await dto.Archivo.CopyToAsync(fs);

                revision.NombreOriginal = dto.Archivo.FileName;
                revision.NombreServidor = nombreServidor;
                revision.ContentType = ct;
                revision.TamanoBytes = dto.Archivo.Length;
                revision.RutaFisica = rutaFisica;
            }

            _db.RevisionEntregables.Add(revision);

            // 6) actualizar estado del entregable (cabecera)
// ✅ ANTEPROYECTO: el estado global se mueve con el dictamen.
// ✅ Reportes parciales/final: el estado global del REPORTE se maneja por separado (endpoint /estado),
//    las revisiones solo dictaminan el ARCHIVO.
if (esAnteproyecto)
{
    cabecera.IdEstadoEntregable = EstadoIdOrThrow(dictamen);
}
            await _db.SaveChangesAsync();

            return Ok(new
            {
                revision.Id,
                revision.IdEntregableVersion,
                revision.NumeroRevision,
                revision.IdDocenteRevisor,
                revision.Dictamen,
                revision.Observaciones,
                revision.FechaRevision,
                TieneArchivo = !string.IsNullOrWhiteSpace(revision.RutaFisica),
                revision.NombreOriginal,
                revision.ContentType,
                revision.TamanoBytes
            });
        }

        // Helper dentro del mismo controller:
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


    // ===============================
    // POST: api/Entregables
    // Crea cabecera de entregable (1 por tipo por proyecto)
    // ===============================
    [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateEntregableDto dto)
        {
            // Validaciones básicas
            var proyecto = await _db.Proyectos.FirstOrDefaultAsync(p => p.Id == dto.IdProyecto);
            if (proyecto == null) return NotFound("Proyecto no existe.");

            var tipo = await _db.TipoEntregables.FirstOrDefaultAsync(t => t.Id == dto.IdTipoEntregable && t.Activo);
            if (tipo == null) return BadRequest("TipoEntregable inválido o inactivo.");

            // El estudiante debe pertenecer a ese proyecto (según tu regla)
            var estudiante = await _db.Estudiantes.FirstOrDefaultAsync(e => e.id == dto.IdEstudianteAutor);
            if (estudiante == null) return NotFound("Estudiante no existe.");

            if (estudiante.idProyecto != dto.IdProyecto)
                return BadRequest("El estudiante no pertenece a ese proyecto.");

            // Evitar duplicado (IdProyecto + IdTipoEntregable) es Unique en BD, pero mejor avisar bonito
            var existe = await _db.Entregables.AnyAsync(e => e.IdProyecto == dto.IdProyecto && e.IdTipoEntregable == dto.IdTipoEntregable);
            if (existe) return Conflict("Ya existe un entregable de ese tipo para el proyecto.");

            var entregable = new Entregable
            {
                IdProyecto = dto.IdProyecto,
                IdTipoEntregable = dto.IdTipoEntregable,
                IdEstudianteAutor = dto.IdEstudianteAutor,
                VersionActual = 0,
                IdEstadoEntregable = EstadoIdOrThrow("EN_REVISION")
            };


            _db.Entregables.Add(entregable);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetDetalle), new { idEntregable = entregable.Id }, new
            {
                entregable.Id,
                entregable.IdProyecto,
                entregable.IdTipoEntregable,
                entregable.IdEstudianteAutor,
                entregable.VersionActual,
                entregable.IdEstadoEntregable
            });
        }

        // ===============================
        // POST: api/Entregables/{idEntregable}/versiones
        // Sube una nueva versión con archivo
        // ===============================
        [HttpPost("{idEntregable:int}/versiones")]
        [RequestSizeLimit(50_000_000)]
        public async Task<IActionResult> UploadVersion(int idEntregable, [FromForm] UploadEntregableVersionDto dto)
        {
            var entregable = await _db.Entregables.FirstOrDefaultAsync(e => e.Id == idEntregable);
            if (entregable == null) return NotFound("Entregable no existe.");

            var estudiante = await _db.Estudiantes.FirstOrDefaultAsync(e => e.id == dto.IdEstudianteSubio);
            if (estudiante == null) return NotFound("Estudiante no existe.");

            if (estudiante.idProyecto != entregable.IdProyecto)
                return BadRequest("El estudiante no pertenece al proyecto del entregable.");

            if (dto.Archivo == null || dto.Archivo.Length == 0)
                return BadRequest("Archivo inválido.");


// ✅ Tipos permitidos: PDF / Word / Imágenes (png/jpg/jpeg)
var ctUp = dto.Archivo.ContentType ?? "application/octet-stream";
var extUp = (Path.GetExtension(dto.Archivo.FileName) ?? "").ToLowerInvariant();

var allowedCtUp = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "application/pdf",
    "application/msword",
    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
    "image/png",
    "image/jpeg"
};

var allowedExtUp = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    ".pdf", ".doc", ".docx", ".png", ".jpg", ".jpeg"
};

if (!allowedCtUp.Contains(ctUp) && !allowedExtUp.Contains(extUp))
    return BadRequest("Archivo no permitido. Solo PDF/DOC/DOCX o imágenes PNG/JPG/JPEG.");

            // Siguiente versión
            var lastVersion = await _db.EntregableVersiones
                .Where(v => v.IdEntregable == idEntregable)
                .MaxAsync(v => (int?)v.NumeroVersion) ?? 0;

            var nextVersion = lastVersion + 1;

            // Guardado físico
            var root = Path.Combine(_env.ContentRootPath, "Uploads", "Entregables",
                entregable.IdProyecto.ToString(), idEntregable.ToString());
            Directory.CreateDirectory(root);

            var ext = Path.GetExtension(dto.Archivo.FileName);
            var safeExt = string.IsNullOrWhiteSpace(ext) ? "" : ext;
            var nombreServidor = $"{Guid.NewGuid():N}{safeExt}";
            var rutaFisica = Path.Combine(root, nombreServidor);

            await using (var fs = new FileStream(rutaFisica, FileMode.Create))
            {
                await dto.Archivo.CopyToAsync(fs);
            }

            var version = new EntregableVersion
            {
                IdEntregable = idEntregable,
                NumeroVersion = nextVersion,
                IdEstudianteSubio = dto.IdEstudianteSubio,
                NombreOriginal = dto.Archivo.FileName,
                NombreServidor = nombreServidor,
                ContentType = ctUp,
                TamanoBytes = dto.Archivo.Length,
                RutaFisica = rutaFisica
            };

            _db.EntregableVersiones.Add(version);

            // Actualiza cabecera
            entregable.VersionActual = nextVersion;
            entregable.IdEstadoEntregable = EstadoIdOrThrow("EN_REVISION");

            // ✅ 1) Persistir
            await _db.SaveChangesAsync();

            // ✅ 2) Notificar (sin tumbar el upload si falla)
            try
            {
                await _notificaciones.AvisarRevisionEntregableSubidoAsync(
                    idEntregable: idEntregable,
                    idEstudianteSubio: dto.IdEstudianteSubio,
                    numeroVersion: nextVersion
                );
            }
            catch
            {
                // Ideal: loggear
            }

            // ✅ 3) Responder
            return Ok(new
            {
                version.Id,
                version.IdEntregable,
                version.NumeroVersion,
                version.IdEstudianteSubio,
                version.NombreOriginal,
                version.ContentType,
                version.TamanoBytes
            });
        }

        // ===============================
        // POST: api/Entregables/versiones/{idVersion}/revisiones
        // Crea una revisión para una versión (docente revisor)
        // ===============================

        // ===============================
        // GET: api/Entregables/versiones/{idVersion}/download
        // Descarga archivo (básico). Ojo con seguridad/autorización.
        // ===============================
        [HttpGet("versiones/{idEntregableVersion:int}/download")]
        public async Task<IActionResult> DownloadVersion(int idEntregableVersion)
        {
            var version = await _db.EntregableVersiones.FirstOrDefaultAsync(v => v.Id == idEntregableVersion);
            if (version == null) return NotFound("Versión no existe.");


            if (!System.IO.File.Exists(version.RutaFisica))
                return NotFound("Archivo no encontrado en disco.");

            var bytes = await System.IO.File.ReadAllBytesAsync(version.RutaFisica);
            return File(bytes, version.ContentType, version.NombreOriginal);
        }

        [HttpGet("estados")]
        public async Task<IActionResult> GetEstadosEntregable()
        {
            var data = await _db.EstadoEntregables
                .Where(x => x.Activo)
                .OrderBy(x => x.Id)
                .Select(x => new { x.Id, x.Clave, x.Descripcion })
                .ToListAsync();

            return Ok(data);
        }

        [HttpGet("revisiones/{idRevision:int}/download")]
        public async Task<IActionResult> DownloadRevision(int idRevision)
        {
            var rev = await _db.RevisionEntregables.FirstOrDefaultAsync(r => r.Id == idRevision);
            if (rev == null) return NotFound("Revisión no existe.");

            if (string.IsNullOrWhiteSpace(rev.RutaFisica) || !System.IO.File.Exists(rev.RutaFisica))
                return NotFound("Esta revisión no tiene archivo adjunto.");

            var bytes = await System.IO.File.ReadAllBytesAsync(rev.RutaFisica);
            return File(bytes, rev.ContentType ?? "application/octet-stream", rev.NombreOriginal ?? "revision");
        }


        
// ===============================
// PUT: api/Entregables/{idEntregable}/estado
// Cambia el estado de la cabecera del entregable (estado "maestro" del REPORTE)
// - En ANTEPROYECTO: lo controla el revisor de anteproyecto.
// - En parciales/final: SOLO asesor interno puede marcar APROBADO (no existe RECHAZADO/CANCELADO).
// ===============================
[Authorize]
[HttpPut("{idEntregable:int}/estado")]
public async Task<IActionResult> UpdateEstadoEntregable(int idEntregable, [FromBody] UpdateEntregableEstadoFormDto dto)
{
    if (dto == null) return BadRequest("Body requerido.");

    var clave = (dto.EstadoClave ?? "").Trim().ToUpperInvariant();
    if (string.IsNullOrWhiteSpace(clave))
        return BadRequest("estadoClave es requerido.");

    if (!EstadoEntregableMap.ContainsKey(clave))
        return BadRequest("EstadoEntregable inválido.");

    var ent = await _db.Entregables.FirstOrDefaultAsync(e => e.Id == idEntregable);
    if (ent == null) return NotFound("Entregable no existe.");

    var anteTipoId = await _db.TipoEntregables
        .Where(t => t.Activo && t.Descripcion.ToUpper() == "ANTEPROYECTO")
        .Select(t => t.Id)
        .FirstOrDefaultAsync();

    var esAnteproyecto = (anteTipoId > 0 && ent.IdTipoEntregable == anteTipoId) || ent.IdTipoEntregable == 1;

    // Restricción: parciales/final NO se rechazan ni cancelan
    if (!esAnteproyecto && (clave == "RECHAZADO" || clave == "CANCELADO"))
        return BadRequest("En reportes parciales/final solo se permite EN_REVISION, CAMBIOS o APROBADO.");

    var idUsuario = GetUserId();
    if (idUsuario <= 0) return Unauthorized("No se pudo leer idUsuario del token.");

    var docente = await _db.Docentes.FirstOrDefaultAsync(d => d.idUsuario == idUsuario);
    if (docente == null) return Forbid("No eres docente.");

    // Validar rol de docente dentro del proyecto para poder cambiar estado
    if (esAnteproyecto)
    {
        var esRevisorAnte = await (
            from pd in _db.ProyectoDocente
            join tr in _db.TipoRelacionDocenteProyecto on pd.IdTipoRelacion equals tr.Id
            where pd.idProyecto == ent.IdProyecto
                  && pd.idDocente == docente.Id
                  && tr.Clave == "REVISOR_ANTEPROYECTO"
            select pd
        ).AnyAsync();

        if (!esRevisorAnte)
            return Forbid("Solo el revisor de anteproyecto puede cambiar el estado del anteproyecto.");
    }
    else
    {
        var esAsesor = await (
            from pd in _db.ProyectoDocente
            join tr in _db.TipoRelacionDocenteProyecto on pd.IdTipoRelacion equals tr.Id
            where pd.idProyecto == ent.IdProyecto
                  && pd.idDocente == docente.Id
                  && tr.Clave == "ASESOR_INTERNO"
            select pd
        ).AnyAsync();

        if (!esAsesor)
            return Forbid("Solo el asesor interno puede aprobar/cambiar el estado del reporte.");
    }

    ent.IdEstadoEntregable = EstadoIdOrThrow(clave);
    await _db.SaveChangesAsync();

    return Ok(new
    {
        ok = true,
        ent.Id,
        ent.IdProyecto,
        ent.IdTipoEntregable,
        ent.IdEstadoEntregable,
        EstadoClave = clave
    });
}

// ===============================
        // PUT: api/Entregables/versiones/{idEntregableVersion}/reemplazar
        // Reemplaza archivo de una versión (NO crea nueva versión)
        // Solo permitido si NO hay revisor de anteproyecto asignado
        // ===============================
        [HttpPut("versiones/{idEntregableVersion:int}/reemplazar")]
        [RequestSizeLimit(50_000_000)]
        public async Task<IActionResult> ReemplazarArchivoVersion(
            int idEntregableVersion,
            [FromForm] UploadEntregableVersionDto dto)
        {
            if (dto == null) return BadRequest("Body requerido.");
            if (dto.Archivo == null || dto.Archivo.Length == 0) return BadRequest("Archivo inválido.");

            // 1) versión
            var version = await _db.EntregableVersiones.FirstOrDefaultAsync(v => v.Id == idEntregableVersion);
            if (version == null) return NotFound("Versión no existe.");
            // 2) entregable
            var entregable = await _db.Entregables.FirstOrDefaultAsync(e => e.Id == version.IdEntregable);
            if (entregable == null) return Conflict("Entregable no existe.");

            // 3) validar que sea ANTEPROYECTO
            var anteTipoId = await _db.TipoEntregables
                .Where(t => t.Activo && t.Descripcion.ToUpper() == "ANTEPROYECTO")
                .Select(t => t.Id)
                .FirstOrDefaultAsync();

            if (anteTipoId <= 0) return BadRequest("No existe TipoEntregable 'ANTEPROYECTO' activo.");
            if (entregable.IdTipoEntregable != anteTipoId)
                return Conflict("Solo se permite reemplazo para ANTEPROYECTO.");

            // 4) validar estudiante pertenece al proyecto
            var estudiante = await _db.Estudiantes.FirstOrDefaultAsync(e => e.id == dto.IdEstudianteSubio);
            if (estudiante == null) return NotFound("Estudiante no existe.");
            if (estudiante.idProyecto != entregable.IdProyecto)
                return BadRequest("El estudiante no pertenece al proyecto del entregable.");

            // 5) bloquear si ya tiene revisiones (por seguridad)
            var tieneRevisiones = await _db.RevisionEntregables.AnyAsync(r => r.IdEntregableVersion == version.Id);
            if (tieneRevisiones)
                return Conflict("No se puede reemplazar: la versión ya tiene revisiones.");

            // 6) bloquear si ya hay revisor asignado al anteproyecto
            var hayRevisorAnte = await (
                from pd in _db.ProyectoDocente
                join tr in _db.TipoRelacionDocenteProyecto on pd.IdTipoRelacion equals tr.Id
                where pd.idProyecto == entregable.IdProyecto
                      && tr.Clave == "REVISOR_ANTEPROYECTO"
                select pd
            ).AnyAsync();

            if (hayRevisorAnte)
                return Conflict("No se puede reemplazar: ya existe revisor de anteproyecto asignado.");

            // 7) borrar archivo anterior si existe
            try
            {
                if (!string.IsNullOrWhiteSpace(version.RutaFisica) && System.IO.File.Exists(version.RutaFisica))
                    System.IO.File.Delete(version.RutaFisica);
            }
            catch
            {
                // opcional: log
            }

            // 8) guardar nuevo archivo
            var root = Path.Combine(_env.ContentRootPath, "Uploads", "Entregables",
                entregable.IdProyecto.ToString(), entregable.Id.ToString());

            Directory.CreateDirectory(root);

            var ext = Path.GetExtension(dto.Archivo.FileName);
            var safeExt = string.IsNullOrWhiteSpace(ext) ? "" : ext;
            var nombreServidor = $"{Guid.NewGuid():N}{safeExt}";
            var rutaFisica = Path.Combine(root, nombreServidor);

            await using (var fs = new FileStream(rutaFisica, FileMode.Create))
                await dto.Archivo.CopyToAsync(fs);

            // 9) actualizar registro (MISMA versión)
            version.NombreOriginal = dto.Archivo.FileName;
            version.NombreServidor = nombreServidor;
            version.ContentType = dto.Archivo.ContentType ?? "application/octet-stream";
            version.TamanoBytes = dto.Archivo.Length;
            version.RutaFisica = rutaFisica;
            version.FechaSubida = DateTime.Now;

            // 10) reset estado del entregable a EN_REVISION (por si quieres)
            entregable.IdEstadoEntregable = EstadoIdOrThrow("EN_REVISION");

            await _db.SaveChangesAsync();

            return Ok(new
            {
                ok = true,
                version.Id,
                version.NumeroVersion,
                version.NombreOriginal,
                version.ContentType,
                version.TamanoBytes,
                version.FechaSubida
            });
        }


    }
}
