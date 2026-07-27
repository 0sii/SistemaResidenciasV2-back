// ═══════════════════════════════════════════════════════════════════════
// ProyectosController.Oficios.cs
//
// INSTRUCCIONES DE INTEGRACIÓN:
//
// 1) En tu ProyectosController.cs ORIGINAL, cambia:
//        public class ProyectosController : ControllerBase
//    por:
//        public partial class ProyectosController : ControllerBase
//
// 2) Copia este archivo junto a él (mismo namespace, misma carpeta).
//    Comparte _context, _pdf, los ESTADO_* y GetUserIdOrNull() porque
//    son "partial" del mismo tipo.
//
// 3) DENTRO de tu método AutoAsignarme(...) existente, reemplaza el bloque
//    completo que va desde:
//
//        byte[] pdfBytes; string pdfFileName;
//
//        if (clave == "ASESOR_INTERNO")
//        { ... }
//        else if (clave == "REVISOR_ANTEPROYECTO")
//        { ... }
//        else // REVISOR_RESIDENCIA
//        { ... }
//
//        return File(pdfBytes, "application/pdf", pdfFileName);
//
//    por ESTO (una sola línea, delega todo al helper consolidado):
//
//        var (pdfBytes, pdfFileName) = await GenerarOficioConsolidadoAsync(docente.Id, clave);
//        return File(pdfBytes, "application/pdf", pdfFileName);
//
//    Esto es lo que logra el punto clave que pediste: cuando el docente ya
//    tiene varios proyectos asignados con ese mismo rol, el oficio que se
//    genera automáticamente al asignarse ya NO es "solo del proyecto nuevo",
//    sino de TODOS sus proyectos vigentes para ese rol, en un solo PDF.
//
// 4) El bloque de arriba de "Guardar relación" (donde ya se hace el
//    _context.SaveChangesAsync()) NO se toca: sigue igual.
// ═══════════════════════════════════════════════════════════════════════

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApiVinculacionProyectosV2.Models;

namespace WebApiVinculacionProyectosV2.Controllers
{
    public partial class ProyectosController
    {
        // ═══════════════════════════════════════════════════════════════
        // Núcleo reutilizable: arma el oficio consolidado de un docente
        // para un rol (tipoClave), con TODOS los proyectos vigentes que
        // tiene asignados en ese rol.
        // ═══════════════════════════════════════════════════════════════
        private async Task<(byte[] bytes, string fileName)> GenerarOficioConsolidadoAsync(int idDocente, string tipoClave)
        {
            var clave = (tipoClave ?? "").Trim().ToUpperInvariant();
            if (clave != "ASESOR_INTERNO" && clave != "REVISOR_ANTEPROYECTO" && clave != "REVISOR_RESIDENCIA")
                throw new ArgumentException("tipoClave debe ser ASESOR_INTERNO, REVISOR_ANTEPROYECTO o REVISOR_RESIDENCIA.");

            var docente = await _context.Docentes.AsNoTracking().FirstOrDefaultAsync(d => d.Id == idDocente)
                ?? throw new KeyNotFoundException("Docente no encontrado.");

            var tipo = await _context.TipoRelacionDocenteProyecto.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Clave == clave && t.Activo)
                ?? throw new KeyNotFoundException($"Tipo de relación '{clave}' no configurado.");

            // Todos los proyectos donde este docente tiene ese rol vigente
            var idsProyectos = await _context.ProyectoDocente.AsNoTracking()
                .Where(pd => pd.idDocente == idDocente && pd.IdTipoRelacion == tipo.Id)
                .Select(pd => pd.idProyecto)
                .Distinct()
                .ToListAsync();

            if (idsProyectos.Count == 0)
                throw new InvalidOperationException("El docente no tiene proyectos asignados con ese rol.");

            var proyectos = await _context.Proyectos.AsNoTracking()
                .Where(p => idsProyectos.Contains(p.Id))
                .ToListAsync();

