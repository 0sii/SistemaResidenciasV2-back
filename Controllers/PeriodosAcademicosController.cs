using Microsoft.AspNetCore.Authorization;
using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApiVinculacionProyectosV2.Dto;
using WebApiVinculacionProyectosV2.Models;
using Microsoft.EntityFrameworkCore;
using WebApiVinculacionProyectosV2.Services;
using iText.Commons.Actions.Contexts;
using System.IO.Compression;

public sealed class PeriodoDocumentosConfigDto
{
    public string? JefeDepartamentoNombre { get; set; }
    public string? JefeDepartamentoCargoLinea1 { get; set; }
    public string? JefeDepartamentoCargoLinea2 { get; set; }

    public string? PrefijoOficio { get; set; } // "JV"
    public int? ConsecutivoOficioAsesor { get; set; }
    public int? ConsecutivoOficioRevisor { get; set; }
}
public class PeriodoActualDto
{
    public int IdPeriodoAcademico { get; set; }
    public string PeriodoNombre { get; set; } = string.Empty;
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin { get; set; }
    public bool Activo { get; set; }
}

public sealed class PeriodoAcademicoCreateDto
{
    public string Nombre { get; set; } = null!;
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin { get; set; }
    public bool Activo { get; set; } = true;

    public string JefeDepartamentoNombre { get; set; } = null!;
}

public sealed class PeriodoAcademicoUpdateDto
{
    public string Nombre { get; set; } = null!;
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin { get; set; }
    public bool Activo { get; set; } = true;

    public string JefeDepartamentoNombre { get; set; } = null!;

    // ✅ opcional (solo si se quiere resetear/ajustar)
    public int? ConsecutivoOficio { get; set; } = null;
}

public sealed class ConstanciaAceptacionZipItem
{
    public string FileName { get; set; } = "Constancia.pdf";
    public ConstanciaAceptacionReportePreliminarRequest Payload { get; set; } = new();
}

public sealed class ConstanciaAceptacionZipRequest
{
    public List<ConstanciaAceptacionZipItem> Items { get; set; } = new();
}

public sealed class OficioAsesorZipItem
{
    public string FileName { get; set; } = "Oficio.pdf";
    public OficioAsignacionAsesorInternoRequest Payload { get; set; } = new();
}

public sealed class OficioAsesorZipRequest
{
    public List<OficioAsesorZipItem> Items { get; set; } = new();
}

public sealed class OficiosRevisoresZipItem
{
    public string FileName { get; set; } = "Oficio_Revisor.pdf";
    public OficiosAsignacionRevisoresRequest Payload { get; set; } = new();
}

public sealed class OficiosRevisoresZipRequest
{
    public List<OficiosRevisoresZipItem> Items { get; set; } = new();
}

