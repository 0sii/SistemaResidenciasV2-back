// ═══════════════════════════════════════════════════════════════════════
// ConstanciasPdfService.Consolidado.cs
//
// INSTRUCCIONES DE INTEGRACIÓN (léelas antes de pegar el archivo):
//
// 1) En tu archivo ORIGINAL "ConstanciaPdfService.cs":
//      - Cambia:   public interface IConstanciasPdfService
//        por:      public partial interface IConstanciasPdfService
//
//      - Cambia:   public class ConstanciasPdfService : IConstanciasPdfService
//        por:      public partial class ConstanciasPdfService : IConstanciasPdfService
//
// 2) Copia este archivo tal cual a la carpeta de tu proyecto (junto al
//    original). No necesitas tocar nada más: los métodos privados que
//    reutilizamos (WrapLines, medidas, flip de membrete, etc.) se
//    redeclaran aquí de forma local para no depender de los privados del
//    otro archivo (evita choques de "partial" con helpers locales).
//
// 3) Estos métodos NO reemplazan a los que ya tienes; los complementan:
//      - BuildOficioAsignacionAsesorInternoConsolidado(...)
//      - BuildOficioAsignacionRevisorReportePreliminarConsolidado(...)
//      - BuildConstanciasAceptacionReportePreliminarConsolidado(...)
//
//    El de "Revisor de Residencia" NO necesita método nuevo: tu
//    BuildOficiosAsignacionRevisoresFormatoFoto(...) YA soporta varias
//    filas (proyectos) agrupadas para un mismo revisor. Solo hay que
//    alimentarlo con TODOS los proyectos del docente (ver
//    ProyectosController.Oficios.cs).
// ═══════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;

using PdfDocument = iText.Kernel.Pdf.PdfDocument;
using PdfReader = iText.Kernel.Pdf.PdfReader;
using PdfWriter = iText.Kernel.Pdf.PdfWriter;

// ─────────────────────────────────────────────────────────────────────────
// DTOs para los oficios consolidados (un docente, varios proyectos)
// ─────────────────────────────────────────────────────────────────────────

/// <summary>Un proyecto dentro del oficio consolidado de Asesor Interno.</summary>
public sealed class ProyectoAsesorInternoItem
{
    public string NumeroControl { get; set; } = "";      // si hay varios residentes, se puede dejar vacío
    public string NombreProyecto { get; set; } = "";
    public string Empresa { get; set; } = "";
    public string Carrera { get; set; } = "";
    public string PeriodoRealizacion { get; set; } = "";
    public List<string> Residentes { get; set; } = new();
}
public sealed class OficioAsignacionAsesorInternoConsolidadoRequest
{
    public string Ciudad { get; set; } = "Oaxaca de Juárez, Oaxaca";
    public DateTime Fecha { get; set; } = DateTime.Today;
    public string Oficio { get; set; } = "JV-XXX/2025";

    public string DestinatarioNombre { get; set; } = "";
    public string DestinatarioCargoLinea1 { get; set; } = "CATEDRATICO(A) DEL I.T. DE OAXACA";

    public List<ProyectoAsesorInternoItem> Proyectos { get; set; } = new();

    public string FirmaNombre { get; set; } = "";
    public string FirmaCargoLinea1 { get; set; } = "JEFA(E) DEL DEPARTAMENTO";
    public string FirmaCargoLinea2 { get; set; } = "DE SISTEMAS Y COMPUTACIÓN";
}

/// <summary>Un proyecto dentro del oficio consolidado de Revisor de Reporte Preliminar.</summary>
public sealed class ProyectoRevisorReportePreliminarItem
{
    public string NombreProyecto { get; set; } = "";
    public List<RevisorReportePreliminarEstudianteItem> Estudiantes { get; set; } = new();
}

public sealed class OficioAsignacionRevisorReportePreliminarConsolidadoRequest
{
    public string Ciudad { get; set; } = "Oaxaca de Juárez, Oaxaca";
    public DateTime Fecha { get; set; } = DateTime.Today;
    public string Oficio { get; set; } = "JV-XXX/2026";
    public string Asunto { get; set; } = "Asignación de revisor de reporte preliminar";
    public string NumeralLineamiento { get; set; } = "12.4.1.7";

