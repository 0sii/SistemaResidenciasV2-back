using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using WebApiVinculacionProyectosV2.Dto;
using WebApiVinculacionProyectosV2.Models;
using WebApiVinculacionProyectosV2.Models.DTOs;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;


public class ActualizarEstadoDocumentoDto
{
    public int EstadoRevision { get; set; }   // 0 = EnRevision, 1 = Aceptado, 2 = Rechazado
    public string? ComentarioRevision { get; set; }
}

namespace WebApiVinculacionProyectosV2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentosController : ControllerBase
    {
        private readonly ResidenciasDbContext _context;
        private readonly IWebHostEnvironment _env;

        private const int TIPO_ANTEPROYECTO = 1;

        // ✅ Expediente: ahora 1–13 (según tu lista)
        private const int TIPO_EXPEDIENTE_MIN = 1;
        private const int TIPO_EXPEDIENTE_MAX = 12;

        private const int TIPO_EXPEDIENTE_CD = 11;
        private const int TIPO_EXPEDIENTE_ACTA = 12;

        private const long MAX_PDF_BYTES = 15L * 1024 * 1024;     // 15MB (ajusta)
        private const long MAX_CD_BYTES = 300L * 1024 * 1024;    // 300MB (ajusta)
        private const long MAX_CD_ZIP_BYTES = 300L * 1024 * 1024;      // 300MB (ajusta)

        private static readonly int[] TIPOS_EXPEDIENTE_OBLIGATORIOS = { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
        private static readonly int[] TIPOS_EXPEDIENTE_PDF_UNIFICADOS = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 12 };

        private static bool EsTipoExpediente(int tipo)
            => tipo >= TIPO_EXPEDIENTE_MIN && tipo <= TIPO_EXPEDIENTE_MAX;

        public DocumentosController(ResidenciasDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        private int GetUserId()
        {
            var candidates = new[]
            {
                ClaimTypes.NameIdentifier,
                JwtRegisteredClaimNames.Sub,
                JwtRegisteredClaimNames.NameId,
                "nameid", "id", "userId"
            };

            foreach (var t in candidates)
            {
                var v = User.FindFirstValue(t);
                if (int.TryParse(v, out var id) && id > 0) return id;
            }
            return 0;
        }

        private async Task<Estudiantes?> GetEstudianteActual()
        {
            var idUsuario = GetUserId();
            if (idUsuario <= 0) return null;

            return await _context.Estudiantes.FirstOrDefaultAsync(e => e.idUsuario == idUsuario);
        }

        // ✅ Angular: POST /api/documentos/anteproyecto  (FormData campo: "Archivo")
        [HttpPost("anteproyecto")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> SubirAnteproyecto(IFormFile Archivo)
        {
            var file = Archivo ?? Request.Form.Files.FirstOrDefault();
            if (file == null || file.Length == 0)
                return BadRequest("Archivo requerido.");

            var est = await GetEstudianteActual();
            if (est == null) return Unauthorized("No se pudo identificar estudiante desde el token.");

            var folder = Path.Combine(_env.ContentRootPath, "Uploads", "Anteproyectos");
            Directory.CreateDirectory(folder);

            var ext = Path.GetExtension(file.FileName);
            var serverName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(folder, serverName);

            await using (var fs = new FileStream(fullPath, FileMode.Create))
                await file.CopyToAsync(fs);

            List<Estudiantes> destinatarios;

            if (est.idProyecto.HasValue && est.idProyecto.Value > 0)
            {
                var idProy = est.idProyecto.Value;

                destinatarios = await _context.Estudiantes
                    .Where(x => x.idProyecto == idProy)
                    .ToListAsync();

                if (destinatarios.Count == 0) destinatarios = new List<Estudiantes> { est };
            }
            else
            {
                destinatarios = new List<Estudiantes> { est };
            }

            var docs = destinatarios.Select(alumno => new Documento
            {
                IdEstudiante = alumno.id,
                TipoDocumento = TIPO_ANTEPROYECTO,
                FechaSubida = DateTime.UtcNow,

                NombreOriginal = Path.GetFileName(file.FileName),
                NombreServidor = serverName,
                ContentType = file.ContentType ?? "application/octet-stream",
                TamanoBytes = file.Length,
                RutaFisica = fullPath
            }).ToList();

            _context.Documentos.AddRange(docs);
            await _context.SaveChangesAsync();

            return Ok(new DocumentoUploadResultDto
            {
                TotalRegistrosCreados = docs.Count,
                IdsDocumentosCreados = docs.Select(d => d.Id).ToList()
            });
        }

        // ✅ Angular: GET /api/documentos/mis-anteproyectos
        [HttpGet("mis-anteproyectos")]
        public async Task<ActionResult<List<DocumentoDto>>> GetMisAnteproyectos()
        {
            var est = await GetEstudianteActual();
            if (est == null) return Unauthorized();

            var rows = await _context.Documentos
                .Where(d => d.IdEstudiante == est.id && d.TipoDocumento == TIPO_ANTEPROYECTO)
                .OrderByDescending(d => d.FechaSubida)
                .Select(d => new DocumentoDto
                {
                    Id = d.Id,
                    TipoDocumento = d.TipoDocumento,
                    FechaSubida = d.FechaSubida,
                    NombreOriginal = d.NombreOriginal,
                    TamanoBytes = d.TamanoBytes
                })
                .ToListAsync();

            return Ok(rows);
        }

        // ✅ Angular: GET /api/documentos/{id}/descargar
        [HttpGet("{id:int}/descargar")]
        public async Task<IActionResult> Descargar(int id)
        {
            var est = await GetEstudianteActual();
            if (est == null) return Unauthorized();

            // 🔒 Asegura que el documento pertenezca al estudiante
            var doc = await _context.Documentos.FirstOrDefaultAsync(d => d.Id == id && d.IdEstudiante == est.id);
            if (doc == null) return NotFound("Documento no existe.");

            if (!System.IO.File.Exists(doc.RutaFisica)) return NotFound("Archivo no encontrado en disco.");

            return PhysicalFile(doc.RutaFisica, doc.ContentType, doc.NombreOriginal);
        }

        [HttpGet("tipos-expediente")]
        public async Task<IActionResult> GetTiposExpediente()
        {
            var tipos = await _context.TipoDocumentos
                .Where(t => t.Activo && t.Id >= TIPO_EXPEDIENTE_MIN && t.Id <= TIPO_EXPEDIENTE_MAX)
                .OrderBy(t => t.Id)
                .Select(t => new { t.Id, t.Descripcion })
                .ToListAsync();

            return Ok(tipos);
        }
        [HttpGet("mis-expediente")]
        public async Task<IActionResult> GetMisExpediente()
        {
            try
            {
                var est = await GetEstudianteActual();
                if (est == null) return Unauthorized("No se pudo identificar estudiante desde el token.");

                var docs = await _context.Documentos
                    .Where(d => d.IdEstudiante == est.id &&
                                d.TipoDocumento >= TIPO_EXPEDIENTE_MIN &&
                                d.TipoDocumento <= TIPO_EXPEDIENTE_MAX)
                    .OrderBy(d => d.TipoDocumento)
                    .Select(d => new
                    {
                        d.Id,
                        d.TipoDocumento,
                        d.FechaSubida,
                        d.NombreOriginal,
                        d.ContentType,
                        d.TamanoBytes,
                        d.UrlExterna,
                        EstadoRevision = (int)d.EstadoRevision,
                        EstadoRevisionTexto = d.EstadoRevision.ToString(),
                        d.ComentarioRevision,
                        d.FechaRevision,
                        d.RevisadoPorUsuarioId
                    })
                    .ToListAsync();

                return Ok(docs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = ex.Message,
                    inner = ex.InnerException?.Message
                });
            }
        }



        [HttpPost("expediente/{tipo:int}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> SubirExpediente(int tipo, IFormFile Archivo)
        {
            if (!EsTipoExpediente(tipo))
                return BadRequest($"Tipo de documento no permitido para expediente (solo {TIPO_EXPEDIENTE_MIN}–{TIPO_EXPEDIENTE_MAX}).");

            if (tipo == TIPO_EXPEDIENTE_ACTA)
                return Forbid("El Acta (tipo 12) la sube el asesor interno desde Proyecto Docente.");

            // 🔒 tipo 11 ya NO acepta archivo
            if (tipo == TIPO_EXPEDIENTE_CD)
                return BadRequest("El tipo 11 solo permite entrega por link de Drive.");

            var file = Archivo ?? Request.Form.Files.FirstOrDefault();
            if (file == null || file.Length == 0)
                return BadRequest("Archivo requerido.");


            var est = await GetEstudianteActual();
            if (est == null) return Unauthorized("No se pudo identificar estudiante desde el token.");

            var existente = await _context.Documentos
                .FirstOrDefaultAsync(d => d.IdEstudiante == est.id && d.TipoDocumento == tipo);

            var folder = Path.Combine(_env.ContentRootPath, "Uploads", "Expediente", est.id.ToString());
            Directory.CreateDirectory(folder);

            string serverName;
            string fullPath;
            string contentType;

            // ✅ Tipo 12: ZIP/RAR (hasta MAX_CD_BYTES)
            if (tipo == TIPO_EXPEDIENTE_CD)
            {
                if (!IsZipOrRar(file, out var extLower, out var ct))
                    return BadRequest("Tipo 12: solo se permite ZIP/RAR válido.");

                if (file.Length > MAX_CD_BYTES)
                    return StatusCode(413, new
                    {
                        mensaje = $"El ZIP/RAR supera el límite ({MAX_CD_BYTES / (1024 * 1024)} MB). Usa entrega por link.",
                        limiteMB = (MAX_CD_BYTES / (1024 * 1024))
                    });

                contentType = ct;
                serverName = $"{Guid.NewGuid():N}{extLower}";
                fullPath = Path.Combine(folder, serverName);
            }
            else
            {
                // ✅ Resto: PDF
                if (!IsPdf(file))
                    return BadRequest("Solo se permite PDF válido.");

                if (file.Length > MAX_PDF_BYTES)
                    return BadRequest($"El PDF supera el límite permitido ({MAX_PDF_BYTES / (1024 * 1024)} MB).");

                contentType = "application/pdf";
                serverName = $"{Guid.NewGuid():N}.pdf";
                fullPath = Path.Combine(folder, serverName);
            }

            await using (var fs = new FileStream(fullPath, FileMode.Create))
                await file.CopyToAsync(fs);

            if (existente != null)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(existente.RutaFisica) && System.IO.File.Exists(existente.RutaFisica))
                        System.IO.File.Delete(existente.RutaFisica);
                }
                catch { }

                existente.FechaSubida = DateTime.UtcNow;
                existente.NombreOriginal = Path.GetFileName(file.FileName);
                existente.NombreServidor = serverName;
                existente.ContentType = contentType;
                existente.TamanoBytes = file.Length;
                existente.RutaFisica = fullPath;
                existente.UrlExterna = null;

                // NUEVO
                existente.EstadoRevision = EstadoRevisionDocumento.EnRevision;
                existente.ComentarioRevision = null;
                existente.FechaRevision = null;
                existente.RevisadoPorUsuarioId = null;

                await _context.SaveChangesAsync();
                return Ok(new { reemplazado = true, idDocumento = existente.Id });
            }
            var doc = new Documento
            {
                IdEstudiante = est.id,
                TipoDocumento = tipo,
                FechaSubida = DateTime.UtcNow,
                NombreOriginal = Path.GetFileName(file.FileName),
                NombreServidor = serverName,
                ContentType = contentType,
                TamanoBytes = file.Length,
                RutaFisica = fullPath,
                UrlExterna = null,

                EstadoRevision = EstadoRevisionDocumento.EnRevision,
                ComentarioRevision = null,
                FechaRevision = null,
                RevisadoPorUsuarioId = null
            };

            _context.Documentos.Add(doc);
            await _context.SaveChangesAsync();

            return Ok(new { reemplazado = false, idDocumento = doc.Id });
        }



        [HttpGet("expediente/{tipo:int}/descargar")]
        public async Task<IActionResult> DescargarExpediente(int tipo)
        {
            if (!EsTipoExpediente(tipo))
                return BadRequest($"Tipo de documento no permitido (solo {TIPO_EXPEDIENTE_MIN}–{TIPO_EXPEDIENTE_MAX}).");

            var est = await GetEstudianteActual();
            if (est == null) return Unauthorized();

            var doc = await _context.Documentos
                .FirstOrDefaultAsync(d => d.IdEstudiante == est.id && d.TipoDocumento == tipo);

            if (doc == null) return NotFound("Documento no existe.");
            if (!System.IO.File.Exists(doc.RutaFisica)) return NotFound("Archivo no encontrado en disco.");

            return PhysicalFile(doc.RutaFisica, doc.ContentType, doc.NombreOriginal);
        }

        [HttpGet("expediente/{tipo:int}/ver")]
        public async Task<IActionResult> VerExpediente(int tipo)
        {
            if (!EsTipoExpediente(tipo))
                return BadRequest($"Tipo de documento no permitido (solo {TIPO_EXPEDIENTE_MIN}–{TIPO_EXPEDIENTE_MAX}).");

            var est = await GetEstudianteActual();
            if (est == null) return Unauthorized();

            var doc = await _context.Documentos
                .FirstOrDefaultAsync(d => d.IdEstudiante == est.id && d.TipoDocumento == tipo);

            if (doc == null) return NotFound("Documento no existe.");
            if (!System.IO.File.Exists(doc.RutaFisica)) return NotFound("Archivo no encontrado en disco.");

            return PhysicalFile(doc.RutaFisica, doc.ContentType);
        }

        private static bool IsPdf(IFormFile? file)

        {
            if (file == null || file.Length == 0) return false;

            var ext = Path.GetExtension(file.FileName);
            if (!string.Equals(ext, ".pdf", StringComparison.OrdinalIgnoreCase)) return false;

            // Validación real por encabezado: "%PDF"
            try
            {
                using var s = file.OpenReadStream();
                Span<byte> header = stackalloc byte[4];
                if (s.Read(header) < 4) return false;

                return header[0] == (byte)'%' &&
                       header[1] == (byte)'P' &&
                       header[2] == (byte)'D' &&
                       header[3] == (byte)'F';
            }
            catch
            {
                return false;
            }
        }



        // ✅ Lista de documentos de expediente de UN estudiante (para Candidatos)
        [HttpGet("expediente/estudiante/{idEstudiante:int}")]
        public async Task<IActionResult> GetExpedienteByEstudiante(int idEstudiante)
        {
            // al menos sesión válida
            if (GetUserId() <= 0) return Unauthorized();

            var docs = await _context.Documentos
                .Where(d => d.IdEstudiante == idEstudiante &&
                            d.TipoDocumento >= TIPO_EXPEDIENTE_MIN &&
                            d.TipoDocumento <= TIPO_EXPEDIENTE_MAX)
                .OrderBy(d => d.TipoDocumento)
                .Select(d => new
                {
                    d.Id,
                    d.TipoDocumento,
                    d.FechaSubida,
                    d.NombreOriginal,
                    d.ContentType,
                    d.TamanoBytes,
                    d.UrlExterna,
                    EstadoRevision = (int)d.EstadoRevision,
                    EstadoRevisionTexto = d.EstadoRevision.ToString(),
                    d.ComentarioRevision,
                    d.FechaRevision,
                    d.RevisadoPorUsuarioId
                })
                .ToListAsync();

            return Ok(docs);
        }

        // ✅ Ver PDF (inline) por estudiante + tipo
        [HttpGet("expediente/estudiante/{idEstudiante:int}/tipo/{tipo:int}/ver")]
        public async Task<IActionResult> VerExpedienteByEstudiante(int idEstudiante, int tipo)
        {
            if (GetUserId() <= 0) return Unauthorized();
            if (!EsTipoExpediente(tipo)) return BadRequest("Tipo no permitido (solo 1–13).");

            var doc = await _context.Documentos
                .FirstOrDefaultAsync(d => d.IdEstudiante == idEstudiante && d.TipoDocumento == tipo);

            if (doc == null) return NotFound("Documento no existe.");
            if (!System.IO.File.Exists(doc.RutaFisica)) return NotFound("Archivo no encontrado en disco.");

            return PhysicalFile(doc.RutaFisica, doc.ContentType ?? "application/pdf");
        }

        // ✅ Descargar PDF por estudiante + tipo
        [HttpGet("expediente/estudiante/{idEstudiante:int}/tipo/{tipo:int}/descargar")]
        public async Task<IActionResult> DescargarExpedienteByEstudiante(int idEstudiante, int tipo)
        {
            if (GetUserId() <= 0) return Unauthorized();
            if (!EsTipoExpediente(tipo)) return BadRequest("Tipo no permitido (solo 1–13).");

            var doc = await _context.Documentos
                .FirstOrDefaultAsync(d => d.IdEstudiante == idEstudiante && d.TipoDocumento == tipo);

            if (doc == null) return NotFound("Documento no existe.");
            if (!System.IO.File.Exists(doc.RutaFisica)) return NotFound("Archivo no encontrado en disco.");

            return PhysicalFile(doc.RutaFisica, doc.ContentType ?? "application/pdf", doc.NombreOriginal);
        }

        // ════════════════════════════════════════════════════════════════════
        // POST /api/documentos/expediente/descargar-seleccionados
        // Descarga solo los tipos de expediente indicados, fusionados en un PDF.
        // Body: { "tipos": [1, 2, 3, ...] }
        // ════════════════════════════════════════════════════════════════════
        [HttpPost("expediente/descargar-seleccionados")]
        public async Task<IActionResult> DescargarExpedienteSeleccionados([FromBody] DescargarSeleccionadosDto dto)
        {
            if (dto?.Tipos == null || dto.Tipos.Count == 0)
                return BadRequest("Debes seleccionar al menos un documento.");

            var est = await GetEstudianteActual();
            if (est == null) return Unauthorized();

            var docs = await _context.Documentos
                .Where(d => d.IdEstudiante == est.id
                         && dto.Tipos.Contains(d.TipoDocumento))
                .OrderBy(d => d.TipoDocumento)
                .ToListAsync();

            if (docs.Count == 0)
                return NotFound("No se encontraron documentos para los tipos seleccionados.");

            // Filtrar solo PDFs con archivo físico válido
            var pdfDocs = new List<Documento>();
            var sinArchivo = new List<int>();

            foreach (var d in docs)
            {
                bool esPdf = !string.IsNullOrWhiteSpace(d.RutaFisica)
                    && System.IO.File.Exists(d.RutaFisica)
                    && string.Equals(Path.GetExtension(d.RutaFisica), ".pdf",
                                     StringComparison.OrdinalIgnoreCase);
                if (esPdf)
                    pdfDocs.Add(d);
                else
                    sinArchivo.Add(d.TipoDocumento);
            }

            if (pdfDocs.Count == 0)
                return BadRequest(new
                {
                    mensaje = "Ninguno de los documentos seleccionados es un PDF disponible.",
                    tiposSinArchivo = sinArchivo
                });

            byte[] mergedBytes;
            try
            {
                mergedBytes = UnirPdfs(pdfDocs.Select(x => x.RutaFisica!).ToList());
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al unir los PDFs.", error = ex.Message });
            }

            var safeNoControl = SanitizeFileNamePart(est.noControl);
            var fileName = $"Expediente_Seleccionado_{safeNoControl}_{DateTime.Today:yyyyMMdd}.pdf";
            return File(mergedBytes, "application/pdf", fileName);
        }

        // ────────────────────────────────────────────────────────────────────
        // MISMO endpoint pero para la Jefa: por estudiante
        // POST /api/documentos/expediente/estudiante/{idEstudiante}/descargar-seleccionados
        // ────────────────────────────────────────────────────────────────────
        [HttpPost("expediente/estudiante/{idEstudiante:int}/descargar-seleccionados")]
        public async Task<IActionResult> DescargarExpedienteSeleccionadosByEstudiante(
            int idEstudiante, [FromBody] DescargarSeleccionadosDto dto)
        {
            if (dto?.Tipos == null || dto.Tipos.Count == 0)
                return BadRequest("Debes seleccionar al menos un documento.");

            var est = await _context.Estudiantes.AsNoTracking()
                .FirstOrDefaultAsync(e => e.id == idEstudiante);
            if (est == null) return NotFound("Estudiante no encontrado.");

            var docs = await _context.Documentos
                .Where(d => d.IdEstudiante == idEstudiante
                         && dto.Tipos.Contains(d.TipoDocumento))
                .OrderBy(d => d.TipoDocumento)
                .ToListAsync();

            if (docs.Count == 0)
                return NotFound("No se encontraron documentos para los tipos seleccionados.");

            var pdfDocs = docs.Where(d =>
                !string.IsNullOrWhiteSpace(d.RutaFisica)
                && System.IO.File.Exists(d.RutaFisica)
                && string.Equals(Path.GetExtension(d.RutaFisica), ".pdf",
                                 StringComparison.OrdinalIgnoreCase)).ToList();

            if (pdfDocs.Count == 0)
                return BadRequest("Ninguno de los seleccionados es un PDF disponible.");

            var mergedBytes = UnirPdfs(pdfDocs.Select(x => x.RutaFisica!).ToList());
            var safeNoControl = SanitizeFileNamePart(est.noControl);
            var fileName = $"Expediente_Seleccionado_{safeNoControl}_{DateTime.Today:yyyyMMdd}.pdf";
            return File(mergedBytes, "application/pdf", fileName);
        }

        [HttpGet("expediente/descargar-completo")]
        public async Task<IActionResult> DescargarMiExpedienteCompleto()
        {
            var est = await GetEstudianteActual();
            if (est == null) return Unauthorized();

            return await DescargarExpedienteCompletoInterno(est.id, est.noControl);
        }

        [HttpGet("expediente/estudiante/{idEstudiante:int}/descargar-completo")]
        public async Task<IActionResult> DescargarExpedienteCompletoByEstudiante(int idEstudiante)
        {
            if (GetUserId() <= 0) return Unauthorized();

            var est = await _context.Estudiantes.FirstOrDefaultAsync(e => e.id == idEstudiante);
            if (est == null) return NotFound("Estudiante no encontrado.");

            return await DescargarExpedienteCompletoInterno(idEstudiante, est.noControl);
        }

        private async Task<IActionResult> DescargarExpedienteCompletoInterno(int idEstudiante, string? noControl)
        {
            var docs = await _context.Documentos
                .Where(d => d.IdEstudiante == idEstudiante &&
                            d.TipoDocumento >= TIPO_EXPEDIENTE_MIN &&
                            d.TipoDocumento <= TIPO_EXPEDIENTE_MAX)
                .OrderBy(d => d.TipoDocumento)
                .ToListAsync();

            if (docs.Count == 0)
                return NotFound("El estudiante no tiene documentos de expediente.");

            var faltantes = new List<int>();
            var noAceptados = new List<int>();

            foreach (var tipo in TIPOS_EXPEDIENTE_OBLIGATORIOS)
            {
                var doc = docs.FirstOrDefault(d => d.TipoDocumento == tipo);
                if (doc == null)
                {
                    faltantes.Add(tipo);
                    continue;
                }

                if (doc.EstadoRevision != EstadoRevisionDocumento.Aceptado)
                    noAceptados.Add(tipo);
            }

            if (faltantes.Count > 0 || noAceptados.Count > 0)
            {
                return BadRequest(new
                {
                    mensaje = "El expediente todavía no está completo y aceptado.",
                    tiposFaltantes = faltantes,
                    tiposNoAceptados = noAceptados
                });
            }

            // PDFs que sí se unirán:
            // - 2..11 y 13 obligatorios
            // - 1 (dictamen) solo si existe
            var pdfDocs = docs
                .Where(d =>
                    TIPOS_EXPEDIENTE_PDF_UNIFICADOS.Contains(d.TipoDocumento) &&
                    d.EstadoRevision == EstadoRevisionDocumento.Aceptado)
                .OrderBy(d => d.TipoDocumento)
                .ToList();

            // Si el dictamen (#1) existe pero no es PDF local, lo ignoramos.
            // Para los demás PDFs obligatorios, sí exigimos archivo físico válido.
            var pdfDocsFinales = new List<Documento>();
            var pdfConProblema = new List<int>();

            foreach (var d in pdfDocs)
            {
                bool existeArchivo =
                    !string.IsNullOrWhiteSpace(d.RutaFisica) &&
                    System.IO.File.Exists(d.RutaFisica) &&
                    string.Equals(Path.GetExtension(d.RutaFisica), ".pdf", StringComparison.OrdinalIgnoreCase);

                if (d.TipoDocumento == 1)
                {
                    if (existeArchivo)
                        pdfDocsFinales.Add(d);

                    continue;
                }

                if (!existeArchivo)
                {
                    pdfConProblema.Add(d.TipoDocumento);
                    continue;
                }

                pdfDocsFinales.Add(d);
            }

            if (pdfConProblema.Count > 0)
            {
                return Conflict(new
                {
                    mensaje = "Hay documentos aceptados que deberían estar en PDF pero no tienen archivo físico válido.",
                    tiposConProblema = pdfConProblema
                });
            }

            if (pdfDocsFinales.Count == 0)
                return BadRequest("No hay PDFs aceptados para unificar.");

            byte[] mergedBytes;
            try
            {
                mergedBytes = UnirPdfs(pdfDocsFinales.Select(x => x.RutaFisica!).ToList());
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    mensaje = "No se pudieron unir los PDFs del expediente.",
                    error = ex.Message
                });
            }

            var safeNoControl = SanitizeFileNamePart(noControl);
            var fileName = $"Expediente_{safeNoControl}.pdf";

            return File(mergedBytes, "application/pdf", fileName);
        }

        private static byte[] UnirPdfs(List<string> rutasPdf)
        {
            using var output = new PdfDocument();
            output.Info.Title = "Expediente completo del estudiante";

            foreach (var ruta in rutasPdf)
            {
                using var input = PdfReader.Open(ruta, PdfDocumentOpenMode.Import);

                for (int i = 0; i < input.PageCount; i++)
                {
                    output.AddPage(input.Pages[i]);
                }
            }

            using var ms = new MemoryStream();
            output.Save(ms, false);
            return ms.ToArray();
        }

        private static string SanitizeFileNamePart(string? value)
        {
            var safe = string.IsNullOrWhiteSpace(value) ? "Alumno" : value.Trim();

            foreach (var ch in Path.GetInvalidFileNameChars())
                safe = safe.Replace(ch, '_');

            return string.IsNullOrWhiteSpace(safe) ? "Alumno" : safe;
        }

        private static bool IsZipOrRar(IFormFile? file, out string extLower, out string contentType)
        {
            extLower = "";
            contentType = "application/octet-stream";
            if (file == null || file.Length == 0) return false;

            extLower = (Path.GetExtension(file.FileName) ?? "").ToLowerInvariant();
            if (extLower != ".zip" && extLower != ".rar") return false;

            try
            {
                using var s = file.OpenReadStream();
                Span<byte> header = stackalloc byte[8];
                var read = s.Read(header);

                // ZIP: PK..
                bool isZip =
                    read >= 4 &&
                    header[0] == (byte)'P' &&
                    header[1] == (byte)'K' &&
                    (header[2] == 0x03 || header[2] == 0x05 || header[2] == 0x07) &&
                    (header[3] == 0x04 || header[3] == 0x06 || header[3] == 0x08);

                // RAR4: 52 61 72 21 1A 07 00
                bool isRar4 =
                    read >= 7 &&
                    header[0] == 0x52 && header[1] == 0x61 && header[2] == 0x72 && header[3] == 0x21 &&
                    header[4] == 0x1A && header[5] == 0x07 && header[6] == 0x00;

                // RAR5: 52 61 72 21 1A 07 01 00
                bool isRar5 =
                    read >= 8 &&
                    header[0] == 0x52 && header[1] == 0x61 && header[2] == 0x72 && header[3] == 0x21 &&
                    header[4] == 0x1A && header[5] == 0x07 && header[6] == 0x01 && header[7] == 0x00;

                if (isZip) { contentType = "application/zip"; return true; }
                if (isRar4 || isRar5) { contentType = "application/vnd.rar"; return true; }

                return false;
            }
            catch { return false; }
        }
        private const int REL_ASESOR_INTERNO = 2;

        private async Task<Docentes?> GetDocenteActual()
        {
            var idUsuario = GetUserId();
            if (idUsuario <= 0) return null;

            return await _context.Docentes.FirstOrDefaultAsync(d => d.idUsuario == idUsuario);
        }

        private async Task<bool> EsAsesorInternoDeProyecto(int idDocente, int idProyecto)
        {
            return await _context.ProyectoDocente.AnyAsync(pd =>
                pd.idProyecto == idProyecto &&
                pd.idDocente == idDocente &&
                pd.IdTipoRelacion == REL_ASESOR_INTERNO
            );
        }

        [HttpGet("acta-residencia/proyecto/{idProyecto:int}")]
        public async Task<IActionResult> GetActasResidenciaByProyecto(int idProyecto)
        {
            var docente = await GetDocenteActual();
            if (docente == null) return Unauthorized();

            if (!await EsAsesorInternoDeProyecto(docente.Id, idProyecto))
                return Forbid("Solo el asesor interno puede consultar las actas de este proyecto.");

            var rows = await _context.Documentos
                .Where(d => d.TipoDocumento == TIPO_EXPEDIENTE_ACTA)
                .Join(
                    _context.Estudiantes.Where(e => e.idProyecto == idProyecto),
                    d => d.IdEstudiante,
                    e => e.id,
                    (d, e) => new
                    {
                        idEstudiante = e.id,
                        idDocumento = d.Id,
                        d.FechaSubida,
                        d.NombreOriginal,
                        d.ContentType,
                        d.TamanoBytes,

                        // ✅ estos faltaban
                        EstadoRevision = (int)d.EstadoRevision,
                        EstadoRevisionTexto = d.EstadoRevision.ToString(),
                        d.ComentarioRevision,
                        d.FechaRevision,
                        d.RevisadoPorUsuarioId
                    }
                )
                .ToListAsync();

            return Ok(rows);
        }

        [HttpPost("acta-residencia/proyecto/{idProyecto:int}/estudiante/{idEstudiante:int}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> SubirActaResidencia(int idProyecto, int idEstudiante, IFormFile Archivo)
        {
            var docente = await GetDocenteActual();
            if (docente == null) return Unauthorized();

            if (!await EsAsesorInternoDeProyecto(docente.Id, idProyecto))
                return Forbid("Solo el asesor interno puede subir el acta.");

            // validar que el alumno pertenezca al proyecto
            var alumnoOk = await _context.Estudiantes.AnyAsync(e => e.id == idEstudiante && e.idProyecto == idProyecto);
            if (!alumnoOk) return BadRequest("El estudiante no pertenece a este proyecto.");

            var file = Archivo ?? Request.Form.Files.FirstOrDefault();
            if (!IsPdf(file)) return BadRequest("Solo se permite PDF válido.");
            if (file!.Length > MAX_PDF_BYTES) return BadRequest($"El PDF supera el límite permitido ({MAX_PDF_BYTES / (1024 * 1024)} MB).");

            var folder = Path.Combine(_env.ContentRootPath, "Uploads", "Expediente", idEstudiante.ToString());
            Directory.CreateDirectory(folder);

            var serverName = $"{Guid.NewGuid():N}.pdf";
            var fullPath = Path.Combine(folder, serverName);

            await using (var fs = new FileStream(fullPath, FileMode.Create))
                await file.CopyToAsync(fs);

            var existente = await _context.Documentos
                .FirstOrDefaultAsync(d => d.IdEstudiante == idEstudiante && d.TipoDocumento == TIPO_EXPEDIENTE_ACTA);

            if (existente != null)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(existente.RutaFisica) && System.IO.File.Exists(existente.RutaFisica))
                        System.IO.File.Delete(existente.RutaFisica);
                }
                catch { }

                existente.FechaSubida = DateTime.UtcNow;
                existente.NombreOriginal = Path.GetFileName(file.FileName);
                existente.NombreServidor = serverName;
                existente.ContentType = "application/pdf";
                existente.TamanoBytes = file.Length;
                existente.RutaFisica = fullPath;

                existente.EstadoRevision = EstadoRevisionDocumento.EnRevision;
                existente.ComentarioRevision = null;
                existente.FechaRevision = null;
                existente.RevisadoPorUsuarioId = null;

                await _context.SaveChangesAsync();
                return Ok(new { reemplazado = true, idDocumento = existente.Id });
            }

            var doc = new Documento
            {
                IdEstudiante = idEstudiante,
                TipoDocumento = TIPO_EXPEDIENTE_ACTA,
                FechaSubida = DateTime.UtcNow,
                NombreOriginal = Path.GetFileName(file.FileName),
                NombreServidor = serverName,
                ContentType = "application/pdf",
                TamanoBytes = file.Length,
                RutaFisica = fullPath,
                EstadoRevision = EstadoRevisionDocumento.EnRevision,
                ComentarioRevision = null,
                FechaRevision = null,
                RevisadoPorUsuarioId = null
            };

            _context.Documentos.Add(doc);
            await _context.SaveChangesAsync();

            return Ok(new { reemplazado = false, idDocumento = doc.Id });
        }


        public class ExpedienteLinkDto
        {
            public string Url { get; set; } = "";
        }

        [HttpPut("expediente/{tipo:int}/link")]
        public async Task<IActionResult> SetExpedienteLink(int tipo, [FromBody] ExpedienteLinkDto dto)
        {
            try
            {
                if (!EsTipoExpediente(tipo)) return BadRequest("Tipo no permitido.");
                if (tipo != TIPO_EXPEDIENTE_CD)
                    return BadRequest("Solo el tipo 11 permite entrega por link.");

                var est = await GetEstudianteActual();
                if (est == null) return Unauthorized();

                var url = (dto?.Url ?? "").Trim();
                if (string.IsNullOrWhiteSpace(url)) return BadRequest("URL requerida.");

                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                    (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                    return BadRequest("La URL debe ser absoluta y usar http/https.");

                if (url.Length > 1024) return BadRequest("La URL es demasiado larga.");

                var existente = await _context.Documentos
                    .FirstOrDefaultAsync(d => d.IdEstudiante == est.id && d.TipoDocumento == tipo);

                if (existente != null)
                {
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(existente.RutaFisica) && System.IO.File.Exists(existente.RutaFisica))
                            System.IO.File.Delete(existente.RutaFisica);
                    }
                    catch { }

                    existente.FechaSubida = DateTime.UtcNow;
                    existente.UrlExterna = url;

                    // ✅ NO null (evita NOT NULL)
                    existente.RutaFisica = "";
                    existente.NombreServidor = "";

                    existente.ContentType = "text/uri-list";
                    existente.TamanoBytes = 0;
                    existente.NombreOriginal = "Entrega por enlace";

                    existente.EstadoRevision = EstadoRevisionDocumento.EnRevision;
                    existente.ComentarioRevision = null;
                    existente.FechaRevision = null;
                    existente.RevisadoPorUsuarioId = null;

                    await _context.SaveChangesAsync();
                    return Ok(new { actualizado = true, idDocumento = existente.Id });
                }

                var doc = new Documento
                {
                    IdEstudiante = est.id,
                    TipoDocumento = tipo,
                    FechaSubida = DateTime.UtcNow,
                    UrlExterna = url,

                    // ✅ NO null
                    RutaFisica = "",
                    NombreServidor = "",

                    ContentType = "text/uri-list",
                    TamanoBytes = 0,
                    NombreOriginal = "Entrega por enlace",

                    EstadoRevision = EstadoRevisionDocumento.EnRevision,
                    ComentarioRevision = null,
                    FechaRevision = null,
                    RevisadoPorUsuarioId = null
                };

                _context.Documentos.Add(doc);
                await _context.SaveChangesAsync();

                return Ok(new { creado = true, idDocumento = doc.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, inner = ex.InnerException?.Message });
            }
        }

        [HttpPut("{idDocumento:int}/estado")]
        public async Task<IActionResult> ActualizarEstadoDocumento(int idDocumento, [FromBody] ActualizarEstadoDocumentoDto dto)
        {
            if (GetUserId() <= 0) return Unauthorized();

            if (dto == null)
                return BadRequest("Datos requeridos.");

            if (!Enum.IsDefined(typeof(EstadoRevisionDocumento), dto.EstadoRevision))
                return BadRequest("Estado de revisión inválido.");

            var estado = (EstadoRevisionDocumento)dto.EstadoRevision;

            if (estado == EstadoRevisionDocumento.Rechazado &&
                string.IsNullOrWhiteSpace(dto.ComentarioRevision))
            {
                return BadRequest("Debes capturar el motivo de rechazo.");
            }

            var doc = await _context.Documentos.FirstOrDefaultAsync(d => d.Id == idDocumento);
            if (doc == null)
                return NotFound("Documento no encontrado.");

            doc.EstadoRevision = estado;
            doc.ComentarioRevision = estado == EstadoRevisionDocumento.Rechazado
                ? dto.ComentarioRevision!.Trim()
                : null;
            doc.FechaRevision = DateTime.UtcNow;
            doc.RevisadoPorUsuarioId = GetUserId();

            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = "Estado del documento actualizado.",
                idDocumento = doc.Id,
                estadoRevision = (int)doc.EstadoRevision,
                estadoRevisionTexto = doc.EstadoRevision.ToString(),
                comentarioRevision = doc.ComentarioRevision,
                fechaRevision = doc.FechaRevision,
                revisadoPorUsuarioId = doc.RevisadoPorUsuarioId
            });
        }
    }

public class DescargarSeleccionadosDto
{
    public List<int> Tipos { get; set; } = new();
}

}