namespace WebApiVinculacionProyectosV2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PeriodosAcademicosController : ControllerBase
    {
        private readonly ResidenciasDbContext _context;
        private readonly IConstanciasPdfService _pdf;

        public PeriodosAcademicosController(ResidenciasDbContext context, IConstanciasPdfService pdf)
        {
            _context = context;

            _pdf = pdf;
        }

        [HttpGet]
        public async Task<ActionResult<List<PeriodoAcademico>>> GetAll()
        {
            return Ok(await _context.PeriodosAcademicos
                .OrderByDescending(p => p.FechaInicio)
                .ToListAsync());
        }

        [HttpGet("Activos")]
        public async Task<ActionResult<List<PeriodoAcademico>>> GetActivos()
        {
            return Ok(await _context.PeriodosAcademicos
                .Where(p => p.Activo)
                .ToListAsync());
        }

        [HttpPost]
public async Task<IActionResult> Create([FromBody] PeriodoAcademicoCreateDto dto)
{
    if (dto.FechaFin < dto.FechaInicio)
        return BadRequest("La fecha fin no puede ser menor a la fecha inicio.");

    if (string.IsNullOrWhiteSpace(dto.JefeDepartamentoNombre))
        return BadRequest("El nombre del Jefe(a) de Departamento es obligatorio.");

    bool solapado = await _context.PeriodosAcademicos.AnyAsync(p =>
        dto.FechaInicio <= p.FechaFin &&
        dto.FechaFin >= p.FechaInicio
    );

    if (solapado)
        return Conflict("El período se cruza con otro existente.");

    if (dto.Activo)
    {
        var activos = await _context.PeriodosAcademicos.Where(p => p.Activo).ToListAsync();
        foreach (var p in activos) p.Activo = false;
    }

    var periodoNombre = string.IsNullOrWhiteSpace(dto.Nombre)
        ? GetPeriodoNombre(dto.FechaInicio, dto.FechaFin)
        : dto.Nombre.Trim();

    var periodo = new PeriodoAcademico
    {
        Nombre = periodoNombre,
        FechaInicio = dto.FechaInicio,
        FechaFin = dto.FechaFin,
        Activo = dto.Activo,
        JefeDepartamentoNombre = dto.JefeDepartamentoNombre.Trim(),
        // PrefijoOficio = "JV" (default)
        // ConsecutivoOficio = 1 (default)
    };

    _context.PeriodosAcademicos.Add(periodo);
    await _context.SaveChangesAsync();

    return Ok(periodo);
}


        [HttpGet("Activos/membrentado")]
        public async Task<IActionResult> DownloadMembrentadoActivo()
        {
            var periodoActivo = await _context.PeriodosAcademicos
                .AsNoTracking()
                .Where(p => p.Activo)
                .OrderByDescending(p => p.FechaInicio)
                .FirstOrDefaultAsync();

            if (periodoActivo == null)
                return NotFound("No hay período activo.");

            var mem = await _context.PeriodosMembrentados
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.PeriodoAcademicoId == periodoActivo.Id);

            if (mem == null)
                return NotFound("El período activo no tiene membrentado.");

            return File(mem.PdfBytes, mem.ContentType, mem.FileName);
        }





        // ✅ META: saber si existe membrentado para ese periodo
        [HttpGet("{periodoId:int}/membrentado/meta")]
        public async Task<IActionResult> GetMembrentadoMeta(int periodoId)
        {
            var existePeriodo = await _context.PeriodosAcademicos
                .AsNoTracking()
                .AnyAsync(p => p.Id == periodoId);

            if (!existePeriodo)
                return NotFound("No existe el período académico.");

            var m = await _context.PeriodosMembrentados
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.PeriodoAcademicoId == periodoId);

            return Ok(new
            {
                exists = m != null,
                fileName = m?.FileName,
                contentType = m?.ContentType,
                uploadedAt = m?.UploadedAt
            });
        }

        // ✅ DESCARGAR membrentado
        [HttpGet("{periodoId:int}/membrentado")]
        public async Task<IActionResult> DownloadMembrentado(int periodoId)
        {
            var m = await _context.PeriodosMembrentados
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.PeriodoAcademicoId == periodoId);

            if (m == null || m.PdfBytes == null || m.PdfBytes.Length == 0)
                return NotFound("Este período no tiene membrentado.");

            var fileName = string.IsNullOrWhiteSpace(m.FileName)
                ? $"membrentado_periodo_{periodoId}.pdf"
                : m.FileName;

            var contentType = string.IsNullOrWhiteSpace(m.ContentType)
                ? "application/pdf"
                : m.ContentType;

            return File(m.PdfBytes, contentType, fileName);
        }

        // ✅ SUBIR / REEMPLAZAR membrentado (multipart/form-data)
        [HttpPost("{periodoId:int}/membrentado")]
        [RequestSizeLimit(20_000_000)] // 20MB
        public async Task<IActionResult> UploadMembrentado(int periodoId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Archivo requerido.");

            var existePeriodo = await _context.PeriodosAcademicos
                .AnyAsync(p => p.Id == periodoId);

            if (!existePeriodo)
                return NotFound("No existe el período académico.");

            // ✅ Validación PDF (simple pero efectiva)
            var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (ext != ".pdf" && file.ContentType != "application/pdf")
                return BadRequest("Solo se permite PDF.");

            if (file.Length > 20_000_000)
                return BadRequest("El PDF excede 20MB.");

            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                bytes = ms.ToArray();
            }

            var m = await _context.PeriodosMembrentados
                .FirstOrDefaultAsync(x => x.PeriodoAcademicoId == periodoId);

            if (m == null)
            {
                m = new PeriodoMembrentado
                {
                    PeriodoAcademicoId = periodoId
                };
                _context.PeriodosMembrentados.Add(m);
            }

            m.FileName = file.FileName;
            m.ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/pdf" : file.ContentType;
            m.PdfBytes = bytes;
            m.UploadedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Membrentado guardado.",
                periodoId,
                uploadedAt = m.UploadedAt,
                fileName = m.FileName
            });
        }

        // ✅ ELIMINAR membrentado
        [HttpDelete("{periodoId:int}/membrentado")]
        public async Task<IActionResult> DeleteMembrentado(int periodoId)
        {
            var m = await _context.PeriodosMembrentados
                .FirstOrDefaultAsync(x => x.PeriodoAcademicoId == periodoId);

            if (m == null)
                return NotFound("Este período no tiene membrentado.");

            _context.PeriodosMembrentados.Remove(m);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Membrentado eliminado.", periodoId });
        }
        [HttpPost("{idPeriodoAcademico:int}/constancias/aceptacion-reporte-preliminar")]
        public async Task<IActionResult> GenerateConstanciaAceptacionReportePreliminar(
    int idPeriodoAcademico,
    [FromBody] ConstanciaAceptacionReportePreliminarRequest req)
        {
            var mem = await _context.PeriodosMembrentados
                .AsNoTracking()
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync(x => x.PeriodoAcademicoId == idPeriodoAcademico);

            if (mem == null || mem.PdfBytes == null || mem.PdfBytes.Length == 0)
                return NotFound("Este período no tiene membrentado.");

            var pdfBytes = _pdf.BuildConstanciaAceptacionReportePreliminar(mem.PdfBytes, req);

            var safeName = string.IsNullOrWhiteSpace(req.NoControl)
                ? "Constancia_Aceptacion_ReportePreliminar"
                : $"Constancia_Aceptacion_ReportePreliminar_{req.NoControl}";

            return File(pdfBytes, "application/pdf", $"{safeName}.pdf");
        }

        [HttpPost("{idPeriodo:int}/constancias/oficio-asesor-interno")]