    public string DestinatarioNombre { get; set; } = "";
    public string DestinatarioCargoLinea1 { get; set; } = "CATEDRATICO(A) DEL I.T. DE OAXACA";

    public List<ProyectoRevisorReportePreliminarItem> Proyectos { get; set; } = new();

    public string FirmaNombre { get; set; } = "";
    public string FirmaCargoLinea1 { get; set; } = "SUBDIRECTORA ACADÉMICA";
}

public partial interface IConstanciasPdfService
    {
        byte[] BuildOficioAsignacionAsesorInternoConsolidado(
            byte[] templatePdf, OficioAsignacionAsesorInternoConsolidadoRequest req);

        byte[] BuildOficioAsignacionRevisorReportePreliminarConsolidado(
            byte[] templatePdf, OficioAsignacionRevisorReportePreliminarConsolidadoRequest req);

        /// <summary>
        /// Concatena en un solo PDF varias "Constancias de aceptación de reporte
        /// preliminar" (una página por proyecto/estudiante), para el mismo asesor
        /// interno. Reutiliza BuildConstanciaAceptacionReportePreliminar por dentro,
        /// por lo que el layout de cada página no cambia.
        /// </summary>
        byte[] BuildConstanciasAceptacionReportePreliminarConsolidado(
            byte[] templatePdf, List<ConstanciaAceptacionReportePreliminarRequest> items);
    }

namespace WebApiVinculacionProyectosV2.Services
{
    public partial class ConstanciasPdfService : IConstanciasPdfService
    {
        // ── Helpers locales (independientes de los privados del otro archivo) ──

        private static List<string> C_WrapLines(string text, PdfFont f, float size, float maxWidth)
        {
            text ??= "";
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var lines = new List<string>();
            string line = "";

            foreach (var w in words)
            {
                var test = string.IsNullOrEmpty(line) ? w : $"{line} {w}";
                if (f.GetWidth(test, size) > maxWidth && !string.IsNullOrEmpty(line))
                {
                    lines.Add(line);
                    line = w;
                }
                else line = test;
            }
            if (!string.IsNullOrEmpty(line)) lines.Add(line);
            return lines;
        }

        private PdfPage C_NewTemplatedPage(PdfDocument outPdf, byte[] templatePdf)
        {
            PdfPage page;
            if (templatePdf != null && templatePdf.Length > 0)
            {
                using var src = new PdfDocument(new PdfReader(new MemoryStream(templatePdf)));
                src.CopyPagesTo(1, 1, outPdf);
                page = outPdf.GetLastPage();
            }
            else
            {
                page = outPdf.AddNewPage(PageSize.LETTER);
            }

            // Compensar flip vertical del membrete escaneado (mismo criterio que el resto del servicio)
            var pdfCanvas = new PdfCanvas(page);
            var rect = page.GetPageSizeWithRotation();
            var pageRotation = page.GetRotation();
            bool hasYFlip = false;

            if (pageRotation == 0)
            {
                try
                {
                    var pageDict = page.GetPdfObject();
                    var contentsObj = pageDict.Get(PdfName.Contents);
                    PdfStream? firstStream = null;

                    if (contentsObj is PdfArray arr && arr.Size() > 0) firstStream = arr.GetAsStream(0);
                    else if (contentsObj is PdfStream s) firstStream = s;

                    if (firstStream != null)
                    {
                        var bytes = firstStream.GetBytes();
                        var preview = System.Text.Encoding.Latin1.GetString(bytes, 0, Math.Min(bytes.Length, 80));
                        hasYFlip = System.Text.RegularExpressions.Regex.IsMatch(preview, @"1\s+0\s+0\s+-1\s+0\s+[\d.]+\s+cm");
                    }
                }
                catch { }
            }

            float w = rect.GetWidth(), h = rect.GetHeight();
            if (hasYFlip) pdfCanvas.ConcatMatrix(1, 0, 0, -1, 0, h);
            else if (pageRotation == 90) pdfCanvas.ConcatMatrix(0, 1, -1, 0, h, 0);
            else if (pageRotation == 180) pdfCanvas.ConcatMatrix(-1, 0, 0, -1, w, h);
            else if (pageRotation == 270) pdfCanvas.ConcatMatrix(0, -1, 1, 0, 0, w);

            return page;
        }