            // Periodo académico + membretado: se toma el del proyecto más reciente
            // (asumiendo que normalmente todos los proyectos de un mismo oficio
            // pertenecen al mismo periodo; si no, se generan oficios separados
            // por periodo — ver nota al final).
            var periodoIds = proyectos.Select(p => p.IdPeriodoAcademico).Distinct().ToList();

            var docenteNombre = $"{docente.Nombre} {docente.ApellidoPaterno} {docente.ApellidoMaterno}".Trim().ToUpperInvariant();

            byte[] pdfBytes; string pdfFileName;

            // Si hay varios periodos académicos mezclados, generamos un oficio
            // por periodo y los concatenamos (mismo docente, mismo rol).
            using var outMs = new MemoryStream();
            using var outWriter = new iText.Kernel.Pdf.PdfWriter(outMs);
            using var outPdf = new iText.Kernel.Pdf.PdfDocument(outWriter);

            string ultimoOficio = "";

            foreach (var periodoId in periodoIds)
            {
                var proyectosDelPeriodo = proyectos.Where(p => p.IdPeriodoAcademico == periodoId).ToList();

                var periodo = await _context.PeriodosAcademicos.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == periodoId);

                var mem = await _context.PeriodosMembrentados.AsNoTracking()
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefaultAsync(x => x.PeriodoAcademicoId == periodoId);

                if (mem == null || mem.PdfBytes == null || mem.PdfBytes.Length == 0)
                    throw new InvalidOperationException(
                        $"No se encontró el membretado para el periodo académico {periodo?.Nombre ?? periodoId?.ToString()}. Súbelo en Períodos.");

                var periodoTxt = periodo?.Nombre?.ToUpperInvariant() ?? "—";
                var firmaNombre = periodo?.JefeDepartamentoNombre?.Trim() ?? "JEFA(E) DEL DEPARTAMENTO";

                var numeroOficio = $"{(periodo?.PrefijoOficio ?? "JV")}-{(periodo?.ConsecutivoOficio ?? 1):000}/{(DateTime.Today.Year % 100):00}";
                if (periodo != null)
                {
                    var periodoUpd = await _context.PeriodosAcademicos.FindAsync(periodo.Id);
                    if (periodoUpd != null) { periodoUpd.ConsecutivoOficio++; await _context.SaveChangesAsync(); }
                }
                ultimoOficio = numeroOficio;

                byte[] pdfDelPeriodo;