public async Task<IActionResult> OficioAsesorInterno(int idPeriodo, [FromBody] OficioAsignacionAsesorInternoRequest req)
{
    var mem = await _context.PeriodosMembrentados
        .AsNoTracking()
        .OrderByDescending(x => x.Id)
        .FirstOrDefaultAsync(x => x.PeriodoAcademicoId == idPeriodo);

    if (mem == null || mem.PdfBytes == null || mem.PdfBytes.Length == 0)
        return NotFound("Este período no tiene membrentado.");

    using var tx = await _context.Database.BeginTransactionAsync();

    var periodo = await _context.PeriodosAcademicos.FirstOrDefaultAsync(p => p.Id == idPeriodo);
    if (periodo == null) return NotFound("No existe el período académico.");

    if (req.Fecha == default) req.Fecha = DateTime.Today;

    ApplyFirma(periodo, req);

    if (NeedsAutoOficio(req.Oficio))
    {
        req.Oficio = BuildOficio(periodo.PrefijoOficio, periodo.ConsecutivoOficio, req.Fecha);
        periodo.ConsecutivoOficio++;
        await _context.SaveChangesAsync();
    }

    await tx.CommitAsync();

    var bytes = _pdf.BuildOficioAsignacionAsesorInterno(mem.PdfBytes, req);
    return File(bytes, "application/pdf", $"Oficio_Asesor_{req.Oficio}.pdf");
}

        [HttpPost("{idPeriodo:int}/constancias/oficios-revisores")]
        public async Task<IActionResult> OficiosRevisores(int idPeriodo, [FromBody] OficiosAsignacionRevisoresRequest req)
        {
            var mem = await _context.PeriodosMembrentados
                .AsNoTracking()
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync(x => x.PeriodoAcademicoId == idPeriodo);

            if (mem == null || mem.PdfBytes == null || mem.PdfBytes.Length == 0)
                return NotFound("Este período no tiene membrentado.");
            var bytes = _pdf.BuildOficiosAsignacionRevisores(mem.PdfBytes, req);
            return File(bytes, "application/pdf", "Oficios_Revisores.pdf");
        }

        [HttpPost("{idPeriodo:int}/constancias/oficios-revisores-formato-foto")]
        public async Task<IActionResult> OficiosRevisoresFormatoFoto(int idPeriodo, [FromBody] OficiosAsignacionRevisoresRequest req)
        {
            var mem = await _context.PeriodosMembrentados
                .AsNoTracking()
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync(x => x.PeriodoAcademicoId == idPeriodo);

            if (mem == null || mem.PdfBytes == null || mem.PdfBytes.Length == 0)
                return NotFound("Este período no tiene membrentado.");

            var bytes = _pdf.BuildOficiosAsignacionRevisoresFormatoFoto(mem.PdfBytes, req);

            return File(bytes, "application/pdf", "Oficios_Revisores_Formato_Foto.pdf");
        }

        [HttpGet("Actual")]
        public async Task<ActionResult<PeriodoActualDto>> GetPeriodoActual()
        {
            // Usamos fecha local del servidor. Si tu servidor está en UTC y quieres México,
            // lo correcto es guardar/consultar en UTC o usar una zona definida.
            // Como tus fechas son DateOnly, normalmente esto basta.
            var hoy = DateOnly.FromDateTime(DateTime.Now);

            // 1) Preferencia: el que esté marcado Activo
            var periodoActivo = await _context.PeriodosAcademicos
                .AsNoTracking()
                .Where(p => p.Activo)
                .OrderByDescending(p => p.FechaInicio)
                .FirstOrDefaultAsync();

            if (periodoActivo != null)
            {
                return Ok(new PeriodoActualDto
                {
                    IdPeriodoAcademico = periodoActivo.Id,
                    PeriodoNombre = periodoActivo.Nombre ?? $"Periodo #{periodoActivo.Id}",
                    FechaInicio = periodoActivo.FechaInicio,
                    FechaFin = periodoActivo.FechaFin,
                    Activo = periodoActivo.Activo
                });
            }

            // 2) Fallback: el que contenga la fecha de hoy
            var periodoPorFecha = await _context.PeriodosAcademicos
                .AsNoTracking()
                .Where(p => p.FechaInicio <= hoy && p.FechaFin >= hoy)
                .OrderByDescending(p => p.FechaInicio)
                .FirstOrDefaultAsync();

            if (periodoPorFecha != null)
            {
                return Ok(new PeriodoActualDto
                {
                    IdPeriodoAcademico = periodoPorFecha.Id,
                    PeriodoNombre = periodoPorFecha.Nombre ?? $"Periodo #{periodoPorFecha.Id}",
                    FechaInicio = periodoPorFecha.FechaInicio,
                    FechaFin = periodoPorFecha.FechaFin,
                    Activo = periodoPorFecha.Activo
                });
            }

            // 3) Nada aplica: error explícito
            return NotFound(new
            {
                message = "No existe un período académico actual. No hay período Activo y ninguno cubre la fecha de hoy.",
                hoy = hoy.ToString("yyyy-MM-dd")
            });
        }




        [HttpPost("{idPeriodoAcademico:int}/constancias/aceptacion-reporte-preliminar/zip")]
        public async Task<IActionResult> ZipConstanciasAceptacionReportePreliminar(
            int idPeriodoAcademico,
            [FromBody] ConstanciaAceptacionZipRequest req)
        {
            if (req?.Items == null || req.Items.Count == 0)
                return BadRequest("No hay items para generar.");

            var mem = await _context.PeriodosMembrentados
                .AsNoTracking()
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync(x => x.PeriodoAcademicoId == idPeriodoAcademico);

            if (mem == null || mem.PdfBytes == null || mem.PdfBytes.Length == 0)
                return NotFound("Este período no tiene membrentado.");

            using var ms = new MemoryStream();
            using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var it in req.Items)
                {
                    if (it?.Payload == null) continue;

                    var fileName = SafeZipName(it.FileName, "Constancia.pdf");
                    var pdfBytes = _pdf.BuildConstanciaAceptacionReportePreliminar(mem.PdfBytes, it.Payload);

                    var entry = zip.CreateEntry(fileName, CompressionLevel.Fastest);
                    using var entryStream = entry.Open();
                    await entryStream.WriteAsync(pdfBytes, 0, pdfBytes.Length);
                }
            }

            ms.Position = 0;
            var zipName = $"Constancias_Aceptacion_Periodo_{idPeriodoAcademico}.zip";
            return File(ms.ToArray(), "application/zip", zipName);
        }