        // ═══════════════════════════════════════════════════════════════════
        // ASESOR INTERNO — consolidado (varios proyectos del mismo docente)
        // ═══════════════════════════════════════════════════════════════════
        public byte[] BuildOficioAsignacionAsesorInternoConsolidado(
            byte[] templatePdf, OficioAsignacionAsesorInternoConsolidadoRequest req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            var proyectos = req.Proyectos ?? new List<ProyectoAsesorInternoItem>();
            if (proyectos.Count == 0)
                proyectos.Add(new ProyectoAsesorInternoItem { NombreProyecto = "—", Empresa = "—", Carrera = "—", PeriodoRealizacion = "—" });

            using var ms = new MemoryStream();
            using var writer = new PdfWriter(ms);
            using var outPdf = new PdfDocument(writer);

            var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            var fontBold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var esMx = CultureInfo.GetCultureInfo("es-MX");

            const float left = 72f;
            const float rightMargin = 72f;
            const float fechaY = 140f, oficioY = 154f, asuntoY = 168f;
            const float destinatarioTop = 214f, introTop = 260f;
            const float tableTop = 330f, footerSafe = 80f;
            const float atentamenteY = 610f, firmaNombreY = 650f, firmaCargoY = 665f, ccpY = 715f;

            float[] colW = { 150f, 130f, 100f, 100f }; // Proyecto | Empresa | Carrera | Periodo
            float pad = 4f, fontSize = 9.4f, leading = 11.2f, headerH = 20f;

            float MeasureRowHeight(ProyectoAsesorInternoItem p, float w1, float w2, float w3, float w4)
            {
                string proyectoTxt = (p.NombreProyecto ?? "—") +
                    (p.Residentes != null && p.Residentes.Count > 0
                        ? "\n" + string.Join(", ", p.Residentes)
                        : "");

                int l1 = Math.Max(1, C_WrapLines(proyectoTxt, font, fontSize, w1 - pad * 2).Count);
                int l2 = Math.Max(1, C_WrapLines(p.Empresa ?? "—", font, fontSize, w2 - pad * 2).Count);
                int l3 = Math.Max(1, C_WrapLines(p.Carrera ?? "—", font, fontSize, w3 - pad * 2).Count);
                int l4 = Math.Max(1, C_WrapLines(p.PeriodoRealizacion ?? "—", font, fontSize, w4 - pad * 2).Count);

                int maxLines = new[] { l1, l2, l3, l4 }.Max();
                return Math.Max(26f, (maxLines * leading) + pad * 2);
            }

            int idx = 0;

            while (idx < proyectos.Count)
            {
                var page = C_NewTemplatedPage(outPdf, templatePdf);
                var ps = page.GetPageSize();
                float pageW = ps.GetWidth(), pageH = ps.GetHeight();
                float right = pageW - rightMargin;
                float width = right - left;

                float sumCol = colW.Sum();
                float scale = width / sumCol;
                float w1 = colW[0] * scale, w2 = colW[1] * scale, w3 = colW[2] * scale, w4 = colW[3] * scale;

                float Y(float yFromTop) => pageH - yFromTop;
                var pdfCanvas = new PdfCanvas(page);
                var canvas = new Canvas(pdfCanvas, ps);

                void AbsText(string text, float x, float yFromTop, PdfFont f, float size, TextAlignment align)
                {
                    var p = new Paragraph(text ?? "").SetFont(f).SetFontSize(size).SetMargin(0).SetPadding(0);
                    canvas.ShowTextAligned(p, x, Y(yFromTop), align);
                }

                bool firstPage = (idx == 0);

                // Encabezado (todas las páginas)
                string fechaTxt = $"{req.Ciudad} {req.Fecha.ToString("dd'/'MMMM'/'yyyy", esMx)}".ToLower(esMx);
                AbsText(char.ToUpper(fechaTxt[0]) + fechaTxt.Substring(1), right, fechaY, font, 9.6f, TextAlignment.RIGHT);
                AbsText($"OFICIO No. {req.Oficio}", right, oficioY, fontBold, 9.9f, TextAlignment.RIGHT);
                AbsText("ASUNTO: Asesor Interno de Residencia Profesional", right, asuntoY, font, 9.6f, TextAlignment.RIGHT);

                float y = destinatarioTop;
                AbsText((req.DestinatarioNombre ?? "").ToUpperInvariant(), left, y, fontBold, 9.9f, TextAlignment.LEFT);
                y += 17f;
                AbsText((req.DestinatarioCargoLinea1 ?? "").ToUpperInvariant(), left, y, fontBold, 9.7f, TextAlignment.LEFT);
                y += 17f;
                AbsText("P R E S E N T E.", left, y, fontBold, 9.7f, TextAlignment.LEFT);

                if (firstPage)
                {
                    var intro = new Paragraph(
                        proyectos.Count > 1
                        ? "Por este conducto informo a usted que ha sido asignado(a) para fungir como Asesor Interno de los siguientes proyectos de Residencia Profesional:"
                        : "Por este conducto informo a usted que ha sido asignado(a) para fungir como Asesor Interno del Proyecto de Residencia Profesional que a continuación se describe:")
                        .SetFont(font).SetFontSize(9.6f).SetFixedLeading(11.2f).SetMargin(0);
                    intro.SetFixedPosition(left, Y(introTop) - 40f, width);
                    canvas.Add(intro);
                }

                // Selección de filas que caben en esta página
                float maxTableEndTopFinal = atentamenteY - 18f;
                float maxTableHeightFinal = maxTableEndTopFinal - tableTop;
                float maxTableEndTopMid = pageH - footerSafe;
                float maxTableHeightMid = maxTableEndTopMid - tableTop;

                float neededHFinal = headerH;
                for (int j = idx; j < proyectos.Count; j++)
                {
                    neededHFinal += MeasureRowHeight(proyectos[j], w1, w2, w3, w4);
                    if (neededHFinal > maxTableHeightFinal) break;
                }
                bool isLastCandidate = neededHFinal <= maxTableHeightFinal;
                float maxTableHeight = isLastCandidate ? maxTableHeightFinal : maxTableHeightMid;

                float used = headerH;
                var take = new List<ProyectoAsesorInternoItem>();
                while (idx < proyectos.Count)
                {
                    float rh = MeasureRowHeight(proyectos[idx], w1, w2, w3, w4);
                    if (used + rh > maxTableHeight && take.Count > 0) break;
                    take.Add(proyectos[idx]);
                    used += rh;
                    idx++;
                }

                float tableH = used;
                float tableBottom = Y(tableTop) - tableH;

                var table = new Table(UnitValue.CreatePointArray(new[] { w1, w2, w3, w4 }))
                    .SetWidth(UnitValue.CreatePointValue(width))
                    .SetFixedPosition(left, tableBottom, width)
                    .SetFont(font).SetFontSize(fontSize);

                Cell MakeCell(string txt, bool bold = false, bool header = false, float? height = null)
                {
                    var para = new Paragraph(txt ?? "").SetFont(bold ? fontBold : font)
                        .SetFontSize(fontSize).SetFixedLeading(leading).SetMargin(0);
                    var c = new Cell().Add(para).SetPadding(pad)
                        .SetBorder(new SolidBorder(ColorConstants.BLACK, 1))
                        .SetVerticalAlignment(VerticalAlignment.MIDDLE);
                    if (header) c.SetBackgroundColor(new DeviceGray(0.90f));
                    if (height.HasValue) c.SetHeight(height.Value);
                    return c;
                }

                table.AddHeaderCell(MakeCell("PROYECTO / RESIDENTE(S)", true, true, headerH));
                table.AddHeaderCell(MakeCell("EMPRESA", true, true, headerH));
                table.AddHeaderCell(MakeCell("CARRERA", true, true, headerH));
                table.AddHeaderCell(MakeCell("PERIODO", true, true, headerH));

                foreach (var p in take)
                {
                    float rh = MeasureRowHeight(p, w1, w2, w3, w4);
                    string proyectoTxt = (p.NombreProyecto ?? "—") +
                        (p.Residentes != null && p.Residentes.Count > 0 ? "\n" + string.Join(", ", p.Residentes) : "");

                    table.AddCell(MakeCell(proyectoTxt, height: rh));
                    table.AddCell(MakeCell(p.Empresa, height: rh));
                    table.AddCell(MakeCell(p.Carrera, height: rh));
                    table.AddCell(MakeCell(p.PeriodoRealizacion, height: rh));
                }

                canvas.Add(table);

                bool lastPage = idx >= proyectos.Count;
                if (lastPage)
                {
                    float cierreTop = tableTop + tableH + 18f;
                    var cierre = new Paragraph(
                        "Así mismo, le solicito dar el seguimiento pertinente a la realización de cada proyecto aplicando los\n" +
                        "lineamientos establecidos en el Procedimiento para Realizar y Acreditar la Residencia Profesional.\n\n" +
                        "Agradezco de antemano su valioso apoyo en esta importante actividad para la formación profesional\n" +
                        "de nuestro estudiantado.")
                        .SetFont(font).SetFontSize(9.6f).SetFixedLeading(11.2f).SetMargin(0);
                    cierre.SetFixedPosition(left, Y(cierreTop) - 90f, width);
                    canvas.Add(cierre);

                    AbsText("A T E N T A M E N T E", left + width / 2, atentamenteY, fontBold, 9.9f, TextAlignment.CENTER);
                    AbsText(req.FirmaNombre ?? "", left + width / 2, firmaNombreY, fontBold, 9.8f, TextAlignment.CENTER);
                    string cargo = string.Join(" ", new[] { req.FirmaCargoLinea1, req.FirmaCargoLinea2 }.Where(s => !string.IsNullOrWhiteSpace(s)));
                    AbsText(cargo, left + width / 2, firmaCargoY, fontBold, 9.5f, TextAlignment.CENTER);

                    AbsText("ccp. Expediente", left, ccpY, font, 9.2f, TextAlignment.LEFT);
                    AbsText("MMH/mmvh", left, ccpY + 12f, font, 9.2f, TextAlignment.LEFT);
                }

                canvas.Close();
            }

            outPdf.Close();
            return ms.ToArray();
        }