                if (clave == "ASESOR_INTERNO")
                {
                    var itemsReq = new List<ProyectoAsesorInternoItem>();
                    foreach (var p in proyectosDelPeriodo)
                    {
                        var (estudiantes, empresa, carrera) = await ObtenerDatosProyectoAsync(p);
                        itemsReq.Add(new ProyectoAsesorInternoItem
                        {
                            NombreProyecto = p.Titulo ?? "—",
                            Empresa = empresa,
                            Carrera = carrera,
                            PeriodoRealizacion = periodoTxt,
                            Residentes = estudiantes.Select(e => e.Nombre.Trim()).ToList()
                        });
                    }

                    var req = new OficioAsignacionAsesorInternoConsolidadoRequest
                    {
                        Fecha = DateTime.Today,
                        Oficio = numeroOficio,
                        DestinatarioNombre = docenteNombre,
                        DestinatarioCargoLinea1 = "DOCENTE DEL DEPARTAMENTO DE SISTEMAS Y COMPUTACIÓN",
                        Proyectos = itemsReq,
                        FirmaNombre = firmaNombre,
                        FirmaCargoLinea1 = "JEFA(E) DEL DEPARTAMENTO",
                        FirmaCargoLinea2 = "DE SISTEMAS Y COMPUTACIÓN"
                    };
                    pdfDelPeriodo = _pdf.BuildOficioAsignacionAsesorInternoConsolidado(mem.PdfBytes, req);
                }
                else if (clave == "REVISOR_ANTEPROYECTO")
                {
                    var itemsReq = new List<ProyectoRevisorReportePreliminarItem>();
                    foreach (var p in proyectosDelPeriodo)
                    {
                        var (estudiantes, _, _) = await ObtenerDatosProyectoAsync(p);
                        var estReq = estudiantes.Select(e => new RevisorReportePreliminarEstudianteItem
                        {
                            NumeroControl = e.NoControl ?? "—",
                            NombreEstudiante = e.Nombre.Trim()
                        }).ToList();
                        if (!estReq.Any()) estReq.Add(new RevisorReportePreliminarEstudianteItem { NumeroControl = "—", NombreEstudiante = "—" });

                        itemsReq.Add(new ProyectoRevisorReportePreliminarItem
                        {
                            NombreProyecto = p.Titulo ?? "—",
                            Estudiantes = estReq
                        });
                    }

                    var req = new OficioAsignacionRevisorReportePreliminarConsolidadoRequest
                    {
                        Fecha = DateTime.Today,
                        Oficio = numeroOficio,
                        DestinatarioNombre = docenteNombre,
                        DestinatarioCargoLinea1 = "CATEDRATICO(A) DEL I.T. DE OAXACA",
                        Proyectos = itemsReq,
                        FirmaNombre = firmaNombre,
                        // ⚠️ FIX: 'firmaNombre' viene de PeriodoAcademico.JefeDepartamentoNombre —
                        // no existe en el modelo un campo para la Subdirectora Académica, así que
                        // poner "SUBDIRECTORA ACADÉMICA" aquí generaba un nombre y cargo que no
                        // corresponden a la misma persona. Se usa el cargo que sí coincide con el
                        // nombre disponible. Si más adelante agregan un campo real para la
                        // Subdirección Académica, cambien esto para usar ese nombre + este cargo.
                        FirmaCargoLinea1 = "JEFA(E) DEL DEPARTAMENTO DE SISTEMAS Y COMPUTACIÓN"
                    };
                    pdfDelPeriodo = _pdf.BuildOficioAsignacionRevisorReportePreliminarConsolidado(mem.PdfBytes, req);
                }
                else // REVISOR_RESIDENCIA — reutiliza el builder "formato foto" que ya pagina varias filas
                {
                    var rows = new List<OficioRevisorRow>();
                    foreach (var p in proyectosDelPeriodo)
                    {
                        var (estudiantes, _, _) = await ObtenerDatosProyectoAsync(p);

                        var asesorNombre = await (from pd in _context.ProyectoDocente.AsNoTracking()
                                                   join t in _context.TipoRelacionDocenteProyecto.AsNoTracking() on pd.IdTipoRelacion equals t.Id
                                                   join d in _context.Docentes.AsNoTracking() on pd.idDocente equals d.Id
                                                   where pd.idProyecto == p.Id && t.Clave == "ASESOR_INTERNO"
                                                   select (d.Nombre ?? "") + " " + (d.ApellidoPaterno ?? "") + " " + (d.ApellidoMaterno ?? "")
                                                  ).FirstOrDefaultAsync();

                        if (estudiantes.Count == 0)
                        {
                            rows.Add(new OficioRevisorRow { NoControl = "—", Estudiante = "—", Proyecto = p.Titulo ?? "—", Asesor = string.IsNullOrWhiteSpace(asesorNombre) ? "Por asignar" : asesorNombre.Trim() });
                        }
                        else
                        {
                            foreach (var e in estudiantes)
                                rows.Add(new OficioRevisorRow
                                {
                                    NoControl = e.NoControl ?? "—",
                                    Estudiante = e.Nombre.Trim(),
                                    Proyecto = p.Titulo ?? "—",
                                    Asesor = string.IsNullOrWhiteSpace(asesorNombre) ? "Por asignar" : asesorNombre.Trim()
                                });
                        }
                    }

                    var req = new OficiosAsignacionRevisoresRequest
                    {
                        Oficio = numeroOficio,
                        Asunto = "Revisor de Residencia Profesional",
                        FirmaNombre = firmaNombre,
                        FirmaCargoLinea1 = "JEFA(E) DEL DEPARTAMENTO DE SISTEMAS Y COMPUTACIÓN",
                        Revisores = new List<OficioRevisorItem>
                        {
                            new OficioRevisorItem
                            {
                                RevisorNombre = docenteNombre,
                                RevisorCargoLinea1 = "DOCENTE DEL DEPARTAMENTO DE SISTEMAS Y COMPUTACIÓN",
                                Rows = rows
                            }
                        }
                    };
                    // Formato "foto" pagina automáticamente si hay muchos proyectos.
                    pdfDelPeriodo = _pdf.BuildOficiosAsignacionRevisoresFormatoFoto(mem.PdfBytes, req);
                }