[HttpPost("{idPeriodoAcademico:int}/constancias/oficio-asesor-interno/zip")]
public async Task<IActionResult> ZipOficiosAsesorInterno(int idPeriodoAcademico, [FromBody] OficioAsesorZipRequest req)
{
    if (req?.Items == null || req.Items.Count == 0)
        return BadRequest("No hay items para generar.");

    var mem = await _context.PeriodosMembrentados
        .AsNoTracking()
        .OrderByDescending(x => x.Id)
        .FirstOrDefaultAsync(x => x.PeriodoAcademicoId == idPeriodoAcademico);

    if (mem == null || mem.PdfBytes == null || mem.PdfBytes.Length == 0)
        return NotFound("Este período no tiene membrentado.");

    using (var tx = await _context.Database.BeginTransactionAsync())
    {
        var periodo = await _context.PeriodosAcademicos.FirstOrDefaultAsync(p => p.Id == idPeriodoAcademico);
        if (periodo == null) return NotFound("No existe el período académico.");

        foreach (var it in req.Items)
        {
            if (it?.Payload == null) continue;

            if (it.Payload.Fecha == default) it.Payload.Fecha = DateTime.Today;

            ApplyFirma(periodo, it.Payload);

            if (NeedsAutoOficio(it.Payload.Oficio))
            {
                it.Payload.Oficio = BuildOficio(periodo.PrefijoOficio, periodo.ConsecutivoOficio, it.Payload.Fecha);
                periodo.ConsecutivoOficio++;
            }
        }

        await _context.SaveChangesAsync();
        await tx.CommitAsync();
    }

    using var ms = new MemoryStream();
    using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
    {
        foreach (var it in req.Items)
        {
            if (it?.Payload == null) continue;

            var fileName = SafeZipName(it.FileName, $"Oficio_{it.Payload.Oficio}.pdf");
            var pdfBytes = _pdf.BuildOficioAsignacionAsesorInterno(mem.PdfBytes, it.Payload);

            var entry = zip.CreateEntry(fileName, CompressionLevel.Fastest);
            using var entryStream = entry.Open();
            await entryStream.WriteAsync(pdfBytes, 0, pdfBytes.Length);
        }
    }

    ms.Position = 0;
    var zipName = $"Oficios_Asesor_Periodo_{idPeriodoAcademico}.zip";
    return File(ms.ToArray(), "application/zip", zipName);
}

        private static string SafeZipName(string? name, string fallback)
        {
            var s = string.IsNullOrWhiteSpace(name) ? fallback : name.Trim();
            // evita rutas dentro del zip y caracteres raros
            s = s.Replace("\\", "_").Replace("/", "_").Replace("..", "_");
            foreach (var c in Path.GetInvalidFileNameChars())
                s = s.Replace(c, '_');
            return s;
        }


        [HttpPost("{idPeriodoAcademico:int}/constancias/oficios-revisores/zip")]