        // ═══════════════════════════════════════════════════════════════════
        // REVISOR DE REPORTE PRELIMINAR — consolidado (varios proyectos)
        // ═══════════════════════════════════════════════════════════════════
        public byte[] BuildOficioAsignacionRevisorReportePreliminarConsolidado(
            byte[] templatePdf, OficioAsignacionRevisorReportePreliminarConsolidadoRequest req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            var proyectos = req.Proyectos ?? new List<ProyectoRevisorReportePreliminarItem>();
            if (proyectos.Count == 0)
                proyectos.Add(new ProyectoRevisorReportePreliminarItem { NombreProyecto = "—" });

            using var ms = new MemoryStream();
            using var writer = new PdfWriter(ms);
            using var outPdf = new PdfDocument(writer);

            var font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            var fontBold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var esMx = CultureInfo.GetCultureInfo("es-MX");

            const float left = 72f;
            const float rightMargin = 72f;
            const float fechaY = 140f, oficioY = 154f, asuntoY = 168f;
            const float destinatarioTop = 214f, introTop = 260f;
            const float tableTop = 340f, footerSafe = 80f;
            const float atentamenteY = 610f, firmaNombreY = 650f, firmaCargoY = 665f, ccpY = 715f;

            float[] colW = { 220f, 260f }; // Proyecto | Estudiantes (No.Control - Nombre)
            float pad = 4f, fontSize = 9.4f, leading = 11.2f, headerH = 20f;

            string EstudiantesTexto(ProyectoRevisorReportePreliminarItem p) =>
                string.Join("\n", (p.Estudiantes ?? new()).Select(e => $"{e.NumeroControl} — {e.NombreEstudiante}"));

            float MeasureRowHeight(ProyectoRevisorReportePreliminarItem p, float w1, float w2)
            {
                int l1 = Math.Max(1, C_WrapLines(p.NombreProyecto ?? "—", font, fontSize, w1 - pad * 2).Count);
                var estTxt = EstudiantesTexto(p);
                int l2 = Math.Max(1, estTxt.Split('\n').Sum(line => Math.Max(1, C_WrapLines(line, font, fontSize, w2 - pad * 2).Count)));

                int maxLines = Math.Max(l1, l2);
                return Math.Max(26f, (maxLines * leading) + pad * 2);
            }

            int idx = 0;

            while (idx < proyectos.Count)
            {
                var page = C_NewTemplatedPage(outPdf, templatePdf);
                var ps = page.GetPageSize();
                float pageW = ps.GetWidth(), pageH = ps.GetHeight();
                float right = pageW - rightMargin;
                float width = right - left;

                float sumCol = colW.Sum();
                float scale = width / sumCol;
                float w1 = colW[0] * scale, w2 = colW[1] * scale;

                float Y(float yFromTop) => pageH - yFromTop;
                var pdfCanvas = new PdfCanvas(page);
                var canvas = new Canvas(pdfCanvas, ps);

                void AbsText(string text, float x, float yFromTop, PdfFont f, float size, TextAlignment align)
                {
                    var p = new Paragraph(text ?? "").SetFont(f).SetFontSize(size).SetMargin(0).SetPadding(0);
                    canvas.ShowTextAligned(p, x, Y(yFromTop), align);
                }

                bool firstPage = (idx == 0);

                string fechaTxt = $"{req.Ciudad} {req.Fecha.ToString("dd 'de' MMMM 'de' yyyy", esMx)}".ToLower(esMx);
                AbsText(char.ToUpper(fechaTxt[0]) + fechaTxt.Substring(1), right, fechaY, font, 9.6f, TextAlignment.RIGHT);
                AbsText($"OFICIO No. {req.Oficio}", right, oficioY, fontBold, 9.9f, TextAlignment.RIGHT);
                AbsText($"ASUNTO: {req.Asunto}", right, asuntoY, font, 9.6f, TextAlignment.RIGHT);

                float y = destinatarioTop;
                AbsText((req.DestinatarioNombre ?? "").ToUpperInvariant(), left, y, fontBold, 9.9f, TextAlignment.LEFT);
                y += 17f;
                AbsText((req.DestinatarioCargoLinea1 ?? "").ToUpperInvariant(), left, y, fontBold, 9.7f, TextAlignment.LEFT);
                y += 17f;
                AbsText("P R E S E N T E", left, y, fontBold, 9.7f, TextAlignment.LEFT);

                if (firstPage)
                {
                    var intro = new Paragraph(
                        $"Con fundamento en el Lineamiento para la Operación de la Residencia Profesional, numeral {req.NumeralLineamiento}, " +
                        (proyectos.Count > 1
                            ? "se le ha designado como Revisor(a) para evaluar el reporte preliminar de los siguientes proyectos de Residencia Profesional:"
                            : "se le ha designado como Revisor(a) para evaluar el reporte preliminar del siguiente proyecto de Residencia Profesional:"))
                        .SetFont(font).SetFontSize(9.6f).SetFixedLeading(11.2f).SetMargin(0);
                    intro.SetFixedPosition(left, Y(introTop) - 45f, width);
                    canvas.Add(intro);
                }

                float maxTableEndTopFinal = atentamenteY - 18f;
                float maxTableHeightFinal = maxTableEndTopFinal - tableTop;
                float maxTableEndTopMid = pageH - footerSafe;
                float maxTableHeightMid = maxTableEndTopMid - tableTop;

                float neededHFinal = headerH;
                for (int j = idx; j < proyectos.Count; j++)
                {
                    neededHFinal += MeasureRowHeight(proyectos[j], w1, w2);
                    if (neededHFinal > maxTableHeightFinal) break;
                }
                bool isLastCandidate = neededHFinal <= maxTableHeightFinal;
                float maxTableHeight = isLastCandidate ? maxTableHeightFinal : maxTableHeightMid;

                float used = headerH;
                var take = new List<ProyectoRevisorReportePreliminarItem>();
                while (idx < proyectos.Count)
                {
                    float rh = MeasureRowHeight(proyectos[idx], w1, w2);
                    if (used + rh > maxTableHeight && take.Count > 0) break;
                    take.Add(proyectos[idx]);
                    used += rh;
                    idx++;
                }

                float tableH = used;
                float tableBottom = Y(tableTop) - tableH;

                var table = new Table(UnitValue.CreatePointArray(new[] { w1, w2 }))
                    .SetWidth(UnitValue.CreatePointValue(width))
                    .SetFixedPosition(left, tableBottom, width)
                    .SetFont(font).SetFontSize(fontSize);

                Cell MakeCell(string txt, bool bold = false, bool header = false, float? height = null)
                {
                    var para = new Paragraph(txt ?? "").SetFont(bold ? fontBold : font)
                        .SetFontSize(fontSize).SetFixedLeading(leading).SetMargin(0);
                    var c = new Cell().Add(para).SetPadding(pad)
                        .SetBorder(new SolidBorder(ColorConstants.BLACK, 1))
                        .SetVerticalAlignment(VerticalAlignment.MIDDLE);
                    if (header) c.SetBackgroundColor(new DeviceGray(0.90f));
                    if (height.HasValue) c.SetHeight(height.Value);
                    return c;
                }

                table.AddHeaderCell(MakeCell("PROYECTO", true, true, headerH));
                table.AddHeaderCell(MakeCell("ESTUDIANTE(S)", true, true, headerH));

                foreach (var p in take)
                {
                    float rh = MeasureRowHeight(p, w1, w2);
                    table.AddCell(MakeCell(p.NombreProyecto, height: rh));
                    table.AddCell(MakeCell(EstudiantesTexto(p), height: rh));
                }

                canvas.Add(table);

                bool lastPage = idx >= proyectos.Count;
                if (lastPage)
                {
                    float cierreTop = tableTop + tableH + 18f;
                    var cierre = new Paragraph(
                        "Su análisis técnico permitirá a la Academia determinar la viabilidad y pertinencia de cada proyecto para su\n" +
                        "posterior autorización por esta Jefatura. En caso de autorizarlos, deberá entregar a la oficina de Vinculación\n" +
                        "el Anexo II por cada estudiante asignado.\n\n" +
                        "Agradezco de antemano su valiosa colaboración en este proceso.")
                        .SetFont(font).SetFontSize(9.6f).SetFixedLeading(11.2f).SetMargin(0);
                    cierre.SetFixedPosition(left, Y(cierreTop) - 90f, width);
                    canvas.Add(cierre);

                    AbsText("Atentamente", left, atentamenteY, fontBold, 9.9f, TextAlignment.LEFT);
                    AbsText(req.FirmaNombre ?? "", left, firmaNombreY, fontBold, 9.9f, TextAlignment.LEFT);
                    AbsText(req.FirmaCargoLinea1 ?? "", left, firmaCargoY, fontBold, 9.8f, TextAlignment.LEFT);

                    AbsText("ccp. Expediente", left, ccpY, font, 9.2f, TextAlignment.LEFT);
                    AbsText("ICMO/mmvh", left, ccpY + 12f, font, 9.2f, TextAlignment.LEFT);
                }

                canvas.Close();
            }

            outPdf.Close();
            return ms.ToArray();
        }

