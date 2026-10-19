// ═══════════════════════════════════════════════════════════════════════
// ProyectosController.Oficios.cs
//
// Generación de oficios (asesor interno, revisor de reporte preliminar y
// revisor de residencia).
//
// QUIÉN PUEDE GENERARLOS:
//   • Estudiantes: solo los de SU proyecto (desde Seguimiento).
//   • Jefa de vinculación (PERM:Proyecto-Update): cualquier proyecto/docente.
//   • Docentes: NINGUNO. No tienen endpoint para generar oficios.
//
// Requiere que ProyectosController sea "partial".
// ═══════════════════════════════════════════════════════════════════════

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
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
        private async Task<(byte[] bytes, string fileName)> GenerarOficioConsolidadoAsync(int idDocente, string tipoClave, int? soloIdProyecto = null, int? soloIdEstudiante = null)
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

            // Si se pide un solo proyecto (oficio del estudiante), se limita a ese.
            if (soloIdProyecto.HasValue)
                idsProyectos = idsProyectos.Where(i => i == soloIdProyecto.Value).ToList();

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

                if (periodo == null)
                    throw new KeyNotFoundException("Periodo académico no encontrado.");

                // Número y fecha FIJOS: la fecha representa el evento que originó la asignación
                // (FechaInscripcion de ProyectoDocente), NO el día en que se descarga el PDF.
                var oficioEmitido = await ObtenerOCrearOficioEmitidoAsync(periodo.Id, clave, idDocente, soloIdProyecto ?? 0, soloIdEstudiante ?? 0);
                var numeroOficio = oficioEmitido.NumeroOficio;
                var fechaOficio = oficioEmitido.FechaEmision;
                ultimoOficio = numeroOficio;

                byte[] pdfDelPeriodo;

                if (clave == "ASESOR_INTERNO")
                {
                    var itemsReq = new List<ProyectoAsesorInternoItem>();
                    foreach (var p in proyectosDelPeriodo)
                    {
                        // Un oficio por alumno: si viene soloIdEstudiante solo aparece ese alumno.
                        var (estudiantes, empresa, carrera) = await ObtenerDatosProyectoAsync(p, soloIdEstudiante);
                        itemsReq.Add(new ProyectoAsesorInternoItem
                        {
                            NombreProyecto = p.Titulo ?? "—",
                            Empresa = empresa,
                            Carrera = carrera,
                            PeriodoRealizacion = periodoTxt,
                            NumerosControl = estudiantes.Select(e => e.NoControl ?? "—").ToList(),
                            Residentes = estudiantes.Select(e => e.Nombre.Trim()).ToList()
                        });
                    }

                    var req = new OficioAsignacionAsesorInternoConsolidadoRequest
                    {
                        Fecha = fechaOficio,
                        Oficio = numeroOficio,
                        DestinatarioNombre = docenteNombre,
                        DestinatarioCargoLinea1 = "CATEDRATICO(A) DEL I.T. DE OAXACA",
                        Proyectos = itemsReq,
                        FirmaNombre = firmaNombre,
                        FirmaCargoLinea1 = "JEFE DEL DEPARTAMENTO DE SISTEMAS Y COMPUTACIÓN",
                        FirmaCargoLinea2 = ""
                    };
                    pdfDelPeriodo = _pdf.BuildOficioAsignacionAsesorInternoConsolidado(mem.PdfBytes, req);
                }
                else if (clave == "REVISOR_ANTEPROYECTO")
                {
                    var itemsReq = new List<ProyectoRevisorReportePreliminarItem>();
                    foreach (var p in proyectosDelPeriodo)
                    {
                        var (estudiantes, _, _) = await ObtenerDatosProyectoAsync(p, soloIdEstudiante);
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
                        Fecha = fechaOficio,
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
                        FirmaCargoLinea1 = "JEFE DEL DEPARTAMENTO DE SISTEMAS Y COMPUTACIÓN"
                    };
                    pdfDelPeriodo = _pdf.BuildOficioAsignacionRevisorReportePreliminarConsolidado(mem.PdfBytes, req);

                    // Guardar el Nº de oficio en cada proyecto incluido: la "Aceptación del Reporte
                    // Preliminar" de esos proyectos lo cita como "En respuesta al oficio ___".
                    var idsConOficio = proyectosDelPeriodo.Select(x => x.Id).ToList();
                    var proyectosTracked = await _context.Proyectos.Where(x => idsConOficio.Contains(x.Id)).ToListAsync();
                    // El oficio individual (el del estudiante) siempre manda; el consolidado de la
                    // jefa solo llena el campo si estaba vacío, para no cambiar el número que ya
                    // cita una Aceptación generada.
                    foreach (var pt in proyectosTracked)
                        if (soloIdProyecto.HasValue || string.IsNullOrWhiteSpace(pt.OficioRevisorReportePreliminar))
                            pt.OficioRevisorReportePreliminar = numeroOficio;
                    await _context.SaveChangesAsync();
                }
                else // REVISOR_RESIDENCIA — reutiliza el builder "formato foto" que ya pagina varias filas
                {
                    var rows = new List<OficioRevisorRow>();
                    foreach (var p in proyectosDelPeriodo)
                    {
                        var (estudiantes, _, _) = await ObtenerDatosProyectoAsync(p, soloIdEstudiante);

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
                        FirmaCargoLinea1 = "JEFE DEL DEPARTAMENTO DE SISTEMAS Y COMPUTACIÓN",
                        Revisores = new List<OficioRevisorItem>
                        {
                            new OficioRevisorItem
                            {
                                RevisorNombre = docenteNombre,
                                RevisorCargoLinea1 = "CATEDRATICO(A) DEL I.T. DE OAXACA",
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
            var sufijoProyecto = (soloIdProyecto.HasValue ? $"_P{soloIdProyecto.Value}" : "")
                               + (soloIdEstudiante.HasValue ? $"_E{soloIdEstudiante.Value}" : "");
            pdfFileName = $"Oficio_{etiquetaRol}_{docenteNombre.Replace(" ", "_")}{sufijoProyecto}_{ultimoOficio.Replace("/", "-")}.pdf";

            return (pdfBytes, pdfFileName);
        }

        // ═══════════════════════════════════════════════════════════════
        // Reglas de qué se genera por rol (todas con número de oficio fijo):
        //   • ASESOR_INTERNO       → UN oficio por alumno (2 alumnos = 2 oficios).
        //   • REVISOR_ANTEPROYECTO → UN oficio por proyecto, con todos sus integrantes.
        //   • REVISOR_RESIDENCIA   → UN oficio por docente con TODOS los proyectos
        //                            en los que es revisor.
        // soloIdProyecto limita a un proyecto (asesor y revisor de anteproyecto).
        // Si hay varios oficios, se devuelven unidos en un solo PDF.
        // ═══════════════════════════════════════════════════════════════
        private async Task<(byte[] bytes, string fileName)> GenerarOficiosDeDocenteAsync(int idDocente, string tipoClave, int? soloIdProyecto = null)
        {
            var clave = (tipoClave ?? "").Trim().ToUpperInvariant();

            // Revisor de residencia: siempre todos los proyectos del docente.
            if (clave == "REVISOR_RESIDENCIA")
                return await GenerarOficioConsolidadoAsync(idDocente, clave);

            if (clave != "ASESOR_INTERNO" && clave != "REVISOR_ANTEPROYECTO")
                throw new ArgumentException("tipoClave debe ser ASESOR_INTERNO, REVISOR_ANTEPROYECTO o REVISOR_RESIDENCIA.");

            var tipo = await _context.TipoRelacionDocenteProyecto.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Clave == clave && t.Activo)
                ?? throw new KeyNotFoundException($"Tipo de relación '{clave}' no configurado.");

            var idsProyectos = await _context.ProyectoDocente.AsNoTracking()
                .Where(pd => pd.idDocente == idDocente && pd.IdTipoRelacion == tipo.Id
                             && (soloIdProyecto == null || pd.idProyecto == soloIdProyecto))
                .Select(pd => pd.idProyecto)
                .Distinct()
                .OrderBy(i => i)
                .ToListAsync();

            if (idsProyectos.Count == 0)
                throw new InvalidOperationException("El docente no tiene proyectos asignados con ese rol.");

            var pdfs = new List<byte[]>();
            var nombreArchivo = "Oficio.pdf";

            foreach (var idProyecto in idsProyectos)
            {
                if (clave == "REVISOR_ANTEPROYECTO")
                {
                    var (b, n) = await GenerarOficioConsolidadoAsync(idDocente, clave, idProyecto);
                    if (pdfs.Count == 0) nombreArchivo = n;
                    pdfs.Add(b);
                    continue;
                }

                // ASESOR_INTERNO: un oficio por alumno
                var idsEstudiantes = await _context.Estudiantes.AsNoTracking()
                    .Where(e => e.idProyecto == idProyecto)
                    .OrderBy(e => e.id)
                    .Select(e => e.id)
                    .ToListAsync();

                foreach (var idEst in idsEstudiantes)
                {
                    var (b, n) = await GenerarOficioConsolidadoAsync(idDocente, clave, idProyecto, idEst);
                    if (pdfs.Count == 0) nombreArchivo = n;
                    pdfs.Add(b);
                }
            }

            if (pdfs.Count == 0)
                throw new InvalidOperationException("El proyecto no tiene estudiantes para generar el oficio.");

            return (pdfs.Count == 1 ? pdfs[0] : UnirPdfs(pdfs), nombreArchivo);
        }

        // ═══════════════════════════════════════════════════════════════
        // Número de oficio persistente.
        // Devuelve el oficio ya emitido para (periodo, rol, docente, proyecto) o, si
        // es la primera vez, toma el siguiente consecutivo del periodo y lo guarda.
        // idProyecto / idEstudiante = 0 → no aplica a ese nivel.
        // ═══════════════════════════════════════════════════════════════
        private async Task<OficioEmitido> ObtenerOCrearOficioEmitidoAsync(int idPeriodo, string clave, int idDocente, int idProyecto, int idEstudiante)
        {
            Task<OficioEmitido?> Buscar() => _context.OficiosEmitidos.AsNoTracking().FirstOrDefaultAsync(o =>
                o.IdPeriodoAcademico == idPeriodo && o.TipoClave == clave &&
                o.IdDocente == idDocente && o.IdProyecto == idProyecto && o.IdEstudiante == idEstudiante);

            var existente = await Buscar();

            // La fecha del oficio es la fecha en que se registró la asignación del docente.
            // Para un oficio consolidado de residencia con varios proyectos, usamos la fecha
            // más antigua de las asignaciones incluidas en ese oficio.
            var tipo = await _context.TipoRelacionDocenteProyecto.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Clave == clave && t.Activo)
                ?? throw new KeyNotFoundException($"Tipo de relación '{clave}' no configurado.");

            var fechasAsignacion = await _context.ProyectoDocente.AsNoTracking()
                .Where(pd => pd.idDocente == idDocente && pd.IdTipoRelacion == tipo.Id
                          && (idProyecto == 0 || pd.idProyecto == idProyecto))
                .Select(pd => pd.FechaInscripcion)
                .ToListAsync();

            var fechaAsignacion = fechasAsignacion.Count > 0
                ? fechasAsignacion.Min().ToDateTime(TimeOnly.MinValue).Date
                : DateTime.Today;

            // Si el oficio ya existía pero fue creado antes de esta corrección,
            // sincronizamos su fecha con la asignación real para que una descarga
            // posterior tampoco conserve una fecha de generación incorrecta.
            if (existente != null)
            {
                if (existente.FechaEmision.Date != fechaAsignacion)
                {
                    existente.FechaEmision = fechaAsignacion;
                    await _context.SaveChangesAsync();
                }
                return existente;
            }

            // Reintenta si dos peticiones simultáneas chocan por el mismo consecutivo
            // o por el mismo oficio (los índices únicos lo detectan).
            for (var intento = 0; intento < 5; intento++)
            {
                var periodo = await _context.PeriodosAcademicos.FirstAsync(p => p.Id == idPeriodo);

                var nuevo = new OficioEmitido
                {
                    IdPeriodoAcademico = idPeriodo,
                    TipoClave = clave,
                    IdDocente = idDocente,
                    IdProyecto = idProyecto,
                    IdEstudiante = idEstudiante,
                    NumeroOficio = $"{(string.IsNullOrWhiteSpace(periodo.PrefijoOficio) ? "JV" : periodo.PrefijoOficio)}-{periodo.ConsecutivoOficio:000}/{(fechaAsignacion.Year % 100):00}",
                    FechaEmision = fechaAsignacion
                };

                _context.OficiosEmitidos.Add(nuevo);
                periodo.ConsecutivoOficio++;

                try
                {
                    // Un solo SaveChanges = una sola transacción: o se guarda el oficio y el
                    // consecutivo avanza, o no pasa ninguna de las dos cosas.
                    await _context.SaveChangesAsync();
                    return nuevo;
                }
                catch (DbUpdateException)
                {
                    _context.Entry(nuevo).State = EntityState.Detached;
                    _context.Entry(periodo).State = EntityState.Detached;

                    var ganador = await Buscar();
                    if (ganador != null) return ganador;
                }
            }

            throw new InvalidOperationException("No se pudo asignar el número de oficio. Intenta de nuevo.");
        }

        private async Task<(List<(string NoControl, string Nombre)> estudiantes, string empresa, string carrera)>
            ObtenerDatosProyectoAsync(Proyectos proyecto, int? soloIdEstudiante = null)
        {
            var estudiantesRaw = await (
                from e in _context.Estudiantes.AsNoTracking()
                where e.idProyecto == proyecto.Id && (soloIdEstudiante == null || e.id == soloIdEstudiante)
                select new { e.noControl, Nombre = (e.ApellidoPaterno ?? "") + " " + (e.ApellidoMaterno ?? "") + " " + (e.Nombre ?? "") }
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
                var (bytes, fileName) = await GenerarOficiosDeDocenteAsync(idDocente, tipoClave);
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
        // GET /api/Proyectos/Oficios/ProyectosAceptacion
        //
        // Para la pantalla de la jefa de vinculación: proyectos cuyo reporte
        // preliminar ya fue aceptado (estado >= 6, sin cancelados), con sus
        // integrantes, para descargar la Aceptación (una por proyecto).
        // ═══════════════════════════════════════════════════════════════
        [Authorize(Policy = "PERM:Proyecto-Update")]
        [HttpGet("Oficios/ProyectosAceptacion")]
        public async Task<IActionResult> ProyectosConAceptacion()
        {
            const int ESTADO_DESDE_ACEPTACION = 6;
            const int ESTADO_CANCELADO = 9;

            var proyectos = await _context.Proyectos.AsNoTracking()
                .Where(p => p.idEstado != null && p.idEstado >= ESTADO_DESDE_ACEPTACION && p.idEstado != ESTADO_CANCELADO)
                .OrderBy(p => p.Titulo)
                .Select(p => new { p.Id, p.Titulo })
                .ToListAsync();

            var ids = proyectos.Select(p => p.Id).ToList();

            var integrantes = await _context.Estudiantes.AsNoTracking()
                .Where(e => e.idProyecto != null && ids.Contains(e.idProyecto.Value))
                .Select(e => new { IdProyecto = e.idProyecto!.Value, e.noControl, e.Nombre, e.ApellidoPaterno, e.ApellidoMaterno })
                .ToListAsync();

            var data = proyectos.Select(p => new
            {
                IdProyecto = p.Id,
                Titulo = p.Titulo ?? "—",
                Integrantes = string.Join(", ", integrantes
                    .Where(e => e.IdProyecto == p.Id)
                    .Select(e => $"{e.Nombre} {e.ApellidoPaterno} {e.ApellidoMaterno}".Trim()))
            }).ToList();

            return Ok(data);
        }

        // ═══════════════════════════════════════════════════════════════
        // GET /api/Proyectos/{idProyecto}/Oficios/Asignacion?tipoClave=
        //
        // Oficio de un proyecto específico para el rol indicado:
        //   ASESOR_INTERNO        → oficio de asignación de asesor interno
        //   REVISOR_RESIDENCIA    → oficio de asignación de revisor
        //   REVISOR_ANTEPROYECTO  → oficio de asignación de revisor del
        //                           reporte preliminar
        //
        // Pueden usarlo: el estudiante que pertenece al proyecto y la jefa
        // de vinculación (PERM:Proyecto-Update). Los docentes reciben 403.
        // ═══════════════════════════════════════════════════════════════
        [Authorize]
        [HttpGet("{idProyecto:int}/Oficios/Asignacion")]
        public async Task<IActionResult> GenerarOficioAsignacionDeProyecto(int idProyecto, [FromQuery] string tipoClave)
        {
            var clave = (tipoClave ?? "").Trim().ToUpperInvariant();
            if (clave != "ASESOR_INTERNO" && clave != "REVISOR_ANTEPROYECTO" && clave != "REVISOR_RESIDENCIA")
                return BadRequest("tipoClave debe ser ASESOR_INTERNO, REVISOR_ANTEPROYECTO o REVISOR_RESIDENCIA.");

            var userId = GetUserIdOrNull();
            if (userId == null) return Unauthorized();

            // ── Autorización: jefa de vinculación o estudiante del proyecto ──
            var authSvc = HttpContext.RequestServices.GetRequiredService<IAuthorizationService>();
            var esJefa = (await authSvc.AuthorizeAsync(User, null, "PERM:Proyecto-Update")).Succeeded;

            int? idEstudianteSolicitante = null;

            if (!esJefa)
            {
                // Identificamos al alumno exacto que está descargando el oficio.
                // Un proyecto puede tener varios integrantes, pero el dashboard
                // del estudiante debe generar únicamente su oficio.
                idEstudianteSolicitante = await _context.Estudiantes.AsNoTracking()
                    .Where(e => e.idUsuario == userId.Value && e.idProyecto == idProyecto)
                    .Select(e => (int?)e.id)
                    .FirstOrDefaultAsync();

                if (!idEstudianteSolicitante.HasValue) return Forbid();
            }

            var proyectoExiste = await _context.Proyectos.AsNoTracking().AnyAsync(p => p.Id == idProyecto);
            if (!proyectoExiste) return NotFound("Proyecto no encontrado.");

            var tipo = await _context.TipoRelacionDocenteProyecto.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Clave == clave && t.Activo);
            if (tipo == null) return NotFound($"Tipo de relación '{clave}' no configurado.");

            var idsDocentes = await _context.ProyectoDocente.AsNoTracking()
                .Where(pd => pd.idProyecto == idProyecto && pd.IdTipoRelacion == tipo.Id)
                .Select(pd => pd.idDocente)
                .Distinct()
                .ToListAsync();

            if (idsDocentes.Count == 0)
            {
                var rolTxt = clave switch
                {
                    "ASESOR_INTERNO" => "asesor interno",
                    "REVISOR_ANTEPROYECTO" => "revisor del reporte preliminar",
                    _ => "revisor de residencia"
                };
                return Conflict($"Aún no hay un docente asignado como {rolTxt} en este proyecto.");
            }

            try
            {
                var pdfs = new List<byte[]>();
                string nombreArchivo = "Oficio.pdf";

                foreach (var idDocente in idsDocentes)
                {
                    (byte[] bytes, string fileName) resultado;

                    if (esJefa)
                    {
                        // La jefa conserva la generación completa.
                        resultado = await GenerarOficiosDeDocenteAsync(idDocente, clave, idProyecto);
                    }
                    else
                    {
                        // Cualquier generación desde el dashboard de un estudiante
                        // queda limitada a SU proyecto y SU registro de estudiante.
                        // La generación completa se reserva para la jefa.
                        resultado = await GenerarOficioConsolidadoAsync(
                            idDocente, clave, idProyecto, idEstudianteSolicitante);
                    }

                    pdfs.Add(resultado.bytes);
                    if (pdfs.Count == 1) nombreArchivo = resultado.fileName;
                }

                var salida = pdfs.Count == 1 ? pdfs[0] : UnirPdfs(pdfs);
                return File(salida, "application/pdf", nombreArchivo);
            }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (InvalidOperationException ex) { return Conflict(ex.Message); }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
        }

        private static byte[] UnirPdfs(IEnumerable<byte[]> pdfs)
        {
            using var ms = new MemoryStream();
            using (var writer = new iText.Kernel.Pdf.PdfWriter(ms))
            using (var destino = new iText.Kernel.Pdf.PdfDocument(writer))
            {
                foreach (var bytes in pdfs)
                {
                    using var src = new MemoryStream(bytes);
                    using var reader = new iText.Kernel.Pdf.PdfReader(src);
                    using var origen = new iText.Kernel.Pdf.PdfDocument(reader);
                    origen.CopyPagesTo(1, origen.GetNumberOfPages(), destino);
                }
            }
            return ms.ToArray();
        }
    }
}