public async Task<IActionResult> ZipOficiosRevisores(
    int idPeriodoAcademico,
    [FromBody] OficiosRevisoresZipRequest req)
{
    if (req?.Items == null || req.Items.Count == 0)
        return BadRequest("No hay items para generar.");

    var mem = await _context.PeriodosMembrentados
        .AsNoTracking()
        .OrderByDescending(x => x.Id)
        .FirstOrDefaultAsync(x => x.PeriodoAcademicoId == idPeriodoAcademico);

    if (mem == null || mem.PdfBytes == null || mem.PdfBytes.Length == 0)
        return NotFound("Este período no tiene membrentado.");

    using (var tx = await _context.Database.BeginTransactionAsync())
    {
        var periodo = await _context.PeriodosAcademicos.FirstOrDefaultAsync(p => p.Id == idPeriodoAcademico);
        if (periodo == null) return NotFound("No existe el período académico.");

        foreach (var it in req.Items)
        {
            if (it?.Payload == null) continue;
            if (it.Payload.Revisores == null || it.Payload.Revisores.Count == 0) continue;

            if (it.Payload.Fecha == default)
                it.Payload.Fecha = DateTime.Today;

            ApplyFirma(periodo, it.Payload);

            if (NeedsAutoOficio(it.Payload.Oficio))
            {
                it.Payload.Oficio = BuildOficio(periodo.PrefijoOficio, periodo.ConsecutivoOficio, it.Payload.Fecha);
                periodo.ConsecutivoOficio++;
            }
        }

        await _context.SaveChangesAsync();
        await tx.CommitAsync();
    }

    using var ms = new MemoryStream();
    using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
    {
        foreach (var it in req.Items)
        {
            if (it?.Payload == null) continue;
            if (it.Payload.Revisores == null || it.Payload.Revisores.Count == 0) continue;

            var fileName = SafeZipName(it.FileName, $"Oficio_{it.Payload.Oficio}.pdf");
            var pdfBytes = _pdf.BuildOficiosAsignacionRevisores(mem.PdfBytes, it.Payload);

            var entry = zip.CreateEntry(fileName, CompressionLevel.Fastest);
            using var entryStream = entry.Open();
            await entryStream.WriteAsync(pdfBytes, 0, pdfBytes.Length);
        }
    }

    ms.Position = 0;
    var zipName = $"Oficios_Revisores_Periodo_{idPeriodoAcademico}.zip";
    return File(ms.ToArray(), "application/zip", zipName);
}

        [HttpGet("{periodoId:int}/documentos-config")]
        public async Task<IActionResult> GetDocumentosConfig(int periodoId)
        {
            var p = await _context.PeriodosAcademicos
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == periodoId);

            if (p == null) return NotFound("No existe el período académico.");

            return Ok(new
            {
                p.Id,
                p.JefeDepartamentoNombre,
                p.PrefijoOficio,
            });
        }

        [HttpPut("{periodoId:int}/documentos-config")]
        public async Task<IActionResult> UpdateDocumentosConfig(int periodoId, [FromBody] PeriodoDocumentosConfigDto dto)
        {
            var p = await _context.PeriodosAcademicos.FirstOrDefaultAsync(x => x.Id == periodoId);
            if (p == null) return NotFound("No existe el período académico.");

            p.JefeDepartamentoNombre = dto.JefeDepartamentoNombre?.Trim();

            if (!string.IsNullOrWhiteSpace(dto.PrefijoOficio))
                p.PrefijoOficio = dto.PrefijoOficio.Trim();


            await _context.SaveChangesAsync();
            return Ok(new { message = "Configuración actualizada.", periodoId });
        }

        [HttpPut("{id:int}")]