        // ═══════════════════════════════════════════════════════════════════
        // ACEPTACIÓN DE REPORTE PRELIMINAR — consolidado
        // (concatena N páginas, una por proyecto/estudiante, reutilizando
        //  BuildConstanciaAceptacionReportePreliminar sin tocar su layout)
        // ═══════════════════════════════════════════════════════════════════
        public byte[] BuildConstanciasAceptacionReportePreliminarConsolidado(
            byte[] templatePdf, List<ConstanciaAceptacionReportePreliminarRequest> items)
        {
            if (items == null || items.Count == 0)
                throw new ArgumentException("Debe incluir al menos un reporte a aceptar.", nameof(items));

            using var ms = new MemoryStream();
            using var writer = new PdfWriter(ms);
            using var outPdf = new PdfDocument(writer);

            foreach (var item in items)
            {
                var singlePdfBytes = BuildConstanciaAceptacionReportePreliminar(templatePdf, item);

                using var srcMs = new MemoryStream(singlePdfBytes);
                using var srcReader = new PdfReader(srcMs);
                using var srcPdf = new PdfDocument(srcReader);

                srcPdf.CopyPagesTo(1, srcPdf.GetNumberOfPages(), outPdf);
            }

            outPdf.Close();
            return ms.ToArray();
        }
    }
}