                using var srcMs = new MemoryStream(pdfDelPeriodo);
                using var srcReader = new iText.Kernel.Pdf.PdfReader(srcMs);
                using var srcPdf = new iText.Kernel.Pdf.PdfDocument(srcReader);
                srcPdf.CopyPagesTo(1, srcPdf.GetNumberOfPages(), outPdf);
            }

            outPdf.Close();
            pdfBytes = outMs.ToArray();

            var etiquetaRol = clave switch
            {
                "ASESOR_INTERNO" => "AsesorInterno",
                "REVISOR_ANTEPROYECTO" => "RevisorAnteproyecto",
                _ => "RevisorResidencia"
            };
            pdfFileName = $"Oficio_{etiquetaRol}_{docenteNombre.Replace(" ", "_")}_{ultimoOficio.Replace("/", "-")}.pdf";

            return (pdfBytes, pdfFileName);
        }

        private async Task<(List<(string NoControl, string Nombre)> estudiantes, string empresa, string carrera)>
            ObtenerDatosProyectoAsync(Proyectos proyecto)
        {
            var estudiantesRaw = await (
                from e in _context.Estudiantes.AsNoTracking()
                where e.idProyecto == proyecto.Id
                select new { e.noControl, Nombre = (e.Nombre ?? "") + " " + (e.ApellidoPaterno ?? "") + " " + (e.ApellidoMaterno ?? "") }
            ).ToListAsync();

            var estudiantes = estudiantesRaw.Select(e => (e.noControl ?? "—", e.Nombre)).ToList();

            var empresa = await _context.Empresas.AsNoTracking()
                .Where(em => em.Id == proyecto.IdEmpresa).Select(em => em.Nombre)
                .FirstOrDefaultAsync() ?? "—";

            var carrera = await (
                from est in _context.Estudiantes.AsNoTracking()
                join car in _context.Carreras.AsNoTracking() on est.idcarrera equals car.Id into cars
                from car in cars.DefaultIfEmpty()
                where est.idProyecto == proyecto.Id
                select car != null ? car.Descripcion : null
            ).FirstOrDefaultAsync() ?? "—";

            return (estudiantes, empresa, carrera);
        }

        // ═══════════════════════════════════════════════════════════════
        // GET /api/Proyectos/Oficios/Regenerar?tipoClave=
        //
        // Auto-servicio: el docente logueado regenera SU PROPIO oficio
        // consolidado. No requiere ningún permiso especial, solo estar
        // autenticado y tener un registro de Docente asociado.
        // ═══════════════════════════════════════════════════════════════
        [Authorize]
        [HttpGet("Oficios/Regenerar")]
        public async Task<IActionResult> RegenerarMiOficioConsolidado([FromQuery] string tipoClave)
        {
            if (string.IsNullOrWhiteSpace(tipoClave))
                return BadRequest("tipoClave es obligatorio: ASESOR_INTERNO, REVISOR_ANTEPROYECTO o REVISOR_RESIDENCIA.");

            var userId = GetUserIdOrNull();
            if (userId == null) return Unauthorized();

            var docente = await _context.Docentes.AsNoTracking()
                .FirstOrDefaultAsync(d => d.idUsuario == userId.Value);
            if (docente == null) return NotFound("No existe docente para este usuario.");

            try
            {
                var (bytes, fileName) = await GenerarOficioConsolidadoAsync(docente.Id, tipoClave);
                return File(bytes, "application/pdf", fileName);
            }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
        }

        // ═══════════════════════════════════════════════════════════════
        // GET /api/Proyectos/Oficios/RegenerarDeDocente?idDocente=&tipoClave=
        //
        // Para la jefa de vinculación: genera el oficio consolidado de
        // CUALQUIER docente. Protegido con el mismo esquema de permisos
        // que el resto del proyecto (PERM:Proyecto-Update).
        // ═══════════════════════════════════════════════════════════════
        [Authorize(Policy = "PERM:Proyecto-Update")]
        [HttpGet("Oficios/RegenerarDeDocente")]
        public async Task<IActionResult> RegenerarOficioConsolidadoDeDocente([FromQuery] int idDocente, [FromQuery] string tipoClave)
        {
            if (idDocente <= 0) return BadRequest("idDocente es obligatorio.");
            if (string.IsNullOrWhiteSpace(tipoClave))
                return BadRequest("tipoClave es obligatorio: ASESOR_INTERNO, REVISOR_ANTEPROYECTO o REVISOR_RESIDENCIA.");

            try
            {
                var (bytes, fileName) = await GenerarOficioConsolidadoAsync(idDocente, tipoClave);
                return File(bytes, "application/pdf", fileName);
            }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
        }

        // ═══════════════════════════════════════════════════════════════
        // GET /api/Proyectos/Oficios/PendientesPorDocente?tipoClave=
        //
        // Para la pantalla de la jefa de vinculación: lista de docentes que
        // tienen al menos un proyecto con ese rol, con el conteo de
        // proyectos, para poder generar el oficio consolidado de cada uno.
        // ═══════════════════════════════════════════════════════════════
        [Authorize(Policy = "PERM:Proyecto-Update")]
        [HttpGet("Oficios/PendientesPorDocente")]
        public async Task<IActionResult> DocentesConAsignacion([FromQuery] string tipoClave)
        {
            if (string.IsNullOrWhiteSpace(tipoClave))
                return BadRequest("tipoClave es obligatorio.");

            var clave = tipoClave.Trim().ToUpperInvariant();
            var tipo = await _context.TipoRelacionDocenteProyecto.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Clave == clave && t.Activo);
            if (tipo == null) return NotFound("Tipo de relación no encontrado.");

            var data = await (
                from pd in _context.ProyectoDocente.AsNoTracking()
                join d in _context.Docentes.AsNoTracking() on pd.idDocente equals d.Id
                where pd.IdTipoRelacion == tipo.Id
                group pd by new { d.Id, d.Nombre, d.ApellidoPaterno, d.ApellidoMaterno } into g
                select new
                {
                    IdDocente = g.Key.Id,
                    Nombre = g.Key.Nombre + " " + g.Key.ApellidoPaterno + " " + g.Key.ApellidoMaterno,
                    NumProyectos = g.Select(x => x.idProyecto).Distinct().Count()
                }
            ).OrderBy(x => x.Nombre).ToListAsync();

            return Ok(data);
        }

        // ═══════════════════════════════════════════════════════════════
        // POST /api/Proyectos/Oficios/AceptacionReportePreliminar
        //
        // El asesor interno acepta (dictamina) el reporte preliminar de uno
        // o varios de SUS proyectos en un solo trámite. Se genera un solo
        // PDF con una página por proyecto/estudiante (mismo layout que ya
        // tenías, solo que ahora puede traer varias páginas).
        //
        // Body: [{ idProyecto, tituloReporte, fechaInicio, fechaTermino,
        //           dictamen: "APROBADO"|"NO_APROBADO", comentarios }]
        // ═══════════════════════════════════════════════════════════════
        [Authorize]
        [HttpPost("Oficios/AceptacionReportePreliminar")]
        public async Task<IActionResult> GenerarAceptacionesReportePreliminar([FromBody] List<AceptacionReporteItemDto> items)
        {
            if (items == null || items.Count == 0)
                return BadRequest("Debes incluir al menos un proyecto.");

            var userId = GetUserIdOrNull();
            if (userId == null) return Unauthorized();

            var docente = await _context.Docentes.AsNoTracking().FirstOrDefaultAsync(d => d.idUsuario == userId.Value);
            if (docente == null) return NotFound("No existe docente para este usuario.");

            var docenteNombre = $"{docente.Nombre} {docente.ApellidoPaterno} {docente.ApellidoMaterno}".Trim();

            var solicitudes = new List<ConstanciaAceptacionReportePreliminarRequest>();
            byte[]? membretePdf = null;

            foreach (var item in items)
            {
                var proyecto = await _context.Proyectos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == item.IdProyecto);
                if (proyecto == null) return NotFound($"Proyecto {item.IdProyecto} no encontrado.");

                // Verifica que el docente logueado sea el asesor interno del proyecto
                var tipoAsesor = await _context.TipoRelacionDocenteProyecto.AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Clave == "ASESOR_INTERNO" && t.Activo);
                if (tipoAsesor == null) return NotFound("Tipo ASESOR_INTERNO no configurado.");

                var esAsesor = await _context.ProyectoDocente.AsNoTracking().AnyAsync(pd =>
                    pd.idProyecto == item.IdProyecto && pd.idDocente == docente.Id && pd.IdTipoRelacion == tipoAsesor.Id);
                if (!esAsesor) return Forbid();

                var (estudiantes, _, carrera) = await ObtenerDatosProyectoAsync(proyecto);
                var estudiante = estudiantes.FirstOrDefault();

                if (membretePdf == null)
                {
                    var mem = await _context.PeriodosMembrentados.AsNoTracking()
                        .OrderByDescending(x => x.Id)
                        .FirstOrDefaultAsync(x => x.PeriodoAcademicoId == proyecto.IdPeriodoAcademico);
                    if (mem?.PdfBytes != null && mem.PdfBytes.Length > 0) membretePdf = mem.PdfBytes;
                }

                solicitudes.Add(new ConstanciaAceptacionReportePreliminarRequest
                {
                    Fecha = DateTime.Today,
                    Carrera = carrera,
                    NoControl = estudiante.NoControl ?? "—",
                    Estudiante = estudiante.Nombre ?? "—",
                    TituloReporte = item.TituloReporte ?? proyecto.Titulo ?? "—",
                    FechaInicio = item.FechaInicio ?? "",
                    FechaTermino = item.FechaTermino ?? "",
                    Dictamen = string.IsNullOrWhiteSpace(item.Dictamen) ? "APROBADO" : item.Dictamen,
                    Comentarios = item.Comentarios ?? "",
                    AsesorInterno = docenteNombre
                });
            }

            if (membretePdf == null)
                return Conflict("No se encontró el membretado para el periodo académico de estos proyectos.");

            var pdfBytes = _pdf.BuildConstanciasAceptacionReportePreliminarConsolidado(membretePdf, solicitudes);
            var pdfFileName = $"Aceptacion_ReportePreliminar_{docenteNombre.Replace(" ", "_")}.pdf";

            return File(pdfBytes, "application/pdf", pdfFileName);
        }

        public class AceptacionReporteItemDto
        {
            public int IdProyecto { get; set; }
            public string? TituloReporte { get; set; }
            public string? FechaInicio { get; set; }
            public string? FechaTermino { get; set; }
            public string? Dictamen { get; set; }
            public string? Comentarios { get; set; }
        }
    }
}