public async Task<IActionResult> Update(int id, [FromBody] PeriodoAcademicoUpdateDto dto)
{
    if (dto.FechaFin < dto.FechaInicio)
        return BadRequest("La fecha fin no puede ser menor a la fecha inicio.");

    if (string.IsNullOrWhiteSpace(dto.JefeDepartamentoNombre))
        return BadRequest("El nombre del Jefe(a) de Departamento es obligatorio.");

    var periodo = await _context.PeriodosAcademicos.FirstOrDefaultAsync(p => p.Id == id);
    if (periodo == null) return NotFound("No existe el período académico.");

    // solapamiento excluyendo el propio id
    bool solapado = await _context.PeriodosAcademicos.AnyAsync(p =>
        p.Id != id &&
        dto.FechaInicio <= p.FechaFin &&
        dto.FechaFin >= p.FechaInicio
    );
    if (solapado)
        return Conflict("El período se cruza con otro existente.");

    if (dto.Activo)
    {
        var activos = await _context.PeriodosAcademicos.Where(p => p.Activo && p.Id != id).ToListAsync();
        foreach (var p in activos) p.Activo = false;
    }

    periodo.Nombre = string.IsNullOrWhiteSpace(dto.Nombre)
        ? GetPeriodoNombre(dto.FechaInicio, dto.FechaFin)
        : dto.Nombre.Trim();
    periodo.FechaInicio = dto.FechaInicio;
    periodo.FechaFin = dto.FechaFin;
    periodo.Activo = dto.Activo;
    periodo.JefeDepartamentoNombre = dto.JefeDepartamentoNombre.Trim();

    // ✅ opcional: reset/ajuste de consecutivo
    if (dto.ConsecutivoOficio.HasValue && dto.ConsecutivoOficio.Value > 0)
        periodo.ConsecutivoOficio = dto.ConsecutivoOficio.Value;

    await _context.SaveChangesAsync();
    return Ok(periodo);
}

        private static bool LooksPlaceholderFirma(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return true;
            var t = s.Trim().ToUpperInvariant();
            return t == "NOMBRE DE QUIEN FIRMA"
                || t == "NOMBRE DEL JEFE(A)"
                || t == "NOMBRE DEL JEFE"
                || t == "NOMBRE";
        }

        private static void FillFirmaFromPeriodo(PeriodoAcademico p, OficioAsignacionAsesorInternoRequest req)
        {
            if (LooksPlaceholderFirma(req.FirmaNombre) && !string.IsNullOrWhiteSpace(p.JefeDepartamentoNombre))
                req.FirmaNombre = p.JefeDepartamentoNombre;

        }

        private static void FillFirmaFromPeriodo(PeriodoAcademico p, OficiosAsignacionRevisoresRequest req)
        {
            if (LooksPlaceholderFirma(req.FirmaNombre) && !string.IsNullOrWhiteSpace(p.JefeDepartamentoNombre))
                req.FirmaNombre = p.JefeDepartamentoNombre;

        }

        private static string GetPeriodoNombre(DateOnly fechaInicio, DateOnly fechaFin)
        {
            var meses = new[]
            {
                "ENE", "FEB", "MAR", "ABR", "MAY", "JUN",
                "JUL", "AGO", "SEP", "OCT", "NOV", "DIC"
            };

            var inicio = meses[Math.Clamp(fechaInicio.Month - 1, 0, meses.Length - 1)];
            var fin = meses[Math.Clamp(fechaFin.Month - 1, 0, meses.Length - 1)];
            return $"{inicio}-{fin} {fechaFin.Year}";
        }

        private static bool NeedsAutoOficio(string? oficio)
        {
            if (string.IsNullOrWhiteSpace(oficio)) return true;
    var t = oficio.Trim().ToUpperInvariant();
    return t.Contains("XXX") || t.Contains("____") || t.Contains("________");
}

private static string BuildOficio(string prefijo, int consecutivo, DateTime fecha)
{
    var yy = (fecha.Year % 100).ToString("00");
    return $"{prefijo}-{consecutivo:000}/{yy}";
}

// ✅ cargos predefinidos
private const string CARGO_ASESOR_1 = "JEFA(E) DEL DEPARTAMENTO";
private const string CARGO_ASESOR_2 = "DE SISTEMAS Y COMPUTACIÓN";
private const string CARGO_REVISOR_1 = "JEFA(E) DEL DEPARTAMENTO DE SISTEMAS Y COMPUTACIÓN";

private static void ApplyFirma(PeriodoAcademico p, OficioAsignacionAsesorInternoRequest req)
{
    if (!string.IsNullOrWhiteSpace(p.JefeDepartamentoNombre))
        req.FirmaNombre = p.JefeDepartamentoNombre.Trim();

    req.FirmaCargoLinea1 = CARGO_ASESOR_1;
    req.FirmaCargoLinea2 = CARGO_ASESOR_2;
}

private static void ApplyFirma(PeriodoAcademico p, OficiosAsignacionRevisoresRequest req)
{
    if (!string.IsNullOrWhiteSpace(p.JefeDepartamentoNombre))
        req.FirmaNombre = p.JefeDepartamentoNombre.Trim();

    req.FirmaCargoLinea1 = CARGO_REVISOR_1;
}

    }
}